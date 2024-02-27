using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class SpatialParadoxGenerator
{
    private void ResolvePlayerSection()
    {
        GameObject player = FindObjectOfType<Improved_Movement>().gameObject;
        if (player.GetComponent<Collider>().Raycast(new(player.transform.position, Vector3.down), out RaycastHit hitInfo, 100f))
        {
            TunnelSection section = hitInfo.collider.gameObject.GetComponentInParent<TunnelSection>();
            section = section != null ? section : hitInfo.collider.gameObject.GetComponentInChildren<TunnelSection>();
            if (section != null)
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

    internal Dictionary<int, Texture2D> GenerateMiniMapTextures()
    {
        Dictionary<int, Texture2D> textureLoopUp = new(tunnelSectionsByInstanceID.Count);
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

        if (mothBalledSections.Count > 0)
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

    private bool Spawnable(int id, bool update = false)
    {
        return update ? instanceIdToSection[id].UpdateRule() : instanceIdToSection[id].Spawnable;
    }

    private int GetSpawnDebt(int id)
    {
        return instanceIdToSection[id].SpawnDebt;
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
            Debug.LogException(new KeyNotFoundException(string.Format("Key: {0} not present in dictionary instanceIdToSection!", section.orignalInstanceId)), gameObject);
            Debug.LogErrorFormat(gameObject, "Likely section {0} has incorrect instance id of {1}", section.gameObject.name, section.orignalInstanceId);
        }

        Destroy(section.gameObject);
    }

    private void TransformSection(TunnelSection primary, TunnelSection secondary, Connector primaryConnector, Connector secondaryConnector)
    {
        TransformSection(secondary.transform, primaryConnector, secondaryConnector);

        primary.connectorPairs[primaryConnector.internalIndex] = new(secondary, secondaryConnector.internalIndex);
        secondary.connectorPairs[secondaryConnector.internalIndex] = new(primary, primaryConnector.internalIndex);
        primary.InUse.Add(primaryConnector.internalIndex);
        secondary.InUse.Add(secondaryConnector.internalIndex);
    }

    public Connector GetConnectorFromSection(List<Connector> validConnectors, out int index)
    {
        index = Random.Range(0, validConnectors.Count);
        return validConnectors[index];
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

    private List<int> FilterSections(TunnelSection primary)
    {
        List<int> nextSections = new(tunnelSectionsByInstanceID);

        if (primary.ExcludePrefabConnections.Count > 0)
        {
            primary.ExcludePrefabConnections.ForEach(item => nextSections.RemoveAll(element => element == item));
        }

        nextSections.RemoveAll(element => !Spawnable(element, true));
        int curLength = nextSections.Count;
        for (int i = 0; i < curLength; i++)
        {
            int debt = GetSpawnDebt(nextSections[i]) - 1;
            for (int j = 0; j < debt; j++)
            {
                nextSections.Add(nextSections[i]);
            }
        }
        return nextSections;
    }

    private List<int> FilterSectionsByConnector(ConnectorMask connector, List<int> sections)
    {
        if (connector.exclude != null)
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

}
