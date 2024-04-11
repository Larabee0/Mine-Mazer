using MazeGame.Input;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class SpatialParadoxGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private TunnelSection startSection;
    [SerializeField] private TunnelSection deadEndPlug;
    public int DeadEndPlugInstanceId =>deadEndPlug.orignalInstanceId;
    [SerializeField] private BreakableWall breakableWall;
    [SerializeField] private List<TunnelSection> tunnelSections;
    [SerializeField] private MapResource[] decorations;
    [SerializeField] private float decorCoverage;
    [SerializeField] private int totalDecorations;
    [SerializeField] private GameObject stagnationBeacon;

    private Dictionary<int, TunnelSection> instanceIdToSection;
    private Dictionary<int, BakedTunnelSection> instanceIdToBakedData;
    private List<int> tunnelSectionsByInstanceID;


    [Header("Runtime Map")]
    [SerializeField] private MapTreeElement curPlayerSection;
    [SerializeField] private List<List<MapTreeElement>> mapTree = new();
    // private Dictionary<int, int> sectionCounter = new();
    private Dictionary<MapTreeElement, SectionDstData> mothBalledSections = new();
    private Dictionary<int, List<MapTreeElement>> promoteSectionsDict = new();
    private List<MapTreeElement> promoteSectionsList = new();

    private List<MapTreeElement> deadEnds = new();

    private int reRingInters = 0;
    private Transform sectionGraveYard; 
    private Transform originalInstances; 
    private Coroutine mapUpdateProcess;
    
    private PlayerExplorationStatistics explorationStatistics;
    public PlayerExplorationStatistics ExplorationStatistics => explorationStatistics;

    public Pluse OnMapUpdate;
    public Pluse OnEnterLadderSection;
    public Pluse OnEnterColonySection;
    public List<List<MapTreeElement>> MapTree => mapTree;
    public int TotalSections
    {
        get
        {
            int size = 0;
            mapTree.ForEach(branch => size += branch.Count);
            return size;
        }
    }
    public MapTreeElement CurPlayerSection => curPlayerSection;

    // these are quite unsafe lol
    // Its fine, once initilised their capacity doesn't change and their values are accessed safely by seperate threads.
    // the keys for these parallel hash maps are the prefab instance ids of the sections.
    UnsafeParallelHashMap<int, UnsafeList<UnsafeList<BoxTransform>>> sectionBoxTransforms; // result of box tranform when the map looks to position a new section.
    NativeParallelHashMap<int, UnsafeList<BurstConnector>> sectionConnectorContainers; // connector structs for each section - these all have an identity matrix applied.
    NativeParallelHashMap<int, UnsafeList<float4x4>> sectionBoxMatrices; // local matrices of the bounding boxes of the section

    NativeParallelHashMap<int, UnsafeList<InstancedBox>> incomingBoxBounds; // InstancedBox contains 2 unsafe lists for corners (8) and normals(6)
    UnsafeList<TunnelSectionVirtual> VirtualPhysicsWorld; // TunnelSectionVirtual contains an unsafe list of InstancedBoxes
    private HashSet<int> virtualPhysicsWorldIds = new();

    private MapTreeElement nextPlayerSection;
    private Coroutine incrementalUpdateProcess;
    private Coroutine queuedUpdateProcess;

    private List<MapTreeElement> preProcessingQueue = new();
    private Dictionary<int, MapTreeElement> processingQueue = new();
    private List<MapTreeElement> postProcessingQueue = new();

    public bool SectionsInProcessingQueue => preProcessingQueue.Count > 0 || processingQueue.Count > 0 || postProcessingQueue.Count > 0;

    [Header("Generation Settings")]
    [SerializeField] private int ringRenderDst = 3; 
    [SerializeField, Min(1)] private int maxDst = 3;
    [SerializeField, Range(0, 1)] private float breakableWallAtConnectionChance = 0.5f;
    [SerializeField, Range(0, 1)] private float sanctumPartSpawnChance = 0.5f;
    private int sanctumPartCooldown;
    [Space]
    [SerializeField] private LayerMask tunnelSectionLayerMask;
    [SerializeField, Min(1000)] private int maxInterations = 1000000; /// max iterations allowed for <see cref="PickSection(TunnelSection, List{int}, out Connector, out Connector)"/>
    [SerializeField] private bool randomSeed = true; // generate a new seed every application run or use old seed.
    [SerializeField] private uint seed; // override seed.
    
    private Random randomNG;
    [SerializeField] private bool mapProfiling = false;
    [SerializeField] private bool newDataSystem = false;
    private MapTreeElement lastEnter;
    private MapTreeElement lastExit;
    private bool forceBreakableWallAtConnections = false;
    private bool rejectBreakableWallAtConnections = false;


    private void Awake()
    {



        if (newDataSystem)
        {
            NewSectionBuild();
        }
        else
        {
            OldSectionBuild();
        }
        

        sectionGraveYard = new GameObject("Mothballed Sections").transform;
        sectionGraveYard.parent = transform;
        sectionGraveYard.localPosition = Vector3.zero;


        SetUpBurstDataStructures(tunnelSectionsByInstanceID);
        SetUpBurstMatrices(tunnelSectionsByInstanceID, Allocator.Persistent);
    }

    private void NewSectionBuild()
    {
        this.originalInstances = new GameObject("Original Instances").transform;
        this.originalInstances.parent = transform;
        this.originalInstances.localPosition = Vector3.zero;
        tunnelSections.ForEach(section => section.Build(this));
        instanceIdToSection = new Dictionary<int, TunnelSection>(tunnelSections.Count);
        instanceIdToBakedData = new Dictionary<int, BakedTunnelSection>(tunnelSections.Count);
        tunnelSectionsByInstanceID = new(tunnelSections.Count);

        List<TunnelSection> originalInstances = new(tunnelSections.Count);
        List<BakedTunnelSection> bakedData = new(tunnelSections.Count);
        for (int i = 0; i < tunnelSections.Count; i++)
        {
            tunnelSections[i].GenerateNavMeshLinks();
            originalInstances.Add(Instantiate(tunnelSections[i],this.originalInstances));
            originalInstances[i].gameObject.SetActive(false);
            originalInstances[i].DataFromBake = null;
            Destroy(originalInstances[i].GetComponent<SectionSpawnBaseRule>());

            bakedData.Add(originalInstances[i].GetComponent<TunnelSectionData>().bakedData);
            bakedData[i].SetSpawnRule(tunnelSections[i].GetComponent<SectionSpawnBaseRule>());
            bakedData[i].Build(this, tunnelSections[i].orignalInstanceId);
            Destroy(originalInstances[i].GetComponent<TunnelSectionData>());
            
            tunnelSectionsByInstanceID.Add(tunnelSections[i].orignalInstanceId);
            instanceIdToSection.TryAdd(tunnelSectionsByInstanceID[^1], originalInstances[i]);
            instanceIdToBakedData.TryAdd(tunnelSectionsByInstanceID[^1], bakedData[i]);
        }

        tunnelSections.Clear();
        tunnelSections = null;

        originalInstances.Add(Instantiate(deadEndPlug, this.originalInstances));
        originalInstances[^1].gameObject.SetActive(false);
        Destroy(originalInstances[^1].GetComponent<TunnelSectionData>());

        bakedData.Add(deadEndPlug.GetComponent<TunnelSectionData>().bakedData);
        bakedData[^1].Build(this, deadEndPlug.orignalInstanceId);
        //tunnelSectionsByInstanceID.Add(originalInstances[^1].orignalInstanceId);

        instanceIdToSection.TryAdd(originalInstances[^1].orignalInstanceId, originalInstances[^1]);
        instanceIdToBakedData.TryAdd(originalInstances[^1].orignalInstanceId, bakedData[^1]);
        deadEndPlug = originalInstances[^1];
    }


    private void OldSectionBuild()
    {
        // construct instance Id based data structures so we aren't doing reference based look-ups.
        instanceIdToSection = new Dictionary<int, TunnelSection>(tunnelSections.Count);
        instanceIdToBakedData = new Dictionary<int, BakedTunnelSection>(tunnelSections.Count);
        tunnelSectionsByInstanceID = new(tunnelSections.Count);
        tunnelSections.ForEach(prefab =>
        {
            prefab.DataFromBake = prefab.GetComponent<TunnelSectionData>().bakedData;
            prefab.DataFromBake.excludeConnectorSections.ForEach(connector => connector.Build());
            prefab.DataFromBake.SetSpawnRule(prefab.GetComponent<SectionSpawnBaseRule>());
            prefab.DataFromBake.Build(this, prefab.GetInstanceID(), true);
            prefab.Build(this);
            tunnelSectionsByInstanceID.Add(prefab.orignalInstanceId);
            instanceIdToSection.TryAdd(tunnelSectionsByInstanceID[^1], prefab);
            instanceIdToBakedData.TryAdd(tunnelSectionsByInstanceID[^1], prefab.DataFromBake);
        });
        tunnelSections.Clear();
        tunnelSections = null;
        OldBakeDataForSpecificSection(deadEndPlug);
    }

    private void OldBakeDataForSpecificSection(TunnelSection section)
    {
        section.DataFromBake = section.GetComponent<TunnelSectionData>().bakedData;
        section.DataFromBake.Build(this, section.GetInstanceID(), true);
        section.Build(this);
        instanceIdToSection.TryAdd(section.orignalInstanceId, section);
        instanceIdToBakedData.TryAdd(section.orignalInstanceId, section.DataFromBake);
    }

    private void Start()
    {
        transform.position = Vector3.zero;

#if UNITY_EDITOR
        if (debugging)
        {
            randomNG = new(math.max(seed, 1));
            Debugging();
            return;
        }
#endif
        if (randomSeed)
        {
            seed = unchecked((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        randomNG = new(math.max(seed,1));
        GenerateInitialArea();
    }

    private void OnDestroy()
    {
        CleanUpBurstDataStructures();
    }

    /// <summary>
    /// Initilises a lot of data structures used by the burst compiled job <see cref="global::BigMatrixJob"/>
    /// </summary>
    /// <param name="nextSections"></param>
    private void SetUpBurstDataStructures(List<int> nextSections)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;

        VirtualPhysicsWorld = new(nextSections.Count, Allocator.Persistent);
        sectionConnectorContainers = new(nextSections.Count, Allocator.Persistent);
        sectionBoxMatrices = new(nextSections.Count, Allocator.Persistent);
        incomingBoxBounds = new(nextSections.Count, Allocator.Persistent);

        SetupSectionStructures(nextSections);
        if (!SetupConns)
        {
            new SetupConnectorsJob
            {
                sectionIds = new NativeArray<int>(nextSections.ToArray(), Allocator.TempJob),
                sectionConnectors = sectionConnectorContainers
            }.Schedule(nextSections.Count,new JobHandle()).Complete();
        }
        else
        {
            new SetupConnectorsJob
            {
                sectionIds = new NativeArray<int>(nextSections.ToArray(), Allocator.TempJob),
                sectionConnectors = sectionConnectorContainers
            }.ScheduleParallel(nextSections.Count, 8, new JobHandle()).Complete();
        }

        if (mapProfiling) Debug.LogFormat("Initial Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);

    }

    private void SetupSectionStructures(List<int> nextSections)
    {
        for (int i = 0; i < nextSections.Count; i++)
        {
            int id = nextSections[i];
            BakedTunnelSection dataFromBake = instanceIdToBakedData[id];
            CreateTransformMatrices(id, dataFromBake);
            CreateBurstConnectors(id, dataFromBake);
        }
    }

    private void CreateTransformMatrices(int id, BakedTunnelSection dataFromBake)
    {
        // convert bounding box data to transform matrix (float4x4) to avoid unnessecary matrix calculations during map updates.
        // Debug.LogFormat("{0} {1}", id, section.gameObject.name);

        UnsafeList<float4x4> bounds = new(dataFromBake.boundingBoxes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        UnsafeList<InstancedBox> instancedBoxes = new(dataFromBake.boundingBoxes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        bounds.Resize(dataFromBake.boundingBoxes.Length, NativeArrayOptions.UninitializedMemory);
        instancedBoxes.Resize(dataFromBake.boundingBoxes.Length, NativeArrayOptions.UninitializedMemory);
        for (int j = 0; j < dataFromBake.boundingBoxes.Length; j++)
        {
            bounds[j] = dataFromBake.boundingBoxes[j].LocalMatrix;
            instancedBoxes[j] = new(dataFromBake.boundingBoxes[j]);
        }
        incomingBoxBounds.Add(id, instancedBoxes);
        sectionBoxMatrices.Add(id, bounds);
    }

    private void CreateBurstConnectors(int id, BakedTunnelSection dataFromBake)
    {
        // convert Connector data to BurstConnector to avoid unnessecary matrix calculations during map updates.
        UnsafeList<BurstConnector> connectors = new(dataFromBake.connectors.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        connectors.Resize(dataFromBake.connectors.Length, NativeArrayOptions.UninitializedMemory);
        for (int j = 0; j < dataFromBake.connectors.Length; j++)
        {
            connectors[j] = new(dataFromBake.connectors[j]);
        }
        sectionConnectorContainers.Add(id, connectors);
    }

    private void SetUpBurstMatrices(List<int> sections, Allocator allocator)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        sectionBoxTransforms = new(sections.Count, allocator);
        for (int i = 0; i < sections.Count; i++)
        {
            int id = sections[i];
            BakedTunnelSection dataFromBake = instanceIdToBakedData[id];

            UnsafeList<UnsafeList<BoxTransform>> boxTransforms = new(dataFromBake.connectors.Length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < dataFromBake.connectors.Length; j++)
            {
                /// the boxes are left as Uninitialized - don't read from them before running <see cref="BigMatrix"/> hehe
                UnsafeList<BoxTransform> boxes = new(dataFromBake.boundingBoxes.Length, allocator, NativeArrayOptions.UninitializedMemory);
                boxes.Resize(dataFromBake.boundingBoxes.Length, NativeArrayOptions.UninitializedMemory);
                boxTransforms.AddNoResize(boxes);
            }
            sectionBoxTransforms.Add(id, boxTransforms);
        }
        if (mapProfiling)
        {
            Debug.LogFormat("Matrix Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
        }
    }

    /// <summary>
    /// Safely disposes of all burst data structures to prevent memory leaks when the application ends.
    /// </summary>
    private void CleanUpBurstDataStructures()
    {
        tunnelSectionsByInstanceID.ForEach(id =>
        {
            UnsafeList<InstancedBox> instancedBoxes =  incomingBoxBounds[id];
            for (int i = 0; i < instancedBoxes.Length; i++)
            {
                instancedBoxes[i].Dispose();
            }
            instancedBoxes.Dispose();
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
        incomingBoxBounds.Dispose();

        for (int i = 0; i < VirtualPhysicsWorld.Length; i++)
        {
            VirtualPhysicsWorld[i].Dispose();
        }

        VirtualPhysicsWorld.Dispose();
    }

    private void GenerateInitialArea()
    {
        if (startSection == null)
        {
            int spawnIndex = randomNG.NextInt(0, tunnelSectionsByInstanceID.Count);



            curPlayerSection = new()
            {
                sectionInstance = InstinateSection(spawnIndex)
            };
            curPlayerSection.dataFromBake = instanceIdToBakedData[curPlayerSection.OriginalInstanceId];
        }
        else
        {
            curPlayerSection = new()
            {
                sectionInstance = InstinateSection(instanceIdToSection[startSection.orignalInstanceId])
            };
            curPlayerSection.dataFromBake = instanceIdToBakedData[curPlayerSection.OriginalInstanceId];
        }
        curPlayerSection.sectionInstance.transform.position = new Vector3(0, 0, 0);
        curPlayerSection.UID = curPlayerSection.sectionInstance.GetInstanceID();
        curPlayerSection.sectionInstance.treeElementOwner = curPlayerSection;
        
        AddSection(curPlayerSection.sectionInstance, float4x4.TRS(curPlayerSection.LocalToWorld.Translation(), curPlayerSection.LocalToWorld.Rotation(), Vector3.one));
        
        virtualPhysicsWorldIds.Add(curPlayerSection.UID);

        rejectBreakableWallAtConnections = true;

        mapTree.Add(new() { curPlayerSection });

        StartCoroutine(IncrementalBuilder(true));

        if (!runPostProcessLast)
        {
            StartCoroutine(PostProcessQueueInfinite());
        }

        AmbientController.Instance.FadeAmbientLight(curPlayerSection.sectionInstance.AmbientLightLevel);
    }
}
