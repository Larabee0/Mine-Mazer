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
    [SerializeField] private List<TunnelSection> tunnelSections;

    private Dictionary<int, TunnelSection> instanceIdToSection;
    private List<int> tunnelSectionsByInstanceID;

    [Header("Runtime Map")]
    [SerializeField] private TunnelSection curPlayerSection;
    [SerializeField] private List<List<TunnelSection>> recursiveConstruction = new();

    private Dictionary<TunnelSection, SectionDstData> mothBalledSections = new();
    private Dictionary<int, List<TunnelSection>> unMothBall = new();
    List<TunnelSection> unMothBallSections = new();

    private Coroutine mapUpdateProcess;

    UnsafeParallelHashMap<int, UnsafeList<UnsafeList<BoxTransform>>> matrices;
    NativeParallelHashMap<int, UnsafeList<Connector>> sectionConnectors;
    NativeParallelHashMap<int, UnsafeList<BoxBounds>> boxBounds;

    [Header("Generation Settings")]
    [SerializeField, Min(1)] private int maxDst = 3;
    [SerializeField] private LayerMask tunnelSectionLayerMask;
    [SerializeField, Min(1000)] private int maxInterations = 1000000;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private Random.State seed;

    private int tunnelSectionLayerIndex;
    private TunnelSection lastEnter;
    private TunnelSection lastExit;

#if UNITY_EDITOR
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
        instanceIdToSection = new Dictionary<int, TunnelSection>(tunnelSections.Count);
        tunnelSectionsByInstanceID = new(tunnelSections.Count);
        tunnelSections.ForEach(prefab =>
        {
            tunnelSectionsByInstanceID.Add(prefab.GetInstanceID());
            instanceIdToSection.TryAdd(tunnelSectionsByInstanceID[^1], prefab);
            prefab.excludeConnectorSections.ForEach(connector => connector.Build());
            prefab.Build();
        });
        tunnelSections.Clear();
        tunnelSections = null;


        SetUpBurstDataStructures(tunnelSectionsByInstanceID);
    }

    private void Start()
    {
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
        tunnelSectionsByInstanceID.ForEach(id =>
        {
            boxBounds[id].Dispose();
            sectionConnectors[id].Dispose();
            UnsafeList<UnsafeList<BoxTransform>> boxTransforms = matrices[id];
            for (int i = 0; i < boxTransforms.Length; i++)
            {
                boxTransforms[i].Dispose();
            }
            boxTransforms.Dispose();
        });
        matrices.Dispose();
        boxBounds.Dispose();
        sectionConnectors.Dispose();
    }

    private void SetUpBurstDataStructures(List<int> nextSections)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        sectionConnectors = new(nextSections.Count, Allocator.Persistent);
        boxBounds = new(nextSections.Count, Allocator.Persistent);

        SetUpBurstMatrices(nextSections,Allocator.Persistent);

        for (int i = 0; i < nextSections.Count; i++)
        {
            int id = nextSections[i];
            TunnelSection section = instanceIdToSection[id];

            UnsafeList<BoxBounds> bounds = new(section.boundingBoxes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<BoxBounds> nativeBounds = new(section.boundingBoxes, Allocator.Temp);
            bounds.CopyFrom(nativeBounds);
            boxBounds.Add(id, bounds);

            UnsafeList<Connector> connectors = new(section.connectors.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<Connector> nativeConnectors = new(section.connectors, Allocator.Temp);
            connectors.CopyFrom(nativeConnectors);
            sectionConnectors.Add(id, connectors);
        }
        Debug.LogFormat("Initial Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
    }

    private void SetUpBurstMatrices(List<int> sections, Allocator allocator)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        matrices = new(sections.Count, allocator);
        for (int i = 0; i < sections.Count; i++)
        {
            int id = sections[i];
            TunnelSection section = instanceIdToSection[id];

            UnsafeList<UnsafeList<BoxTransform>> boxTransforms = new(section.connectors.Length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < section.connectors.Length; j++)
            {
                boxTransforms.AddNoResize(new UnsafeList<BoxTransform>(section.boundingBoxes.Length, allocator, NativeArrayOptions.UninitializedMemory));
            }
            matrices.Add(id, boxTransforms);
        }
        Debug.LogFormat("Matrix Prep Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
    }

    private void ClearBurstMatrices()
    {
        tunnelSectionsByInstanceID.ForEach(id =>
        {
            UnsafeList<UnsafeList<BoxTransform>> boxTransforms = matrices[id];
            for (int i = 0; i < boxTransforms.Length; i++)
            {
                UnsafeList<BoxTransform> boxes = boxTransforms[i];
                boxes.Clear();
                boxTransforms[i] = boxes;
            }
            matrices[id] = boxTransforms;
        });
    }


#if UNITY_EDITOR

    private void Debugging()
    {
        if (santiziedCube == null)
        {
            Debug.LogWarning("Santizied cube unassigned!");
        }
        if (initialAreaDebugging)
        {
            StartCoroutine(GenerateInitialAreaDebug());
        }
        if (transformDebugging)
        {
            if (prefab1 == null || prefab2 == null)
            {
                Debug.LogWarning("prefab not assigned!");
            }
            StartCoroutine(TransformDebugging());
        }
    }

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

        // yield return new WaitForSeconds(distanceListPauseTime);
        // yield return FillOneDstListDebug(oneDstSections, curPlayerSection);
        // yield return new WaitForSeconds(distanceListPauseTime);
        // yield return FillTwoDstListDebug(oneDstSections, twoDstSections);
        // yield return new WaitForSeconds(distanceListPauseTime);
        // 
        // oneDstSections.Clear();
        // twoDstSections.Clear();
        recursiveConstruction.Add(new() { curPlayerSection });
        yield return RecursiveBuilderDebug();
        yield return new WaitForSeconds(distanceListPauseTime * 2);

        //yield return MakeRootNode(recursiveConstruction[1][Random.Range(0, recursiveConstruction[1].Count)]);
        //TestBuildListStructures();
        Debug.Log("Ended Initial Area Debug");
    }

    private IEnumerator RecursiveBuilderDebug()
    {
        while (recursiveConstruction.Count <= maxDst)
        {
            yield return new WaitForSeconds(distanceListPauseTime);
            List<TunnelSection> startSections = recursiveConstruction[^1];

            recursiveConstruction.Add(new());

            yield return FillSectionConnectorsDebug(startSections);
        }
    }

    private IEnumerator FillSectionConnectorsDebug(List<TunnelSection> startSections)
    {
        for (int i = 0; i < startSections.Count; i++)
        {
            TunnelSection section = startSections[i];
            int freeConnectors = section.connectors.Length - section.InUse.Count;
            for (int j = 0; j < freeConnectors; j++)
            {
                // pick a new section to connect to
                yield return PickSectionDebug(section);

                TunnelSection sectionInstance = InstinateSection(targetSectionDebug);

                recursiveConstruction[^1].Add(sectionInstance); // add this to 2 back
                TransformSection(section, sectionInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // position new section
                Physics.SyncTransforms();
            }
        }
    }

    private IEnumerator MakeRootNodeDebug(TunnelSection newRoot)
    {
        Debug.Log("Begin Root Node Update");

        UpdateMothBalledSections(newRoot);

        List<List<TunnelSection>> newTree = new() { new() { newRoot } };
        HashSet<TunnelSection> exceptWith = new(newTree[^1]);
        yield return new WaitForSeconds(intersectTestHoldTime);
        Debug.Log("Building new Tree..");
        yield return RecursiveTreeBuilderDebug(newTree, exceptWith);
        if(newTree.Count < recursiveConstruction.Count)
        {
            Debug.LogError("Failed to build tree");
        }
        Debug.LogFormat("New Tree Size {0}", newTree.Count);
        Debug.LogFormat("Original Tree Size {0}", recursiveConstruction.Count);

        yield return new WaitForSeconds(intersectTestHoldTime);

        Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
                TunnelSection section = newTree[^1][i];
                yield return new WaitForSeconds(intersectTestHoldTime);
                if (section.keep)
                {
                    section.gameObject.SetActive(false);
                    mothBalledSections.Add(section, new(math.distancesq(newRoot.transform.position, section.transform.position), newTree.Count - 1));
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

        recursiveConstruction.Clear();
        recursiveConstruction.AddRange(newTree);

        Debug.Log("Growing Tree..");
        int oldSize = recursiveConstruction[^1].Count;
        yield return FillSectionConnectorsDebug(recursiveConstruction[^2]);
        Debug.LogFormat("Grew {0} leaves", recursiveConstruction[^1].Count - oldSize);
        curPlayerSection = newTree[0][0];
        if (curPlayerSection == null)
        {
            ResolvePlayerSection();
        }
    }

    private IEnumerator RecursiveTreeBuilderDebug(List<List<TunnelSection>> recursiveConstruction, HashSet<TunnelSection> exceptWith)
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
            // Debug.LogFormat("Last: {0} Total: {1}", dstOne.Count, recursiveConstruction.Count);
            yield return new WaitForSeconds(intersectTestHoldTime);
            recursiveConstruction.Add(new(dstOne));
            yield return RecursiveTreeBuilderDebug(recursiveConstruction, exceptWith);
        }
    }

    private IEnumerator PickSectionDebug(TunnelSection primary)
    {
        primaryPreferenceDebug = Connector.Empty;
        secondaryPreferenceDebug = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);
        List<int> nextSections = FilterSections(primary);
        ClearBurstMatrices();
        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.Persistent);
        int iterations = maxInterations;

        targetSectionDebug = null;

        while (targetSectionDebug == null && primaryConnectors.Count > 0)
        {
            primaryPreferenceDebug = GetConnectorFromSection(primaryConnectors, out int priIndex);

            double startTime = Time.realtimeSinceStartupAsDouble;
            NativeReference<Connector> priConn = new(primaryPreferenceDebug, Allocator.TempJob);

            JobHandle handle = new MatrixMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());
            handle = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectors,
                boxBounds = boxBounds,
                matrices = matrices
            }.Schedule(nextSections.Count, handle);
            priConn.Dispose(handle).Complete();
            Debug.LogFormat("Big Matrix: {1} Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f, nextSections.Count);

            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(primaryPreferenceDebug), nextSections);
            while (internalNextSections.Count > 0)
            {
                int curInstanceID = internalNextSections.ElementAt(Random.Range(0, internalNextSections.Count));
                targetSectionDebug = instanceIdToSection[curInstanceID];
                yield return IntersectionTestDebug(primary, targetSectionDebug);
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

    private IEnumerator IntersectionTestDebug(TunnelSection primary, TunnelSection target)
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

    private IEnumerator DebugIntersectionTest(TunnelSection primary, TunnelSection target)
    {
        primaryPreferenceDebug.UpdateWorldPos(primary.transform.localToWorldMatrix);
        secondaryPreferenceDebug.UpdateWorldPos(float4x4.identity);

        List<GameObject> objects = new();

        float4x4 secondaryTransform = CalculateSectionMatrix(primaryPreferenceDebug, secondaryPreferenceDebug);

        //objects.Add(Instantiate(target,pos,rot).gameObject);

        bool noIntersections = true;
        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];
            objects.Add(Instantiate(santiziedCube));

            float4x4 m = math.mul(secondaryTransform, target.boundingBoxes[i].Matrix);

            objects[^1].transform.SetPositionAndRotation(m.Translation(), m.Rotation());
            objects[^1].transform.localScale = boxBounds.size;
            objects[^1].GetComponent<MeshRenderer>().material.color = Color.green;
            if (Physics.CheckBox(m.Translation(), boxBounds.size * 0.5f, m.Rotation(), tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
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
    private IEnumerator DebugIntersectionTestBurst(TunnelSection primary, TunnelSection target)
    {
        NativeReference<Connector> pri = new(primaryPreferenceDebug, Allocator.TempJob);
        NativeReference<Connector> sec = new(secondaryPreferenceDebug, Allocator.TempJob);
        JobHandle handle1 = new MatrixMulJob { connector = pri, sectionLTW = primary.transform.localToWorldMatrix }.Schedule(new JobHandle());
        JobHandle handle2 = new MatrixMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryPreferenceDebug = pri.Value;
        secondaryPreferenceDebug = sec.Value;
        pri.Dispose();
        sec.Dispose();

        List<GameObject> objects = new();
        //objects.Add(Instantiate(target,pos,rot).gameObject);
        int instanceID = target.GetInstanceID();
        UnsafeList<UnsafeList<BoxTransform>> transformsContainer = matrices[instanceID];
        if (transformsContainer.Length == 0)
        {
            Debug.LogError("no transforms found for connector");
            yield break;
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
            Quaternion rotation = m.rotation;

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
        recursiveConstruction.Add(new() { curPlayerSection });

        double startTime = Time.realtimeSinceStartupAsDouble;
        RecursiveBuilder();
        Debug.LogFormat("Map Update Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
        // FillOneDstList(oneDstSections, curPlayerSection);
        //
        // FillTwoDstList(oneDstSections, twoDstSections);
    }

    private void RecursiveBuilder()
    {
        while(recursiveConstruction.Count <= maxDst)
        {
            List<TunnelSection> startSections = recursiveConstruction[^1];
            recursiveConstruction.Add(new());
            FillSectionConnectors(startSections);
        }
        StartCoroutine(BreakEditor());
    }

    private IEnumerator BreakEditor()
    {
        yield return null;
        yield return null;
        Debug.Break();
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

        if (lastExit != null)
        {
            UpdateMap();
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
        yield return null;
        Debug.Break();
    }

    private void FillSectionConnectors(List<TunnelSection> startSections)
    {
        if (unMothBall.Count > 0 && unMothBall.ContainsKey(recursiveConstruction.Count - 1))
        {
            int freeConnectors = GetFreeConnectorCount(startSections);
            if (freeConnectors < unMothBall[recursiveConstruction.Count - 1].Count)
            {
                // not enough free connectors, go in a level and regenerate.
                Debug.LogWarningFormat("Regening ring {0} it does not have enough free connectors.", recursiveConstruction.Count - 2);
                RegenRing(recursiveConstruction.Count - 2);
                return;
            }
            else
            {
                unMothBallSections.AddRange(unMothBall[recursiveConstruction.Count - 1]);
                unMothBall.Remove(recursiveConstruction.Count - 1);
            }
        }

        for (int i = 0; i < startSections.Count; i++)
        {
            TunnelSection section = startSections[i];
            int freeConnectors = section.connectors.Length - section.InUse.Count;
            for (int j = 0; j < freeConnectors; j++)
            {
                TunnelSection sectionInstance = PickInstinateConnect(section);
                recursiveConstruction[^1].Add(sectionInstance);
            }
        }

        if (unMothBallSections.Count > 0 && recursiveConstruction.Count <= maxDst)
        {
            if (unMothBall.ContainsKey(recursiveConstruction.Count))
            {
                unMothBall[recursiveConstruction.Count].AddRange(unMothBallSections);
            }
            else
            {
                unMothBall.Add(recursiveConstruction.Count, new(unMothBallSections));
            }
            unMothBall.Clear();
        }
    }

    private void RegenRing(int regenTarget)
    {
        while(recursiveConstruction.Count - 1 != regenTarget)
        {
            for (int i = 0; i < recursiveConstruction[^1].Count; i++)
            {
                TunnelSection section = recursiveConstruction[^1][i];
                if (section.keep)
                {
                    section.gameObject.SetActive(false);
                    mothBalledSections.Add(section, new(math.distancesq(curPlayerSection.Position, section.Position), recursiveConstruction.Count - 1));
                }
                else
                {
                    DestroySection(section);
                }
            }
            recursiveConstruction.RemoveAt(recursiveConstruction.Count - 1);
        }
        UpdateUnMothBallSections();
        RecursiveBuilder();
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
        Debug.LogFormat("Original Tree Size {0}", recursiveConstruction.Count);
        bool forceGrow = newTree.Count < recursiveConstruction.Count;
        Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while(newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
                TunnelSection section = newTree[^1][i];
                if (section.keep)
                {
                    section.gameObject.SetActive(false);
                    mothBalledSections.Add(section, new(math.distancesq(newRoot.Position, section.Position), newTree.Count - 1));
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

        recursiveConstruction.Clear();
        recursiveConstruction.AddRange(newTree);

        Debug.Log("Growing Tree..");
        int oldSize = 0;
        if (forceGrow)
        {
            RecursiveBuilder();
        }
        else
        {
            oldSize = recursiveConstruction[^1].Count;
            FillSectionConnectors(recursiveConstruction[^2]);
        }
        Debug.LogFormat("Grew {0} leaves", recursiveConstruction[^1].Count - oldSize);
        curPlayerSection = newTree[0][0];
        if (curPlayerSection == null)
        {
            Debug.LogWarning("cur player section null, attempting to resolve..");
            ResolvePlayerSection();
        }
    }

    private void UpdateUnMothBallSections()
    {
        if(mothBalledSections.Count > 0)
        {
            List<TunnelSection> mothBalledSections = new(this.mothBalledSections.Keys);
            mothBalledSections.ForEach(section =>
            {
                int curDst = this.mothBalledSections[section].dst;
                if (curDst < recursiveConstruction.Count)
                {
                    if (unMothBall.ContainsKey(curDst))
                    {
                        unMothBall[curDst].Add(section);
                    }
                    else
                    {
                        unMothBall.Add(curDst, new List<TunnelSection>() { section });
                    }

                }
                else
                {
                    if (unMothBall.ContainsKey(curDst) && unMothBall[curDst].Contains(section))
                    {
                        HashSet<TunnelSection> unBallSet = new(unMothBall[curDst]);
                        unBallSet.Remove(section);
                        unMothBall[curDst] = new(unBallSet);
                    }
                }
            });
        }
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
                this.mothBalledSections[section] = newRootDstData;
            });

            UpdateUnMothBallSections();
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
            if (hitInfo.collider.gameObject.TryGetComponent(out TunnelSection section))
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
        if (unMothBallSections.Count > 0)
        {
            List<int> internalSections = new(unMothBallSections.Count);
            unMothBallSections.ForEach(section => internalSections.Add(section.orignalInstanceId));
            pickedSection = PickSection(primary, internalSections, out priPref, out secPref);
            if (!internalSections.Contains(pickedSection.GetInstanceID()))
            {
                List<int> nextSections = FilterSections(primary);
                pickedSection = PickSection(primary, nextSections, out priPref, out secPref);
                pickedInstance = InstinateSection(pickedSection);
            }
            else
            {
                int index = internalSections.IndexOf(pickedSection.GetInstanceID());
                pickedInstance = unMothBallSections[index];
                pickedInstance.gameObject.SetActive(true);
                unMothBallSections.RemoveAt(index);
            }
        }
        else
        {
            List<int> nextSections = FilterSections(primary);
            pickedSection = PickSection(primary, nextSections, out priPref, out secPref);
            pickedInstance = InstinateSection(pickedSection);
        }
        
        TransformSection(primary, pickedInstance, priPref, secPref); // transform the new section
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

        ClearBurstMatrices();

        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.TempJob);

        int iterations = maxInterations;
        TunnelSection targetSection = null;

        while (targetSection == null && primaryConnectors.Count > 0)
        {
            primaryPreference = GetConnectorFromSection(primaryConnectors, out int priIndex);

            NativeReference<Connector> priConn = new(primaryPreference, Allocator.TempJob);

            JobHandle handle = new MatrixMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());
            handle = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectors,
                boxBounds = boxBounds,
                matrices = matrices
            }.ScheduleParallel(nextSections.Count, 64, handle);
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
        JobHandle handle1 = new MatrixMulJob { connector = pri, sectionLTW = primary.transform.localToWorldMatrix }.Schedule(new JobHandle());
        JobHandle handle2 = new MatrixMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryConnector = pri.Value;
        secondaryConnector = sec.Value;
        pri.Dispose();
        sec.Dispose();

        int instanceID = target.GetInstanceID();
        UnsafeList<UnsafeList<BoxTransform>> transformsContainer = matrices[instanceID];
        if (transformsContainer.Length == 0)
        {
            Debug.LogError("no transforms found for connector");
            return false;
        }

        UnsafeList<BoxTransform> transforms = transformsContainer[secondaryPreferenceDebug.internalIndex];
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
            Quaternion rotation = m.rotation;

            if (Physics.CheckBox(position, boxBounds.size * 0.5f, rotation, tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }
        return true;
    }


    private TunnelSection InstinateSection(int index)
    {
        return InstinateSection(instanceIdToSection[tunnelSectionsByInstanceID[index]]);
    }

    private TunnelSection InstinateSection(TunnelSection tunnelSection)
    {
        TunnelSection section = Instantiate(tunnelSection);
        section.orignalInstanceId = tunnelSection.GetInstanceID();
        section.gameObject.SetActive(true);
        section.transform.parent = transform;
        return section;
    }

    private void DestroySection(TunnelSection section)
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
        Destroy(section.gameObject);
    }

    private void TransformSection(TunnelSection primary, TunnelSection secondary, Connector primaryConnector, Connector secondaryConnector)
    {
        float4x4 transformMatrix = CalculateSectionMatrix(primaryConnector, secondaryConnector);
        secondary.transform.SetPositionAndRotation(transformMatrix.Translation(), transformMatrix.Rotation());

        primary.connectorPairs[primaryConnector.internalIndex] = new(secondary, secondaryConnector.internalIndex);
        secondary.connectorPairs[secondaryConnector.internalIndex] = new(primary, primaryConnector.internalIndex);
        primary.InUse.Add(primaryConnector.internalIndex);
        secondary.InUse.Add(secondaryConnector.internalIndex);
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
}
