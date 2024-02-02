using MazeGame.Input;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class SpatialParadoxGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private TunnelSection startSection;
    [SerializeField] private TunnelSection deadEndPlug;
    [SerializeField] private BreakableWall breakableWall;
    [SerializeField] private List<TunnelSection> tunnelSections;
    [SerializeField] private GameObject stagnationBeacon;

    private Dictionary<int, TunnelSection> instanceIdToSection;
    private List<int> tunnelSectionsByInstanceID;

    [Header("Runtime Map")]
    [SerializeField] private TunnelSection curPlayerSection;
    [SerializeField] private List<List<TunnelSection>> mapTree = new();
    // private Dictionary<int, int> sectionCounter = new();
    private Dictionary<TunnelSection, SectionDstData> mothBalledSections = new();
    private Dictionary<int, List<TunnelSection>> promoteSectionsDict = new();
    List<TunnelSection> promoteSectionsList = new();
    private int reRingInters = 0;
    private Transform sectionGraveYard; 
    private Coroutine mapUpdateProcess;
    
    private PlayerExplorationStatistics explorationStatistics;
    public PlayerExplorationStatistics ExplorationStatistics => explorationStatistics;

    public Pluse OnMapUpdate;
    public Pluse OnEnterLadderSection;
    public Pluse OnEnterColonySection;
    public List<List<TunnelSection>> MapTree => mapTree;
    public TunnelSection CurPlayerSection => curPlayerSection;

    // these are quite unsafe lol
    // Its fine, once initilised their capacity doesn't change and their values are accessed safely by seperate threads.
    // the keys for these parallel hash maps are the prefab instance ids of the sections.
    UnsafeParallelHashMap<int, UnsafeList<UnsafeList<BoxTransform>>> sectionBoxTransforms; // result of box tranform when the map looks to position a new section.
    NativeParallelHashMap<int, UnsafeList<BurstConnector>> sectionConnectorContainers; // connector structs for each section - these all have an identity matrix applied.
    NativeParallelHashMap<int, UnsafeList<float4x4>> sectionBoxMatrices; // local matrices of the bounding boxes of the section

    [Header("Generation Settings")]
    [SerializeField] private int ringRenderDst = 3; 
    [SerializeField, Min(1)] private int maxDst = 3;
    [SerializeField, Range(0, 1)] private float breakableWallAtConnectionChance = 0.5f;
    [Space]
    [SerializeField] private LayerMask tunnelSectionLayerMask;
    [SerializeField, Min(1000)] private int maxInterations = 1000000; /// max iterations allowed for <see cref="PickSection(TunnelSection, List{int}, out Connector, out Connector)"/>
    [SerializeField] private bool randomSeed = true; // generate a new seed every application run or use old seed.
    [SerializeField] private Random.State seed; // override seed.
    [SerializeField] private bool parallelMatrixCalculations = false; // allow parallel Matrix calculations for big matrix job
    [SerializeField] private bool mapProfiling = false;
    private int tunnelSectionLayerIndex;
    private TunnelSection lastEnter;
    private TunnelSection lastExit;
    private bool forceBreakableWallAtConnections = false;
    private bool rejectBreakableWallAtConnections = false;


    private void Awake()
    {
        // construct instance Id based data structures so we aren't doing reference based look-ups.
        instanceIdToSection = new Dictionary<int, TunnelSection>(tunnelSections.Count);
        tunnelSectionsByInstanceID = new(tunnelSections.Count);
        tunnelSections.ForEach(prefab =>
        {
            prefab.excludeConnectorSections.ForEach(connector => connector.Build());
            prefab.Build(this);
            tunnelSectionsByInstanceID.Add(prefab.orignalInstanceId);
            instanceIdToSection.TryAdd(tunnelSectionsByInstanceID[^1], prefab);
        });
        tunnelSections.Clear();
        tunnelSections = null;

        deadEndPlug.Build(this);
        instanceIdToSection.TryAdd(deadEndPlug.orignalInstanceId, deadEndPlug);

        sectionGraveYard = new GameObject("Mothballed Sections").transform;
        sectionGraveYard.parent = transform;
        sectionGraveYard.localPosition = Vector3.zero;

        SetUpBurstDataStructures(tunnelSectionsByInstanceID);
        SetUpBurstMatrices(tunnelSectionsByInstanceID, Allocator.Persistent);
    }

    private void Start()
    {
        // InputManager.Instance.interactButton.OnButtonReleased += PlaceStagnationBeacon;

        tunnelSectionLayerIndex = tunnelSectionLayerMask.value;
        transform.position = Vector3.zero;

#if UNITY_EDITOR
        if (debugging)
        {
            Random.state = seed;
            Debugging();
            return;
        }
#endif
        if (!randomSeed)
        {
            Random.state = seed;
        }
        GenerateInitialArea();
    }

    private void OnDestroy()
    {
        CleanUpBurstDataStructures();
    }

    /// <summary>
    /// Initilises a lot of data structures used by the burst compiled job <see cref="BigMatrixJob"/>
    /// </summary>
    /// <param name="nextSections"></param>
    private void SetUpBurstDataStructures(List<int> nextSections)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        sectionConnectorContainers = new(nextSections.Count, Allocator.Persistent);
        sectionBoxMatrices = new(nextSections.Count, Allocator.Persistent);

        for (int i = 0; i < nextSections.Count; i++)
        {
            int id = nextSections[i];
            TunnelSection section = instanceIdToSection[id];

            // convert bounding box data to transform matrix (float4x4) to avoid unnessecary matrix calculations during map updates.
            UnsafeList<float4x4> bounds = new(section.boundingBoxes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            bounds.Resize(section.boundingBoxes.Length, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < section.boundingBoxes.Length; j++)
            {
                bounds[j] = section.boundingBoxes[j].Matrix;
            }
            sectionBoxMatrices.Add(id, bounds);

            // convert Connector data to BurstConnector to avoid unnessecary matrix calculations during map updates.
            UnsafeList<BurstConnector> connectors = new(section.connectors.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            connectors.Resize(section.connectors.Length, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < section.connectors.Length; j++)
            {
                connectors[j] = new(section.connectors[j]);
            }
            sectionConnectorContainers.Add(id, connectors);
        }

        new SetupConnectorsJob
        {
            sectionIds = new NativeArray<int>(nextSections.ToArray(), Allocator.TempJob),
            sectionConnectors = sectionConnectorContainers
        }.ScheduleParallel(nextSections.Count, 8, new JobHandle()).Complete();
        if (mapProfiling) Debug.LogFormat("Initial Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);

    }

    private void SetUpBurstMatrices(List<int> sections, Allocator allocator)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        sectionBoxTransforms = new(sections.Count, allocator);
        for (int i = 0; i < sections.Count; i++)
        {
            int id = sections[i];
            TunnelSection section = instanceIdToSection[id];

            UnsafeList<UnsafeList<BoxTransform>> boxTransforms = new(section.connectors.Length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < section.connectors.Length; j++)
            {
                /// the boxes are left as Uninitialized - don't read from them before running <see cref="BigMatrixJob"/> hehe
                UnsafeList<BoxTransform> boxes = new(section.boundingBoxes.Length, allocator, NativeArrayOptions.UninitializedMemory);
                boxes.Resize(section.boundingBoxes.Length, NativeArrayOptions.UninitializedMemory);
                boxTransforms.AddNoResize(boxes);
            }
            sectionBoxTransforms.Add(id, boxTransforms);
        }
        if (mapProfiling) Debug.LogFormat("Matrix Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
    }

    /// <summary>
    /// Safely disposes of all burst data structures to prevent memory leaks when the application ends.
    /// </summary>
    private void CleanUpBurstDataStructures()
    {
        tunnelSectionsByInstanceID.ForEach(id =>
        {
            sectionBoxMatrices[id].Dispose();
            sectionConnectorContainers[id].Dispose();
            UnsafeList<UnsafeList<BoxTransform>> boxTransforms = sectionBoxTransforms[id];
            for (int i = 0; i < boxTransforms.Length; i++)
            {
                boxTransforms[i].Dispose();
            }
            boxTransforms.Dispose();
        });
        sectionBoxTransforms.Dispose();
        sectionBoxMatrices.Dispose();
        sectionConnectorContainers.Dispose();
    }

    private void GenerateInitialArea()
    {
        seed = Random.state;
        if (startSection == null)
        {
            int spawnIndex = Random.Range(0, tunnelSectionsByInstanceID.Count);
            curPlayerSection = InstinateSection(spawnIndex);
        }
        else
        {
            curPlayerSection = InstinateSection(startSection);
        }
        curPlayerSection.transform.position = new Vector3(0, 0, 0);
        mapTree.Add(new() { curPlayerSection });

        AmbientLightController.Instance.FadeAmbientLight(curPlayerSection.AmbientLightLevel);

        double startTime = Time.realtimeSinceStartupAsDouble;
        rejectBreakableWallAtConnections = true;
        RecursiveBuilder(true);
        if (mapProfiling) Debug.LogFormat("Map Update Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
        OnMapUpdate?.Invoke();
    }
}
