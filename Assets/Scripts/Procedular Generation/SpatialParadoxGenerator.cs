using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpatialParadoxGenerator : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> tunnelSections;

    [SerializeField] private List<TunnelSection> nextNextPlayerSections=new();
    [SerializeField] private List<TunnelSection> nextPlayerSections = new();

    [SerializeField] private TunnelSection curPlayerSection;

    [SerializeField] private List<TunnelSection> prevPlayerSections = new();
    [SerializeField] private List<TunnelSection> prevPrevPlayerSections = new();
    [SerializeField] private LayerMask tunnelSectionLayerMask;
    private int tunnelSectionLayerIndex;
    [SerializeField, Min(1000)] private int maxInterations = 1000000000;

    private TunnelSection lastEnter;
    private TunnelSection lastExit;

    [SerializeField] private GameObject santiziedCube;
    [SerializeField] private GameObject santiziedCap;

    private void Start()
    {
        tunnelSectionLayerIndex = tunnelSectionLayerMask.value;
        transform.position = Vector3.zero;
        StartCoroutine(GenerateInitialArea());
    }

    private IEnumerator GenerateInitialArea()
    {
        int spawnIndex = Random.Range(0, tunnelSections.Count);
        curPlayerSection = InstinateSection(spawnIndex);
        curPlayerSection.transform.position = new Vector3(0, 0, 0);

        int nextIters = curPlayerSection.connectors.Length / 2;
        int prevIters = curPlayerSection.connectors.Length - nextIters;

        yield return new WaitForSeconds(5);
        yield return FillOneDstListDebug(nextPlayerSections, curPlayerSection, nextIters);
        yield return new WaitForSeconds(5);
        yield return FillOneDstListDebug(prevPlayerSections, curPlayerSection, prevIters);
        yield return new WaitForSeconds(5);
        yield return FillTwoDstListDebug(nextPlayerSections, nextNextPlayerSections);
        yield return new WaitForSeconds(5);
        yield return FillTwoDstListDebug(prevPlayerSections, prevPrevPlayerSections);
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
            if (nextPlayerSections.Contains(lastEnter))
            {
                IncrementForward();
            }
            else if (prevPlayerSections.Contains(lastEnter))
            {
                IncrementBackward();
            }
        }
        lastEnter = null;
        lastExit = null;
    }

    private void IncrementForward()
    {
        // destroy sections now 3 sections back.
        prevPrevPlayerSections.ForEach(section => DestroySection(section));
        prevPrevPlayerSections.Clear();

        // add sections that were 1 section back to the 2 back list.
        prevPrevPlayerSections.AddRange(prevPlayerSections);
        prevPlayerSections.Clear(); // clear the 1 back list

        prevPlayerSections.Add(curPlayerSection); // add the old currentSection to the 1 back list

        curPlayerSection = lastEnter; // update to new currentSection

        nextPlayerSections.Remove(curPlayerSection); // remove the new curSection from the 1 foward list
        // for each remaining item
        nextPlayerSections.ForEach(section =>
        {
            prevPlayerSections.Add(section); // add it to the 1 back list

            // for any links it has to 2 foward sections
            List<System.Tuple<TunnelSection, int>> links = new(section.connectorPairs.Values);
            links.ForEach(link =>
            {
                if (link.Item1 != curPlayerSection) // exclude connection to the new currentSection
                {
                    nextNextPlayerSections.Remove(link.Item1); // remove it from 2 forward
                    prevPrevPlayerSections.Add(link.Item1); // add it to 2 back
                }
            });
        });
        nextPlayerSections.Clear(); // clear 1 forward list
        nextPlayerSections.AddRange(nextNextPlayerSections); // add remaining items of 2 forward list
        nextNextPlayerSections.Clear(); // clear 2 forward list

        FillTwoDstList(nextPlayerSections, nextNextPlayerSections);
    }

    private void IncrementBackward()
    {
        // destroy sections now 3 sections forward.
        nextNextPlayerSections.ForEach(section => DestroySection(section));
        nextNextPlayerSections.Clear();

        // add sections that were 1 section forward to the 2 back list.
        nextNextPlayerSections.AddRange(nextPlayerSections);
        nextPlayerSections.Clear(); // clear the 1 forward list

        nextPlayerSections.Add(curPlayerSection); // add the old currentSection to the 1 forward list

        curPlayerSection = lastEnter; // update to new currentSection

        prevPlayerSections.Remove(curPlayerSection); // remove the new curSection from the 1 back list
                                                     // for each remaining item
        prevPlayerSections.ForEach(section =>
        {
            nextPlayerSections.Add(section); // add it to the 1 forward list

            // for any links it has to 2 foward sections
            List<System.Tuple<TunnelSection, int>> links = new(section.connectorPairs.Values);
            links.ForEach(link =>
            {
                if (link.Item1 != curPlayerSection) // exclude connection to the new currentSection
                {
                    prevPrevPlayerSections.Remove(link.Item1); // remove it from 2 back
                    nextNextPlayerSections.Add(link.Item1); // add it to 2 forward
                }
            });
        });
        prevPlayerSections.Clear(); // clear 1 back list
        prevPlayerSections.AddRange(prevPrevPlayerSections); // add remaining items of 2 back list
        prevPrevPlayerSections.Clear(); // clear 2 back list

        FillTwoDstList(prevPlayerSections, prevPrevPlayerSections);
    }

    private void FillOneDstList(List<TunnelSection> oneDstList, TunnelSection primarySection, int iterations)
    {
        for (int j = 0; j < iterations; j++)
        {
            // pick a new section to connect to
            TunnelSection newSection = PickSection(primarySection, out Connector priPref, out Connector secPref);
            oneDstList.Add(newSection);
            TransformSection(primarySection, newSection, priPref, secPref); // position new section
        }
    }

    private IEnumerator FillOneDstListDebug(List<TunnelSection> oneDstList, TunnelSection primarySection, int iterations)
    {
        for (int j = 0; j < iterations; j++)
        {
            // pick a new section to connect to
            yield return PickSection(primarySection);
            TunnelSection sectionInstance = InstinateSection(targetSectionDebug);
            oneDstList.Add(sectionInstance);
            TransformSection(primarySection, sectionInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // position new section
        }
        yield return null;
    }

    private void FillTwoDstList(List<TunnelSection> oneDstList, List<TunnelSection> twoDstList)
    {
        for (int i = 0; i < oneDstList.Count; i++) // for each item in 1 back
        {
            TunnelSection section = oneDstList[i];
            // for each connector -1 (exlucde connection to current section)
            for (int j = 0; j < section.connectors.Length - 1; j++)
            {
                // pick a new section to connect to
                TunnelSection newSection = PickSection(section, out Connector priPref, out Connector secPref);
                twoDstList.Add(newSection); // add this to 2 back
                TransformSection(section, newSection, priPref, secPref); // position new section
            }
        }
    }

    private IEnumerator FillTwoDstListDebug(List<TunnelSection> oneDstList, List<TunnelSection> twoDstList)
    {
        for (int i = 0; i < oneDstList.Count; i++) // for each item in 1 back
        {
            TunnelSection section = oneDstList[i];
            // for each connector -1 (exlucde connection to current section)
            for (int j = 0; j < section.connectors.Length - 1; j++)
            {
                // pick a new section to connect to
                yield return PickSection(section);

                TunnelSection sectionInstance = InstinateSection(targetSectionDebug);

                twoDstList.Add(sectionInstance); // add this to 2 back
                TransformSection(section, sectionInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // position new section
            }
        }
    }

    private TunnelSection PickSection(TunnelSection primary,  out Connector primaryPreference, out Connector secondaryPreference)
    {
        primaryPreference = Connector.Empty;
        secondaryPreference = Connector.Empty;
        List<TunnelSection> nextSections = new(tunnelSections);

        if (primary.ExcludePrefabConnections.Count > 0)
        {
            primary.ExcludePrefabConnections.ForEach(item => nextSections.RemoveAll(element => element == item));
        }
        int iterations = maxInterations;
        TunnelSection targetSection = null;
        while (targetSection == null)
        {
            targetSection = nextSections.ElementAt(Random.Range(0, nextSections.Count));
            if (IntersectionTest(primary, targetSection, out primaryPreference, out secondaryPreference))
            {
                break;
            }
            targetSection = null;
            iterations--;
            if (iterations <= 0)
            {
                throw new System.StackOverflowException("Failed to find section that passed Intersection Test");
            }
        }

        return targetSection;
    }

    private TunnelSection targetSectionDebug;
    private Connector primaryPreferenceDebug;
    private Connector secondaryPreferenceDebug;
    private bool intersectionTest;

    private IEnumerator PickSection(TunnelSection primary)
    {
        primaryPreferenceDebug = Connector.Empty;
        secondaryPreferenceDebug = Connector.Empty;
        List<TunnelSection> nextSections = new(tunnelSections);

        if (primary.ExcludePrefabConnections.Count > 0)
        {
            primary.ExcludePrefabConnections.ForEach(item => nextSections.RemoveAll(element => element == item));
        }
        int iterations = maxInterations;
        targetSectionDebug = null;
        while (targetSectionDebug == null)
        {
            targetSectionDebug = nextSections.ElementAt(Random.Range(0, nextSections.Count));
            yield return IntersectionTestDebug(primary, targetSectionDebug);
            if (intersectionTest)
            {
                break;
            }
            targetSectionDebug = null;
            iterations--;
            if (iterations <= 0)
            {
                throw new System.StackOverflowException("Failed to find section that passed Intersection Test");
            }
        }

        yield return null;
    }

    private bool IntersectionTest(TunnelSection primary, TunnelSection target, out Connector primaryConnector, out Connector secondaryConnector)
    {
        primaryConnector = Connector.Empty;
        secondaryConnector = Connector.Empty;
        HashSet<int> primaryConnectors = new();
        HashSet<int> secondaryConnectors = new();
        while (secondaryConnectors.Count != target.connectors.Length)
        {
            primaryConnector = GetConnectorFromSection(primary, primaryConnectors);
            secondaryConnector = GetConnectorFromSection(target, secondaryConnectors);

            primaryConnector.UpdateWorldPos(primary.transform.localToWorldMatrix);
            secondaryConnector.UpdateWorldPos(target.transform.localToWorldMatrix);


            CalculateSectionTransform(primaryConnector, secondaryConnector, out Vector3 pos, out Quaternion rot);
            bool noIntersections = true;
            for (int i = 0; i < target.BoundingBoxes.Length; i++)
            {
                BoxBounds boxBounds = target.BoundingBoxes[i];
                if (Physics.CheckBox(pos + boxBounds.center, boxBounds.size, rot, tunnelSectionLayerIndex))
                {
                    noIntersections = false;
                    break;
                }
            }

            for (int i = 0; i < target.BoundingCaps.Length; i++)
            {
                CapsuleBounds capBounds = target.BoundingCaps[i];
                if (Physics.CheckCapsule(pos + capBounds.center, pos, capBounds.radius, tunnelSectionLayerIndex))
                {
                    noIntersections = false;
                    break;
                }
            }
            if (noIntersections)
            {
                return true;
            }
            else
            {
                secondaryConnectors.Add(secondaryConnector.internalIndex);
            }
        }
        

        return false;
    }
    private IEnumerator IntersectionTestDebug(TunnelSection primary, TunnelSection target)
    {
        primaryPreferenceDebug = Connector.Empty;
        secondaryPreferenceDebug = Connector.Empty;
        HashSet<int> primaryConnectors = new();
        HashSet<int> secondaryConnectors = new();
        while (secondaryConnectors.Count != target.connectors.Length)
        {
            primaryPreferenceDebug = GetConnectorFromSection(primary, primaryConnectors);
            secondaryPreferenceDebug = GetConnectorFromSection(target, secondaryConnectors);

            primaryPreferenceDebug.UpdateWorldPos(primary.transform.localToWorldMatrix);
            secondaryPreferenceDebug.UpdateWorldPos(target.transform.localToWorldMatrix);


            List<GameObject> objects = new();
            CalculateSectionTransform(primaryPreferenceDebug, secondaryPreferenceDebug, out Vector3 pos, out Quaternion rot);

            Matrix4x4  parentTransform = Matrix4x4.TRS(pos,rot, Vector3.one);

            //objects.Add(Instantiate(target,pos,rot).gameObject);

            bool noIntersections = true;
            for (int i = 0; i < target.BoundingBoxes.Length; i++)
            {
                BoxBounds boxBounds = target.BoundingBoxes[i];
                objects.Add(Instantiate(santiziedCube));

                Matrix4x4 m = parentTransform * target.boundingBoxes[i].Matrix;

                objects[^1].transform.SetPositionAndRotation( m.GetPosition(),m.rotation);
                objects[^1].transform.localScale = boxBounds.size;
                objects[^1].GetComponent<MeshRenderer>().material.color = Color.green;
                if (Physics.CheckBox(m.GetPosition(), boxBounds.size*0.5f, m.rotation, tunnelSectionLayerIndex,QueryTriggerInteraction.Ignore))
                {
                    objects[^1].GetComponent<MeshRenderer>().material.color = Color.red;
                    noIntersections = false;
                }
            }

            for (int i = 0; i < target.BoundingCaps.Length; i++)
            {
                CapsuleBounds capBounds = target.BoundingCaps[i];
                objects.Add(Instantiate(santiziedCap));
                objects[^1].transform.SetPositionAndRotation(pos + capBounds.center, rot);
                objects[^1].transform.localScale = new Vector3(capBounds.radius,capBounds.height,capBounds.radius);
                objects[^1].GetComponent<MeshRenderer>().material.color = Color.green;
                if (Physics.CheckCapsule(pos + capBounds.center, pos, capBounds.radius))
                {
                    objects[^1].GetComponent<MeshRenderer>().material.color = Color.red;
                    noIntersections = false;
                }
            }

            //objects[0].SetActive(true);
            yield return new WaitForSeconds(2.5f);
            objects.ForEach(ob=>Destroy(ob));

            if (noIntersections)
            {
                intersectionTest = true;
                yield break;
            }
            else
            {
                secondaryConnectors.Add(secondaryPreferenceDebug.internalIndex);
            }
        }

        intersectionTest = false;
        yield break;
    }

    public Connector GetConnectorFromSection(TunnelSection section,HashSet<int> excludedConnectors)
    {
        
        int iterations = maxInterations;
        while (true)
        {
            if (excludedConnectors.Count == section.connectors.Length)
            {
                return Connector.Empty;
            }
            iterations--;
            if(iterations <= 0)
            {
                throw new System.StackOverflowException("Failed to find avalible connector");
            }
            int connectorIndex = Random.Range(0,section.connectors.Length);
            if (section.InUse.Contains(connectorIndex) || excludedConnectors.Contains(connectorIndex))
            {
                continue;
            }
            return section.connectors[connectorIndex];
        }
    }

    private TunnelSection InstinateSection(int index)
    {
        return InstinateSection(tunnelSections.ElementAt(index));
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
            }
        });
        Destroy(section.gameObject);
    }

    private static void TransformSection(TunnelSection primary, TunnelSection secondary, Connector primaryConnector, Connector secondaryConnector)
    {
        CalculateSectionTransform(primaryConnector, secondaryConnector, out Vector3 pos, out Quaternion rot);
        secondary.transform.SetPositionAndRotation(pos, rot);

        primary.connectorPairs[primaryConnector.internalIndex] = new(secondary, secondaryConnector.internalIndex);
        secondary.connectorPairs[secondaryConnector.internalIndex] = new(primary, primaryConnector.internalIndex);
        primary.InUse.Add(primaryConnector.internalIndex);
        secondary.InUse.Add(secondaryConnector.internalIndex);
    }

    private static void CalculateSectionTransform(Connector primary, Connector secondary, out Vector3 position, out Quaternion rotation)
    {
        Vector3 offset = secondary.localPosition;
        rotation = ExtraUtilities.BetweenDirections(secondary.Back, primary.Forward);
        offset = rotation.RotatePosition(offset);

        position = primary.position - offset;
        position.y = primary.parentPos.y + (primary.position.y - secondary.position.y);
    }
}
