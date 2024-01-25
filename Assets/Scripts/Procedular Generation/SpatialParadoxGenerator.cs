using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpatialParadoxGenerator : MonoBehaviour
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
    private int tunnelSectionLayerIndex;
    private TunnelSection lastEnter;
    private TunnelSection lastExit;
    private bool forceBreakableWallAtConnections = false;
    private bool rejectBreakableWallAtConnections = false;

#if UNITY_EDITOR
    // extra variables and settings for the debugger, these aren't compiled into the build.
    [Header("Debug")]
    [SerializeField] private TunnelSection prefab1;
    [SerializeField] private TunnelSection prefab2;
    [HideInInspector] public bool debugging = false;
    [HideInInspector] public bool initialAreaDebugging = false;
    [HideInInspector] public bool transformDebugging = false;
    [SerializeField, Min(0.5f)] private float intersectTestHoldTime = 2.5f;
    [SerializeField, Min(0.5f)] private float distanceListPauseTime = 5f;
    [SerializeField, Min(0.5f)] private float transformHoldTime = 2.5f;
    [SerializeField] private GameObject santiziedCube;
    private GameObject primaryObj;
    private GameObject secondaryObj;

    private TunnelSection targetSectionDebug;
    private Connector primaryPreferenceDebug;
    private Connector secondaryPreferenceDebug;
    private bool intersectionTest;

#endif
     
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

    /// <summary>
    /// Places or removes a stagnation beacon in the current player section <see cref="curPlayerSection"/>
    /// This toggles a flag in the <see cref="TunnelSection"/> instance called "keep".
    /// </summary>
    /// <param name="context"></param>
    private void PlaceStagnationBeacon()
    {
        NPC_Interact player = explorationStatistics.GetComponent<NPC_Interact>();
        if (player.HitInteractable)
        {
            return;
        }
        if (curPlayerSection.Keep && !curPlayerSection.StrongKeep)
        {
            curPlayerSection.Keep = false;
            Destroy(curPlayerSection.stagnationBeacon);
        }
        else if (!curPlayerSection.StrongKeep)
        {
            curPlayerSection.Keep = true;
            Transform playerTransform = player.transform;
            curPlayerSection.stagnationBeacon = Instantiate(stagnationBeacon, playerTransform.position- new Vector3(0,0.6f,0f), playerTransform.rotation, curPlayerSection.transform);
        }
        OnMapUpdate?.Invoke();
    }

    public void PlaceStatnationBeacon(TunnelSection section,StagnationBeacon beacon)
    {
        if(section.stagnationBeacon == null)
        {
            beacon.transform.parent = section.transform;
            beacon.targetSection = section;
            section.stagnationBeacon = beacon.gameObject;
            section.Keep = true;
            OnMapUpdate?.Invoke();
        }
    }

    public void RemoveStagnationBeacon(StagnationBeacon beacon)
    {
        if(beacon.targetSection.stagnationBeacon == beacon)
        {
            beacon.targetSection.Keep = false;
            beacon.targetSection.stagnationBeacon = null;
            beacon.targetSection = null;
            OnMapUpdate?.Invoke();
        }
    }

    public void GetPlayerExplorationStatistics()
    {
        explorationStatistics = FindObjectOfType<PlayerExplorationStatistics>();
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

        Debug.LogFormat("Initial Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);

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
        Debug.LogFormat("Matrix Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
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

#if UNITY_EDITOR

    private void Debugging()
    {
        if (santiziedCube == null)
        {
            Debug.LogWarning("Santizied cube unassigned!");
            return;
        }
        if (initialAreaDebugging)
        {
            Random.state = seed;
            StartCoroutine(GenerateInitialAreaDebug());
        }
        else if (transformDebugging)
        {
            if (prefab1 == null || prefab2 == null)
            {
                Debug.LogWarning("prefab not assigned!");
            }
            StartCoroutine(TransformDebugging());
        }
        else
        {
            Random.state = seed;
            GenerateInitialArea();
        }
    }

    /// <summary>
    /// Visually tries every connector combination between <see cref="prefab1"/> and <see cref="prefab2"/>
    /// This runs forever.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TransformDebugging()
    {
        curPlayerSection = InstinateSection(prefab1);
        curPlayerSection.transform.position = new Vector3(0, 0, 0);
        TunnelSection newSection = InstinateSection(prefab2);

        yield return new WaitForSeconds(transformHoldTime);
        primaryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        secondaryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        while (true)
        {
            for (int i = 0; i < curPlayerSection.connectors.Length; i++)
            {
                Connector primaryConnector = curPlayerSection.connectors[i];

                curPlayerSection.connectors[i] = primaryConnector;
                for (int j = 0; j < newSection.connectors.Length; j++)
                {
                    Connector secondaryConnector = newSection.connectors[j];
                    primaryConnector.UpdateWorldPos(curPlayerSection.transform.localToWorldMatrix);
                    secondaryConnector.UpdateWorldPos(curPlayerSection.transform.localToWorldMatrix);
                    newSection.connectors[j] = secondaryConnector;
                    float4x4 matix = CalculateSectionMatrix(primaryConnector, secondaryConnector);
                    newSection.transform.SetPositionAndRotation(matix.Translation(), matix.Rotation());
                    Debug.LogFormat("i = {0} j = {1}", i, j);
                    primaryObj.transform.SetPositionAndRotation(primaryConnector.position, primaryConnector.localRotation);
                    yield return new WaitForSeconds(transformHoldTime);
                    newSection.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
            }
            newSection.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    /// <summary>
    /// slow step through map generation to allow visualisation.
    /// Has side effect to force physics ticks between intersection tests.
    /// </summary>
    /// <returns></returns>
    private IEnumerator GenerateInitialAreaDebug()
    {
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
        yield return RecursiveBuilderDebug();
        yield return new WaitForSeconds(distanceListPauseTime * 2);

        Debug.Log("Ended Initial Area Debug");
        OnMapUpdate?.Invoke();
    }

    /// <summary>
    /// Slow step through increasing the <see cref="mapTree"/> until it is the size of maxDst.
    /// </summary>
    /// <returns></returns>
    private IEnumerator RecursiveBuilderDebug()
    {
        while (mapTree.Count <= maxDst)
        {
            yield return new WaitForSeconds(distanceListPauseTime);
            List<TunnelSection> startSections = mapTree[^1];

            mapTree.Add(new());

            yield return FillSectionConnectorsDebug(startSections);
        }
    }

    /// <summary>
    /// Connectors a new or mothballed section to every free connector of <see cref="startSections"/>
    /// This is the debug version that waits between operations and contains extra logging information.
    /// </summary>
    /// <param name="startSections"></param>
    /// <returns></returns>
    private IEnumerator FillSectionConnectorsDebug(List<TunnelSection> startSections)
    {
        /// promoteSectionsDict contains items when <see cref="CheckForSectionsPromotions"/> is called and finds mothballed sections that can be promoted.
        /// the dictionary much contain the key corrisponding the to current map ring index in order to be delt with this cycle.
        if (promoteSectionsDict.Count > 0 && promoteSectionsDict.ContainsKey(mapTree.Count - 1))
        {
            Debug.LogFormat(gameObject, "Attempting to Promotion {0} rings queued", promoteSectionsDict.Count);
            // calculates number of free connectors on the previous orbital.
            int freeConnectors = GetFreeConnectorCount(startSections);

            // if there isn't enough free connectors for the number of sections we need to promote, then the previous ring needs to be regenerated.
            // this is done by calling RegenRing with the index before this one.
            if (freeConnectors < promoteSectionsDict[mapTree.Count - 1].Count)
            {
                // not enough free connectors, go in a level and regenerate.
                Debug.LogWarningFormat("Re-Gen ring {0} as does not have enough free connectors.", mapTree.Count - 2);
                yield return RegenRingDebug(mapTree.Count - 2);
                yield break;
            }
            else
            {
                /// connectors allowing, the sections to be promoted get copied to <see cref="promoteSectionsList"/> then removed from the dictionary.
                /// <see cref="PickInstinateConnectDebug(TunnelSection)"/> will deal with the rest of the heavy lifting.
                Debug.LogFormat(gameObject, "Enough free connectors exist for ring {1}, queuing promotion of {0} sections", promoteSectionsDict[mapTree.Count - 1].Count, mapTree.Count - 1);
                promoteSectionsList.AddRange(promoteSectionsDict[mapTree.Count - 1]);
                promoteSectionsDict.Remove(mapTree.Count - 1);
            }
        }

        for (int i = 0; i < startSections.Count; i++)
        {
            TunnelSection section = startSections[i];
            int freeConnectors = section.connectors.Length - section.InUse.Count;
            for (int j = 0; j < freeConnectors; j++)
            {
                // pick a new section to connect to
                yield return PickInstinateConnectDebug(section);
            }
        }

        if (promoteSectionsList.Count > 0 && mapTree.Count <= maxDst)
        {
            Debug.LogWarningFormat(gameObject, "{0} mothballed sections weren't added! Adding to next level", promoteSectionsList.Count);
            if (promoteSectionsDict.ContainsKey(mapTree.Count))
            {
                promoteSectionsDict[mapTree.Count].AddRange(promoteSectionsList);
            }
            else
            {
                promoteSectionsDict.Add(mapTree.Count, new(promoteSectionsList));
            }
            promoteSectionsList.Clear();
        }
        //Debug.LogFormat(gameObject, "promoteList: {0} promoteDict: {1}", promoteSectionsList.Count, promoteSectionsDict.Count);
        if (GetFreeConnectorCount(mapTree[^1]) == 0)
        {
            yield return RegenRingDebug(mapTree.Count - 2);
            yield break;
        }
        if (ringRenderDst < maxDst && mapTree.Count > ringRenderDst)
        {
            mapTree[^1].ForEach(section => section.SetRenderersEnabled(false));
        }
    }

    private IEnumerator RegenRingDebug(int regenTarget)
    {
        //yield return BreakEditor();
        while (mapTree.Count - 1 != regenTarget)
        {
            Debug.LogFormat(gameObject, "Cleaning up {0} sections", mapTree[^1].Count);
            for (int i = 0; i < mapTree[^1].Count; i++)
            {
                yield return new WaitForSeconds(intersectTestHoldTime);
                TunnelSection section = mapTree[^1][i];
                if (section.Keep)
                {
                    section.gameObject.SetActive(false);
                    section.transform.parent = sectionGraveYard;
                    ClearConnectors(section);
                    mothBalledSections.Add(section, new(math.distancesq(curPlayerSection.Position, section.Position), mapTree.Count - 1));
                }
                else
                {
                    DestroySection(section);
                }
            }
            mapTree.RemoveAt(mapTree.Count - 1);
        }
        Physics.SyncTransforms();
        CheckForSectionsPromotions();
        Debug.Log("Begining tree re-gen..");
        yield return RecursiveBuilderDebug();
    }

    private IEnumerator MakeRootNodeDebug(TunnelSection newRoot)
    {
        Debug.Log("Begin Root Node Update");

        UpdateMothBalledSections(newRoot);

        List<List<TunnelSection>> newTree = new() { new() { newRoot } };
        HashSet<TunnelSection> exceptWith = new(newTree[^1]);
        yield return new WaitForSeconds(intersectTestHoldTime);
        
        Debug.Log("Building new Tree..");
        RecursiveTreeBuilder(newTree, exceptWith);

        Debug.LogFormat("New Tree Size {0}", newTree.Count);
        Debug.LogFormat("Original Tree Size {0}", mapTree.Count);
        bool forceGrow = newTree.Count < mapTree.Count;

        yield return new WaitForSeconds(intersectTestHoldTime);

        Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
                yield return new WaitForSeconds(intersectTestHoldTime);
                TunnelSection section = newTree[^1][i];
                if (section.Keep)
                {
                    section.gameObject.SetActive(false);
                    section.transform.parent = sectionGraveYard;
                    if (!mothBalledSections.ContainsKey(section))
                    {
                        ClearConnectors(section);
                        mothBalledSections.Add(section, new(math.distancesq(newRoot.Position, section.Position), newTree.Count - 1));
                    }
                }
                else
                {
                    leafCounter++;
                    DestroySection(section);
                }
            }
            newTree.RemoveAt(newTree.Count - 1);
        }
        Physics.SyncTransforms();
        Debug.LogFormat("Pruned {0} leaves", leafCounter);
        yield return new WaitForSeconds(intersectTestHoldTime);

        mapTree.Clear();
        mapTree.AddRange(newTree);

        Debug.Log("Growing Tree..");
        int oldSize = 0;

        if (forceGrow)
        {
            yield return RecursiveBuilderDebug();
        }
        else
        {
            oldSize = mapTree[^1].Count;
            yield return FillSectionConnectorsDebug(mapTree[^2]);
        }

        
        Debug.LogFormat("Grew {0} leaves", mapTree[^1].Count - oldSize);
        curPlayerSection = newTree[0][0];
        if (curPlayerSection == null)
        {
            ResolvePlayerSection();
        }

        if (ringRenderDst < maxDst)
        {
            for (int i = 0; i < ringRenderDst; i++)
            {
                mapTree[i].ForEach(section => section.SetRenderersEnabled(true));
            }
        }
    }

    private IEnumerator PickInstinateConnectDebug(TunnelSection primary)
    {
        TunnelSection pickedInstance = null;
        Connector priPref = Connector.Empty, secPref = Connector.Empty;
        if (promoteSectionsList.Count > 0)
        {
            List<int> internalSections = new(promoteSectionsList.Count);
            promoteSectionsList.ForEach(section => internalSections.Add(section.orignalInstanceId));
            yield return PickSectionDebug(primary, internalSections);
            if (!internalSections.Contains(targetSectionDebug.GetInstanceID())) // returned dead end.
            {
                List<int> nextSections = FilterSections(primary);
                yield return PickSectionDebug(primary, nextSections);
                pickedInstance = InstinateSection(targetSectionDebug);
            }
            else // reinstance section
            {
                int index = internalSections.IndexOf(targetSectionDebug.GetInstanceID());
                pickedInstance = promoteSectionsList[index];
                pickedInstance.gameObject.SetActive(true);
                promoteSectionsList.RemoveAt(index);
            }
        }
        else
        {
            List<int> nextSections = FilterSections(primary);
            yield return PickSectionDebug(primary, nextSections);
            pickedInstance = InstinateSection(targetSectionDebug);
        }

        TransformSection(primary, pickedInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // transform the new section
        Physics.SyncTransforms(); // push changes to physics world now instead of next fixed update

        mapTree[^1].Add(pickedInstance); // add this to 2 back
    }

    private IEnumerator PickSectionDebug(TunnelSection primary, List<int> nextSections)
    {
        primaryPreferenceDebug = Connector.Empty;
        secondaryPreferenceDebug = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);
        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.Persistent);
        int iterations = maxInterations;

        targetSectionDebug = null;

        while (targetSectionDebug == null && primaryConnectors.Count > 0)
        {
            primaryPreferenceDebug = GetConnectorFromSection(primaryConnectors, out int priIndex);

            double startTime = Time.realtimeSinceStartupAsDouble;
            NativeReference<BurstConnector> priConn = new(new(primaryPreferenceDebug), Allocator.TempJob);

            JobHandle handle = new BurstConnectorMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());
            handle = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectorContainers,
                boxBounds = sectionBoxMatrices,
                matrices = sectionBoxTransforms
            }.Schedule(nextSections.Count, handle);
            priConn.Dispose(handle).Complete();
            Debug.LogFormat("Big Matrix: {1} Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f, nextSections.Count);

            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(primaryPreferenceDebug), nextSections);
            while (internalNextSections.Count > 0)
            {
                int curInstanceID = internalNextSections.ElementAt(Random.Range(0, internalNextSections.Count));
                targetSectionDebug = instanceIdToSection[curInstanceID];
                yield return RunIntersectionTestsDebug(primary, targetSectionDebug);
                if (intersectionTest)
                {
                    break;
                }
                internalNextSections.Remove(curInstanceID);
                targetSectionDebug = null;
                iterations--;
                if (iterations <= 0)
                {
                    Debug.LogException(new System.StackOverflowException("Intersection test exceeded max iterations"), this);
                }
            }
            if (targetSectionDebug != null)
            {
                break;
            }
            primaryConnectors.RemoveAt(priIndex);
        }

        nativeNexSections.Dispose();

        if (targetSectionDebug == null)
        {
            targetSectionDebug = deadEndPlug;
            secondaryPreferenceDebug = deadEndPlug.connectors[0];
            secondaryPreferenceDebug.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            Debug.LogWarning("Unable to find usable section, ending the tunnel.", primary);
        }
        yield return null;
    }

    private IEnumerator RunIntersectionTestsDebug(TunnelSection primary, TunnelSection target)
    {
        secondaryPreferenceDebug = Connector.Empty;
        List<Connector> secondaryConnectors = FilterConnectors(target);

        while (secondaryConnectors.Count > 0)
        {
            secondaryPreferenceDebug = GetConnectorFromSection(secondaryConnectors, out int secIndex);
            yield return DebugIntersectionTestBurst(primary, target);
            if (intersectionTest)
            {
                yield break;
            }
            secondaryConnectors.RemoveAt(secIndex);
        }

        intersectionTest = false;
        yield break;
    }

    private IEnumerator DebugIntersectionTestBurst(TunnelSection primary, TunnelSection target)
    {
        NativeReference<Connector> pri = new(primaryPreferenceDebug, Allocator.TempJob);
        NativeReference<Connector> sec = new(secondaryPreferenceDebug, Allocator.TempJob);
        JobHandle handle1 = new ConnectorMulJob { connector = pri, sectionLTW = primary.transform.localToWorldMatrix }.Schedule(new JobHandle());
        JobHandle handle2 = new ConnectorMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryPreferenceDebug = pri.Value;
        secondaryPreferenceDebug = sec.Value;
        pri.Dispose();
        sec.Dispose();

        List<GameObject> objects = new();
        //objects.Add(Instantiate(target,pos,rot).gameObject);
        int instanceID = target.GetInstanceID();
        UnsafeList<UnsafeList<BoxTransform>> transformsContainer = sectionBoxTransforms[instanceID];
        if (transformsContainer.Length == 0)
        {
            Debug.LogError("no transforms found for connector");
            intersectionTest = false;
            yield break;
        }
        if (transformsContainer.Length <= secondaryPreferenceDebug.internalIndex)
        {
            Debug.LogErrorFormat(gameObject, "returned transforms list does not contain index ({0}) for connector", secondaryPreferenceDebug.internalIndex);
        }

        UnsafeList<BoxTransform> transforms = transformsContainer[secondaryPreferenceDebug.internalIndex];
        if (transforms.Length == 0)
        {
            Debug.LogError("no box transforms found");
            yield break;
        }
        bool noIntersections = true;
        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];
            objects.Add(Instantiate(santiziedCube));

            BoxTransform m = transforms[i];
            float3 position = m.pos;
            Quaternion rotation = m.rot;

            objects[^1].transform.SetPositionAndRotation(position, rotation);
            objects[^1].transform.localScale = boxBounds.size;
            objects[^1].GetComponent<MeshRenderer>().material.color = Color.green;
            if (Physics.CheckBox(position, boxBounds.size * 0.5f, rotation, tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
            {
                objects[^1].GetComponent<MeshRenderer>().material.color = Color.red;
                noIntersections = false;
            }
        }

        //objects[0].SetActive(true);

        yield return new WaitForSeconds(intersectTestHoldTime);
        objects.ForEach(ob => Destroy(ob));

        intersectionTest = noIntersections;
    }

    private IEnumerator BreakEditor()
    {
        yield return null;
        yield return null;
        Debug.Break();
    }

#endif

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

        double startTime = Time.realtimeSinceStartupAsDouble;
        rejectBreakableWallAtConnections = true;
        RecursiveBuilder(true);
        Debug.LogFormat("Map Update Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
        OnMapUpdate?.Invoke();
    }

    private void RecursiveBuilder(bool initialArea = false)
    {
        while(mapTree.Count <= maxDst)
        {
            List<TunnelSection> startSections = mapTree[^1];
            mapTree.Add(new());
            FillSectionConnectors(startSections);
            if (initialArea)
            {
                rejectBreakableWallAtConnections = false;
            }
        }
    }

    public void PlayerExitSection(TunnelSection section)
    {
        lastExit = section;

        if (lastEnter != null)
        {
            UpdateMap();
        }
    }

    public void PlayerEnterSection(TunnelSection section)
    {
        lastEnter = section;
        if (!section.explored)
        {
            explorationStatistics.Increment();
        }
        if (lastExit != null)
        {
            UpdateMap();
        }
        if (section.HasLadder)
        {
            OnEnterLadderSection?.Invoke();
        }
        if (section.IsColony)
        {
            OnEnterColonySection?.Invoke();
        }
    }

    private void UpdateMap()
    {
        if (lastExit == curPlayerSection && lastEnter != curPlayerSection && mapUpdateProcess == null)
        {
            mapUpdateProcess = StartCoroutine(DelayedMapGen(lastEnter));
        }
        lastEnter = null;
        lastExit = null;
    }

    private IEnumerator DelayedMapGen(TunnelSection newSection)
    {
        yield return null;
#if UNITY_EDITOR
        if (debugging)
        {
            StartCoroutine(MakeRootNodeDebug(newSection));
        }
        else
        {
            double startTime = Time.realtimeSinceStartupAsDouble;
            MakeRootNode(newSection);
            Debug.LogFormat("Map Update Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
        }
#else
            MakeRootNode(newSection);
#endif
        mapUpdateProcess = null;
        OnMapUpdate?.Invoke();
        //yield return null;
        //Debug.Break();
    }

    private void FillSectionConnectors(List<TunnelSection> startSections)
    {
        if (promoteSectionsDict.Count > 0 && promoteSectionsDict.ContainsKey(mapTree.Count - 1))
        {
            int freeConnectors = GetFreeConnectorCount(startSections);
            if (freeConnectors < promoteSectionsDict[mapTree.Count - 1].Count)
            {
                // not enough free connectors, go in a level and regenerate.
                Debug.LogWarningFormat("Regening ring {0} it does not have enough free connectors.", mapTree.Count - 2);
                RegenRing(mapTree.Count - 2);
                return;
            }
            else
            {
                promoteSectionsList.AddRange(promoteSectionsDict[mapTree.Count - 1]);
                promoteSectionsDict.Remove(mapTree.Count - 1);
            }
        }

        for (int i = 0; i < startSections.Count; i++)
        {
            TunnelSection section = startSections[i];
            int freeConnectors = section.connectors.Length - section.InUse.Count;
            for (int j = 0; j < freeConnectors; j++)
            {
                TunnelSection sectionInstance = PickInstinateConnect(section);
                mapTree[^1].Add(sectionInstance);
            }
        }

        if (promoteSectionsList.Count > 0 && mapTree.Count <= maxDst)
        {
            if (promoteSectionsDict.ContainsKey(mapTree.Count))
            {
                promoteSectionsDict[mapTree.Count].AddRange(promoteSectionsList);
            }
            else
            {
                promoteSectionsDict.Add(mapTree.Count, new(promoteSectionsList));
            }
            promoteSectionsDict.Clear();
        }
        if (GetFreeConnectorCount(mapTree[^1]) == 0)
        {
            RegenRing(mapTree.Count - 2);
            return;
        }
        if (ringRenderDst < maxDst && mapTree.Count > ringRenderDst)
        {
            mapTree[^1].ForEach(section => section.SetRenderersEnabled(false));
        }
    }

    private void RegenRing(int regenTarget)
    {
        regenTarget -= reRingInters;

        if(regenTarget < 2)
        {
            throw new System.InvalidOperationException(string.Format("Regeneration target set to {0} Cannot regenerate root node! Something catastrophic occured!", regenTarget));
        }

        while (mapTree.Count - 1 != regenTarget)
        {
            for (int i = 0; i < mapTree[^1].Count; i++)
            {
                TunnelSection section = mapTree[^1][i];
                if (section.Keep)
                {
                    section.gameObject.SetActive(false);
                    section.transform.parent = sectionGraveYard;
                    ClearConnectors(section);
                    mothBalledSections.Add(section, new(math.distancesq(curPlayerSection.Position, section.Position), mapTree.Count - 1));
                }
                else
                {
                    section.CollidersEnabled = false;
                    DestroySection(section);
                }
            }
            mapTree.RemoveAt(mapTree.Count - 1);
        }
        Physics.SyncTransforms();
        CheckForSectionsPromotions();
        reRingInters = 1;
        RecursiveBuilder();
        reRingInters = 0;
    }

    private int GetFreeConnectorCount(List<TunnelSection> sections)
    {
        int freeConnectors = 0;
        for (int i = 0; i < sections.Count; i++)
        {
            TunnelSection section = sections[i];
            freeConnectors += section.connectors.Length - section.InUse.Count;
        }
        return freeConnectors;
    }

    private void MakeRootNode(TunnelSection newRoot)
    {
        UpdateMothBalledSections(newRoot);

        List<List<TunnelSection>> newTree = new() { new() { newRoot } };
        HashSet<TunnelSection> exceptWith = new(newTree[^1]);

        RecursiveTreeBuilder(newTree, exceptWith);
        Debug.LogFormat("New Tree Size {0}", newTree.Count);
        Debug.LogFormat("Original Tree Size {0}", mapTree.Count);

        bool forceGrow = newTree.Count < mapTree.Count;

        Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
                TunnelSection section = newTree[^1][i];
                if (section.Keep)
                {
                    section.gameObject.SetActive(false);
                    section.transform.parent = sectionGraveYard;
                    if (!mothBalledSections.ContainsKey(section))
                    {
                        ClearConnectors(section);
                        mothBalledSections.Add(section, new(math.distancesq(newRoot.Position, section.Position), newTree.Count - 1));
                    }
                }
                else
                {
                    leafCounter++;
                    DestroySection(section);
                }
            }
            newTree[^1].Clear();
            newTree.RemoveAt(newTree.Count - 1);
        }
        Physics.SyncTransforms();

        mapTree.Clear();
        mapTree.AddRange(newTree);

        Debug.Log("Growing Tree..");
        int oldSize = 0;
        if (forceGrow)
        {
            RecursiveBuilder();
        }
        else
        {
            oldSize = mapTree[^1].Count;
            FillSectionConnectors(mapTree[^2]);
        }
        Debug.LogFormat("Grew {0} leaves", mapTree[^1].Count - oldSize);
        curPlayerSection = newTree[0][0];
        if (curPlayerSection == null)
        {
            Debug.LogWarning("cur player section null, attempting to resolve..");
            ResolvePlayerSection();
        }

        if (ringRenderDst < maxDst)
        {
            for (int i = 0; i < ringRenderDst; i++)
            {
                mapTree[i].ForEach(section => section.SetRenderersEnabled(true));
            }
        }
    }

    private void CheckForSectionsPromotions()
    {
        Debug.LogFormat(gameObject, "promoteList: {0} promoteDict: {1}", promoteSectionsList.Count, promoteSectionsDict.Count);
        if (mothBalledSections.Count > 0)
        {
            List<TunnelSection> mothBalledSections = new(this.mothBalledSections.Keys);
            mothBalledSections.ForEach(section =>
            {
                int curDst = this.mothBalledSections[section].dst;
                if (curDst < mapTree.Count)
                {
                    if (promoteSectionsDict.ContainsKey(curDst))
                    {
                        promoteSectionsDict[curDst].Add(section);
                    }
                    else
                    {
                        promoteSectionsDict.Add(curDst, new List<TunnelSection>() { section });
                    }
                    this.mothBalledSections.Remove(section);
                }
                else
                {
                    if (promoteSectionsDict.ContainsKey(curDst) && promoteSectionsDict[curDst].Contains(section))
                    {
                        HashSet<TunnelSection> promoteSet = new(promoteSectionsDict[curDst]);
                        promoteSet.Remove(section);
                        promoteSectionsDict[curDst] = new(promoteSet);
                    }
                }
            });
        }
        Debug.LogFormat(gameObject, "promoteList: {0} promoteDict: {1}", promoteSectionsList.Count, promoteSectionsDict.Count);
    }

    private void UpdateMothBalledSections(TunnelSection newRoot)
    {
        if (mothBalledSections.Count > 0)
        {
            List<TunnelSection> mothBalledSections = new(this.mothBalledSections.Keys);
            mothBalledSections.ForEach(section =>
            {
                SectionDstData cur = this.mothBalledSections[section];
                SectionDstData newRootDstData = new(math.distancesq(newRoot.Position, section.Position), cur.dst);

                newRootDstData.dst += cur.sqrDst < newRootDstData.sqrDst ? 1 : -1;
                Debug.LogFormat("Updated mothballed section distance: {1} DST: {0}",newRootDstData.dst,section.name);
                this.mothBalledSections[section] = newRootDstData;
            });

            CheckForSectionsPromotions();
        }
    }

    private void RecursiveTreeBuilder(List<List<TunnelSection>> recursiveConstruction, HashSet<TunnelSection> exceptWith)
    {
        exceptWith.UnionWith(recursiveConstruction[^1]);
    
        HashSet<TunnelSection> dstOne = new();
        List<TunnelSection> curList = recursiveConstruction[^1];
        for (int i = 0; i < curList.Count; i++)
        {
            TunnelSection sec = curList[i];
            List<SectionAndConnector> SandCs = new(sec.connectorPairs.Values);
            for (int j = 0; j < SandCs.Count; j++)
            {
                SectionAndConnector SandC = SandCs[j];
                if (!exceptWith.Contains(SandC.sectionInstance))
                {
                    dstOne.Add(SandC.sectionInstance);
                }
            }
        }
        dstOne.ExceptWith(exceptWith);
        if (dstOne.Count > 0)
        {
            recursiveConstruction.Add(new(dstOne));
            RecursiveTreeBuilder(recursiveConstruction, exceptWith);
        }
    }

    private void ResolvePlayerSection()
    {
        GameObject player = FindObjectOfType<Improved_Movement>().gameObject;
        if (player.GetComponent<Collider>().Raycast(new(player.transform.position, Vector3.down), out RaycastHit hitInfo, 100f))
        {
            TunnelSection section = hitInfo.collider.gameObject.GetComponentInParent<TunnelSection>();
            section = section != null ? section : hitInfo.collider.gameObject.GetComponentInChildren<TunnelSection>();
            if ( section != null)
            {
#if UNITY_EDITOR
                if (debugging)
                {
                    StartCoroutine(MakeRootNodeDebug(section));
                }
                else
                {
                    MakeRootNode(section);
                }
#else
                MakeRootNode(section);
#endif
            }
        }
    }


    /// <summary>
    /// Based off the givne primary section, this picks a new section prefab, Instinates it and connects (transforms) it to the primay section
    /// </summary>
    /// <param name="primary">Given root node of this connection</param>
    /// <returns>New Section Instance</returns>
    private TunnelSection PickInstinateConnect(TunnelSection primary)
    {
        TunnelSection pickedSection = null;
        TunnelSection pickedInstance = null;
        Connector priPref = Connector.Empty, secPref = Connector.Empty;
        if (promoteSectionsList.Count > 0)
        {
            List<int> internalSections = new(promoteSectionsList.Count);
            promoteSectionsList.ForEach(section => internalSections.Add(section.orignalInstanceId));
            pickedSection = PickSection(primary, internalSections, out priPref, out secPref);
            if (!internalSections.Contains(pickedSection.GetInstanceID())) // returned dead end.
            {
                List<int> nextSections = FilterSections(primary);
                pickedSection = PickSection(primary, nextSections, out priPref, out secPref);
                pickedInstance = InstinateSection(pickedSection);
            }
            else // reinstance section
            {
                int index = internalSections.IndexOf(pickedSection.GetInstanceID());
                pickedInstance = promoteSectionsList[index];
                pickedInstance.gameObject.SetActive(true);
                pickedInstance.CollidersEnabled = true;
                promoteSectionsList.RemoveAt(index);
            }
        }
        else
        {
            List<int> nextSections = FilterSections(primary);
            pickedSection = PickSection(primary, nextSections, out priPref, out secPref);
            pickedInstance = InstinateSection(pickedSection);
        }
        
        

        TransformSection(primary, pickedInstance, priPref, secPref); // transform the new section

        if (pickedSection != deadEndPlug && !rejectBreakableWallAtConnections)
        {
            if (forceBreakableWallAtConnections || Random.value < breakableWallAtConnectionChance)
            {
                BreakableWall breakableInstance = Instantiate(breakableWall, pickedInstance.transform);
                Connector conn = breakableInstance.connector;
                conn.UpdateWorldPos(breakableWall.transform.localToWorldMatrix);
                TransformSection(breakableInstance.transform, priPref, conn);
            }
        }

        Physics.SyncTransforms(); /// push changes to physics world now instead of next fixed update, required for <see cref="RunIntersectionTests(TunnelSection, TunnelSection, out Connector, out Connector)"/>
        return pickedInstance;
    }

    /// <summary>
    /// Based on a given root, this method figures out what prefabs can connect to it and then chooses, semi-randomly, a prefab from that list that fits in the world.
    /// In the event no section will fit in the world, a dead end is placed instead.
    /// </summary>
    /// <param name="primary">Root Section</param>
    /// <param name="primaryPreference">Root section connector target</param>
    /// <param name="secondaryPreference">New section connector target</param>
    /// <returns>Chosen Prefab</returns>
    private TunnelSection PickSection(TunnelSection primary, List<int> nextSections, out Connector primaryPreference, out Connector secondaryPreference)
    {
        primaryPreference = Connector.Empty;
        secondaryPreference = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);

        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.TempJob);

        int iterations = maxInterations;
        TunnelSection targetSection = null;

        Physics.SyncTransforms();
        while (targetSection == null && primaryConnectors.Count > 0)
        {
            primaryPreference = GetConnectorFromSection(primaryConnectors, out int priIndex);

            NativeReference<BurstConnector> priConn = new(new(primaryPreference), Allocator.TempJob);
            if (!priConn.IsCreated)
            {
                Debug.LogError("Failed to create priConnector native reference!", gameObject);
                continue;
            }
            JobHandle handle = new BurstConnectorMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());

            var bmj = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectorContainers,
                boxBounds = sectionBoxMatrices,
                matrices = sectionBoxTransforms
            };

            handle = parallelMatrixCalculations
                ? bmj.ScheduleParallel(nextSections.Count,8, handle)
                : bmj.Schedule(nextSections.Count, handle);
            
            priConn.Dispose(handle).Complete();


            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(primaryPreference), nextSections);
            while (internalNextSections.Count > 0)
            {
                int curInstanceID = internalNextSections.ElementAt(Random.Range(0, internalNextSections.Count));
                targetSection = instanceIdToSection[curInstanceID];
                if (RunIntersectionTests(primary, targetSection, ref primaryPreference, out secondaryPreference))
                {
                    break;
                }
                internalNextSections.Remove(curInstanceID);
                targetSection = null;
                iterations--;
                if (iterations <= 0)
                {
                    Debug.LogException(new System.StackOverflowException("Intersection test exceeded max iterations"), this);
                }
            }
            if (targetSection != null)
            {
                break;
            }
            primaryConnectors.RemoveAt(priIndex);
        }

        nativeNexSections.Dispose();

        if (targetSection == null)
        {
            secondaryPreference = deadEndPlug.connectors[0];
            secondaryPreference.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            targetSection = deadEndPlug;
            Debug.LogWarning("Unable to find usable section, ending the tunnel.", primary);            
        }
        return targetSection;
    }

    private List<int> FilterSections(TunnelSection primary)
    {
        List<int> nextSections = new(tunnelSectionsByInstanceID);

        if (primary.ExcludePrefabConnections.Count > 0)
        {
            primary.ExcludePrefabConnections.ForEach(item => nextSections.RemoveAll(element => element == item));
        }

        nextSections.RemoveAll(element => !Spawnable(element,true));

        return nextSections;
    }

    private List<int> FilterSectionsByConnector(ConnectorMask connector, List<int> sections)
    {
        if(connector.exclude != null)
        {
            connector.Build();
        }
        List<int> nextSections = new(sections);

        if (connector.excludeRuntime.Count > 0)
        {
            for (int i = sections.Count - 1; i >= 0; i--)
            {
                if (connector.excludeRuntime.Contains(sections[i]))
                {
                    nextSections.RemoveAt(i);
                }
            }
        }

        return nextSections;
    }

    /// <summary>
    /// Systematically runs through all connector cominbations of the given sections.
    /// Projects the bounding boxes of the new section into the current map to see if it will fit.
    /// Runs until all a valid combination is made or all connectors are tested.
    /// </summary>
    /// <param name="primary"></param>
    /// <param name="target"></param>
    /// <param name="primaryConnector"></param>
    /// <param name="secondaryConnector"></param>
    /// <returns></returns>
    private bool RunIntersectionTests(TunnelSection primary, TunnelSection target, ref Connector primaryConnector, out Connector secondaryConnector)
    {
        secondaryConnector = Connector.Empty;
        List<Connector> secondaryConnectors = FilterConnectors(target);

        while (secondaryConnectors.Count > 0)
        {
            secondaryConnector = GetConnectorFromSection(secondaryConnectors, out int secIndex);

            bool noIntersections = IntersectionTest(primary, target, ref primaryConnector, ref secondaryConnector);

            if (noIntersections)
            {
                return true;
            }
            secondaryConnectors.RemoveAt(secIndex);
        }
        return false;
    }

    private List<Connector> FilterConnectors(TunnelSection section)
    {
        List<Connector> connectors = new(section.connectors);

        if (section.InUse.Count > 0)
        {
            for (int i = connectors.Count - 1; i >= 0; i--)
            {
                if (section.InUse.Contains(connectors[i].internalIndex))
                {
                    connectors.RemoveAt(i);
                }
            }
        }

        return connectors;
    }

    public Connector GetConnectorFromSection(List<Connector> validConnectors, out int index)
    {
        index = Random.Range(0, validConnectors.Count);
        return validConnectors[index];
    }

    /// <summary>
    /// Tests whether hte given target section will fit into the map with computed transform from the given connectors.
    /// </summary>
    /// <param name="primary"></param>
    /// <param name="target"></param>
    /// <param name="primaryConnector"></param>
    /// <param name="secondaryConnector"></param>
    /// <returns>True if the section will fit, false if not.</returns>
    private bool IntersectionTest(TunnelSection primary, TunnelSection target, ref Connector primaryConnector, ref Connector secondaryConnector)
    {
        NativeReference<Connector> pri = new(primaryConnector, Allocator.TempJob);
        NativeReference<Connector> sec = new(secondaryConnector, Allocator.TempJob);
        JobHandle handle1 = new ConnectorMulJob { connector = pri, sectionLTW = primary.transform.localToWorldMatrix }.Schedule(new JobHandle());
        JobHandle handle2 = new ConnectorMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryConnector = pri.Value;
        secondaryConnector = sec.Value;
        pri.Dispose();
        sec.Dispose();

        int instanceID = target.GetInstanceID();
        UnsafeList<UnsafeList<BoxTransform>> transformsContainer = sectionBoxTransforms[instanceID];
        if (transformsContainer.Length == 0)
        {
            Debug.LogError("no transforms found for connector");
            return false;
        }

        if(transformsContainer.Length <= secondaryConnector.internalIndex)
        {
            Debug.LogErrorFormat(gameObject, "returned transforms list does not contain index ({0}) for connector", secondaryConnector.internalIndex);
        }

        UnsafeList<BoxTransform> transforms = transformsContainer[secondaryConnector.internalIndex];
        if (transforms.Length == 0)
        {
            Debug.LogError("no box transforms found");
            return false;
        }

        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];

            BoxTransform m = transforms[i];
            float3 position = m.pos;
            Quaternion rotation = m.rot;

            if (Physics.CheckBox(position, boxBounds.size * 0.5f, rotation, tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }
        return true;
    }

    private bool Spawnable(int id, bool update = false)
    {
        return update ? instanceIdToSection[id].UpdateRule() : instanceIdToSection[id].Spawnable;
    }

    private TunnelSection InstinateSection(int index)
    {
        return InstinateSection(instanceIdToSection[tunnelSectionsByInstanceID[index]]);
    }

    private TunnelSection InstinateSection(TunnelSection tunnelSection)
    {
        TunnelSection section = Instantiate(tunnelSection);

        tunnelSection.InstanceCount++;
        section.gameObject.SetActive(true);
        section.transform.parent = transform;
        if (instanceIdToSection.TryGetValue(tunnelSection.orignalInstanceId, out TunnelSection original))
        {
            original.Spawned();
        }
        return section;
    }

    private void DestroySection(TunnelSection section)
    {
        ClearConnectors(section);

        if (instanceIdToSection.ContainsKey(section.orignalInstanceId))
        {
            instanceIdToSection[section.orignalInstanceId].InstanceCount--;
        }
        else
        {
            Debug.LogException(new KeyNotFoundException(string.Format("Key: {0} not present in dictionary instanceIdToSection!", section.orignalInstanceId)),gameObject);
            Debug.LogErrorFormat(gameObject, "Likely section {0} has incorrect instance id of {1}", section.gameObject.name, section.orignalInstanceId);
        }

        Destroy(section.gameObject);
    }

    private static void ClearConnectors(TunnelSection section)
    {
        List<int> pairKeys = new(section.connectorPairs.Keys);
        pairKeys.ForEach(key =>
        {
            SectionAndConnector sectionTwin = section.connectorPairs[key];
            if (sectionTwin != null && sectionTwin.sectionInstance != null)
            {
                sectionTwin.sectionInstance.connectorPairs[sectionTwin.internalIndex] = null;
                sectionTwin.sectionInstance.InUse.Remove(sectionTwin.internalIndex);
                sectionTwin.sectionInstance.connectorPairs.Remove(sectionTwin.internalIndex);
            }
        });

        section.connectorPairs.Clear();
        section.InUse.Clear();
    }

    private void TransformSection(TunnelSection primary, TunnelSection secondary, Connector primaryConnector, Connector secondaryConnector)
    {
        TransformSection(secondary.transform, primaryConnector, secondaryConnector);

        primary.connectorPairs[primaryConnector.internalIndex] = new(secondary, secondaryConnector.internalIndex);
        secondary.connectorPairs[secondaryConnector.internalIndex] = new(primary, primaryConnector.internalIndex);
        primary.InUse.Add(primaryConnector.internalIndex);
        secondary.InUse.Add(secondaryConnector.internalIndex);
    }

    private void TransformSection(Transform secondaryTransform, Connector primaryConnector, Connector secondaryConnector)
    {
        float4x4 transformMatrix = CalculateSectionMatrix(primaryConnector, secondaryConnector);
        secondaryTransform.SetPositionAndRotation(transformMatrix.Translation(), transformMatrix.Rotation());
    }

    private float4x4 CalculateSectionMatrix(Connector primary, Connector secondary)
    {
        Quaternion rotation = Quaternion.Inverse(secondary.rotation) * (primary.rotation * Quaternion.Euler(0, 180, 0));
        secondary.UpdateWorldPos(float4x4.TRS(primary.position, rotation, Vector3.one));

        Vector3 position = primary.position + (primary.position - secondary.position);
        position.y = primary.parentPos.y + (primary.localPosition.y - secondary.localPosition.y);
        
#if UNITY_EDITOR
        if (debugging && transformDebugging)
        {
            secondary.UpdateWorldPos(float4x4.TRS(position, rotation, Vector3.one));

            secondaryObj.transform.SetPositionAndRotation(secondary.position, secondary.localRotation);
        }
#endif
        return float4x4.TRS(position, rotation, Vector3.one);
    }

    internal Dictionary<int,Texture2D> GenerateMiniMapTextures()
    {
        Dictionary<int, Texture2D> textureLoopUp = new (tunnelSectionsByInstanceID.Count);
        for (int i = 0; i < tunnelSectionsByInstanceID.Count; i++)
        {
            int instanceID = tunnelSectionsByInstanceID[i];
            textureLoopUp.Add(instanceID, instanceIdToSection[instanceID].MiniMapAsset);
        }
        return textureLoopUp;
    }

    internal List<TunnelSection> GetMothballedSections()
    {
        HashSet<TunnelSection> sections = new();

        if(mothBalledSections.Count > 0)
        {
            sections.UnionWith(mothBalledSections.Keys);
        }
        if (promoteSectionsList.Count > 0)
        {
            sections.UnionWith(promoteSectionsList);
        }
        if (promoteSectionsDict.Count > 0)
        {
            foreach (var item in promoteSectionsDict.Values)
            {
                sections.UnionWith(item);
            }
        }

        return sections.ToList();
    }
}
