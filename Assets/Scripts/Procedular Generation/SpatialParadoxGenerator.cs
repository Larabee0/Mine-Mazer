using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private List<TunnelSection> twoDstSections = new();
    [SerializeField] private List<TunnelSection> oneDstSections = new();
    
    [Header("Generation Settings")]
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
        tunnelSectionsByInstanceID = new (tunnelSections.Count);
        tunnelSections.ForEach(prefab =>
        {
            tunnelSectionsByInstanceID.Add(prefab.GetInstanceID());
            instanceIdToSection.TryAdd(tunnelSectionsByInstanceID[^1], prefab);
            prefab.excludeConnectorSections.ForEach(connector => connector.Build());
            prefab.Build();
        });
        tunnelSections.Clear();
        tunnelSections = null;
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

        //FindObjectOfType<Improved_Movement>().transform.position = curPlayerSection.Centre;
    }


#if UNITY_EDITOR

    private void Debugging()
    {
        if(santiziedCube == null)
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
        int spawnIndex = Random.Range(0, tunnelSectionsByInstanceID.Count);
        curPlayerSection = InstinateSection(spawnIndex);
        curPlayerSection.transform.position = new Vector3(0, 0, 0);

        yield return new WaitForSeconds(distanceListPauseTime);
        yield return FillOneDstListDebug(oneDstSections, curPlayerSection);
        yield return new WaitForSeconds(distanceListPauseTime);
        yield return FillTwoDstListDebug(oneDstSections, twoDstSections);
    }

    /// <summary>
    /// Replicates Functionality of <see cref="FillOneDstList(List{TunnelSection}, TunnelSection, int)"/> with Coroutine support to allow visualisation of what is happening
    /// </summary>
    /// <param name="oneDstList"></param>
    /// <param name="primarySection"></param>
    /// <param name="iterations"></param>
    /// <returns></returns>
    private IEnumerator FillOneDstListDebug(List<TunnelSection> oneDstList, TunnelSection primarySection)
    {
        for (int j = 0; j < primarySection.connectors.Length; j++)
        {
            // pick a new section to connect to
            yield return PickSectionDebug(primarySection);
            TunnelSection sectionInstance = InstinateSection(targetSectionDebug);
            oneDstList.Add(sectionInstance);
            TransformSection(primarySection, sectionInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // position new section
        }
        yield return null;
    }

    /// <summary>
    /// Replicates Functionality of <see cref="FillTwoDstList(List{TunnelSection}, List{TunnelSection})"/> with Coroutine support to allow visualisation of what is happening
    /// </summary>
    /// <param name="oneDstList"></param>
    /// <param name="twoDstList"></param>
    /// <returns></returns>
    private IEnumerator FillTwoDstListDebug(List<TunnelSection> oneDstList, List<TunnelSection> twoDstList)
    {
        for (int i = 0; i < oneDstList.Count; i++) // for each item in 1 back
        {
            TunnelSection section = oneDstList[i];
            // for each connector -1 (exlucde connection to current section)
            int freeConnectors = section.connectors.Length - section.InUse.Count;
            for (int j = 0; j < freeConnectors; j++)
            {
                // pick a new section to connect to
                yield return PickSectionDebug(section);

                TunnelSection sectionInstance = InstinateSection(targetSectionDebug);

                twoDstList.Add(sectionInstance); // add this to 2 back
                TransformSection(section, sectionInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // position new section
            }
        }
    }

    private IEnumerator PickSectionDebug(TunnelSection primary)
    {
        primaryPreferenceDebug = Connector.Empty;
        secondaryPreferenceDebug = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);
        List<int> nextSections = FilterSections(primary);

        int iterations = maxInterations;
        targetSectionDebug = null;

        while(targetSectionDebug == null && primaryConnectors.Count > 0)
        {
            primaryPreferenceDebug = GetConnectorFromSection(primaryConnectors, out int priIndex);
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

        /*
        while (targetSectionDebug == null)
        {
            int curInstanceID = nextSections.ElementAt(Random.Range(0, nextSections.Count));
            targetSectionDebug = instanceIdToSection[curInstanceID];
            yield return IntersectionTestDebug(primary, targetSectionDebug);
            if (intersectionTest)
            {
                break;
            }
            nextSections.Remove(curInstanceID);
            targetSectionDebug = null;
            iterations--;
            if (iterations <= 0)
            {
                Debug.LogException(new System.StackOverflowException("Intersection test exceeded max iterations"), this);
            }
        }
        */

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

        while(secondaryConnectors.Count > 0)
        {
            secondaryPreferenceDebug = GetConnectorFromSection(secondaryConnectors, out int secIndex);
            yield return DebugIntersectionTest(primary, target);
            if (intersectionTest)
            {
                yield break;
            }
            secondaryConnectors.RemoveAt(secIndex);
        }
        /*
        while (primaryConnectors.Count > 0)
        {
            primaryPreferenceDebug = GetConnectorFromSection(primaryConnectors, out int priIndex);
            while (secondaryConnectors.Count > 0)
            {
                secondaryPreferenceDebug = GetConnectorFromSection(secondaryConnectors, out int secIndex);

                yield return DebugIntersectionTest(primary, target);
                if (intersectionTest)
                {
                    yield break;
                }
                secondaryConnectors.RemoveAt(secIndex);
            }
            secondaryConnectors = FilterConnectors(target);
            primaryConnectors.RemoveAt(priIndex);
        }
        */
        intersectionTest = false;
        yield break;
    }

    private IEnumerator DebugIntersectionTest(TunnelSection primary, TunnelSection target)
    {
        primaryPreferenceDebug.UpdateWorldPos(primary.transform.localToWorldMatrix);
        secondaryPreferenceDebug.UpdateWorldPos(target.transform.localToWorldMatrix);

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

#endif

    private void GenerateInitialArea()
    {
        seed = Random.state;
        if(startSection == null)
        {
            int spawnIndex = Random.Range(0, tunnelSectionsByInstanceID.Count);
            curPlayerSection = InstinateSection(spawnIndex);
        }
        else
        {
            curPlayerSection = InstinateSection(startSection);
        }
        curPlayerSection.transform.position = new Vector3(0, 0, 0);

        FillOneDstList(oneDstSections, curPlayerSection);

        FillTwoDstList(oneDstSections, twoDstSections);
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
        if (lastExit == curPlayerSection && lastEnter != curPlayerSection)
        {
            //if (oneDstSections.Contains(lastEnter))
            //{
            //    IncrementForward();
            //}
            //else if (prevPlayerSections.Contains(lastEnter))
            //{
            //    IncrementBackward();
            //}
            IncrementMap();
        }
        lastEnter = null;
        lastExit = null;
    }

    private void IncrementMap()
    {
        List<System.Tuple<TunnelSection, int>> links = new(lastEnter.connectorPairs.Values);
        List<TunnelSection> newOneDst = new(links.Count);
        links.ForEach(link => newOneDst.Add(link.Item1));
        HashSet<TunnelSection> newTwoDst = new(links.Count);
        for (int i = 0; i < newOneDst.Count; i++)
        {
            links = new(newOneDst[i].connectorPairs.Values);
            links.ForEach(link => newTwoDst.Add(link.Item1));
        }
        //oneDst.ForEach(section =>
        //{
        //    List<System.Tuple<TunnelSection, int>> links = new(section.connectorPairs.Values);
        //    links.ForEach(link => twoDst.Add(link.Item1));
        //});
        newTwoDst.ExceptWith(newOneDst);
        newTwoDst.Remove(lastEnter);
        HashSet<TunnelSection> oldTwoDst = new(twoDstSections);
        oldTwoDst.ExceptWith(newTwoDst);
        oldTwoDst.ExceptWith(newOneDst);
        List<TunnelSection> cleanUpsections = new(oldTwoDst);
        cleanUpsections.ForEach(section=>DestroySection(section));
        Physics.SyncTransforms();
        newOneDst.Remove(lastEnter);
        oneDstSections.Clear();
        twoDstSections.Clear();
        oneDstSections.AddRange(newOneDst);
        twoDstSections.AddRange(newTwoDst);
        FillTwoDstList(oneDstSections, twoDstSections);
        curPlayerSection = lastEnter;

        TunnelSection[] allSections = GetComponentsInChildren<TunnelSection>();
        for (int i = 0; i < allSections.Length; i++)
        {
            List<System.Tuple<TunnelSection, int>> newLinks = new(allSections[i].connectorPairs.Values);
            for (int l = 0; l < newLinks.Count; l++)
            {
                if (newLinks[l] == null || newLinks[l].Item1 == null)
                {
                    Debug.LogWarning("Null link!");
                }
            }
        }
    }

    /// <summary>
    /// Picks and connects new section to the given tunnel Section primarySection.
    /// </summary>
    /// <param name="oneDstList"></param>
    /// <param name="primarySection"></param>
    private void FillOneDstList(List<TunnelSection> oneDstList, TunnelSection primarySection)
    {
        for (int j = 0; j < primarySection.connectors.Length; j++)
        {
            // Pick Instinate & connect a new section
            TunnelSection sectionInstance = PickInstinateConnect(primarySection);
            oneDstList.Add(sectionInstance); // add this new section to the 1 distance list
        }
    }

    /// <summary>
    /// Picks and connects new sections to sections stored in the 1 distance list.
    /// Adds them to the 2 distance list as these sections will be the 2nd section away from the current.
    /// </summary>
    /// <param name="oneDstList"></param>
    /// <param name="twoDstList"></param>
    private void FillTwoDstList(List<TunnelSection> oneDstList, List<TunnelSection> twoDstList)
    {
        for (int i = 0; i < oneDstList.Count; i++)
        {
            TunnelSection section = oneDstList[i];
            // calculate number of remaining connectors - this is how many sections we need to connect.
            int freeConnectors = section.connectors.Length - section.InUse.Count;
            for (int j = 0; j < freeConnectors; j++)
            {
                // Pick Instinate & connect a new section
                TunnelSection sectionInstance = PickInstinateConnect(section);
                twoDstList.Add(sectionInstance); // add this new section to the 2 distance list
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
        TunnelSection pickedSection = PickSection(primary, out Connector priPref, out Connector secPref);
        TunnelSection pickedInstance = InstinateSection(pickedSection);
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
    private TunnelSection PickSection(TunnelSection primary, out Connector primaryPreference, out Connector secondaryPreference)
    {
        primaryPreference = Connector.Empty;
        secondaryPreference = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);
        List<int> nextSections = FilterSections(primary);

        int iterations = maxInterations;
        TunnelSection targetSection = null;

        while (targetSection == null && primaryConnectors.Count > 0)
        {
            primaryPreference = GetConnectorFromSection(primaryConnectors, out int priIndex);
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
        
        /*
        while (targetSection == null && nextSections.Count > 0)
        {
            int curInstanceID = nextSections.ElementAt(Random.Range(0, nextSections.Count));
            targetSection = instanceIdToSection[curInstanceID];
            if (RunIntersectionTests(primary, targetSection, out primaryPreference, out secondaryPreference))
            {
                break;
            }
            nextSections.Remove(curInstanceID);
            targetSection = null;
            iterations--;
            if (iterations <= 0)
            {
                Debug.LogException(new System.StackOverflowException("Intersection test exceeded max iterations"), this);
            }
        }
        */

        if(targetSection == null)
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
        List<int> nextSections = new(tunnelSectionsByInstanceID);

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
        primaryConnector.UpdateWorldPos(primary.transform.localToWorldMatrix);
        secondaryConnector.UpdateWorldPos(target.transform.localToWorldMatrix);
        
        float4x4 secondaryTransform = CalculateSectionMatrix(primaryConnector, secondaryConnector);

        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];
            float4x4 m = math.mul(secondaryTransform, target.boundingBoxes[i].Matrix);
            if (Physics.CheckBox(m.Translation(), boxBounds.size * 0.5f, m.Rotation(), tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
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
        section.gameObject.SetActive(true);
        section.transform.parent = transform;
        return section;
    }

    private void DestroySection(TunnelSection section)
    {
        List<int> pairKeys = new(section.connectorPairs.Keys);
        pairKeys.ForEach(key =>
        {
            System.Tuple<TunnelSection, int> sectionTwin = section.connectorPairs[key];
            if (sectionTwin != null && sectionTwin.Item1 != null)
            {
                sectionTwin.Item1.connectorPairs[sectionTwin.Item2] = null;
                sectionTwin.Item1.InUse.Remove(sectionTwin.Item2);
                sectionTwin.Item1.connectorPairs.Remove(sectionTwin.Item2);
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
