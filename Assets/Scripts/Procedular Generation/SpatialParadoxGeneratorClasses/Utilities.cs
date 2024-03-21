using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
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
                nextPlayerSection = section.treeElementParent;
                if (incrementalUpdateProcess == null)
                {
                    incrementalUpdateProcess = StartCoroutine(MakeRootNodeIncremental(section.treeElementParent));
                }
                else queuedUpdateProcess ??= StartCoroutine(AwaitCurrentIncrementalComplete());

            }
        }
    }

    private static void ClearConnectors(MapTreeElement section)
    {
        List<int> pairKeys = new(section.ConnectorPairs.Keys);
        pairKeys.ForEach(key =>
        {
            SectionAndConnector sectionTwin = section.ConnectorPairs[key];
            if (sectionTwin != null && sectionTwin.element != null)
            {
                sectionTwin.element.ConnectorPairs[sectionTwin.internalIndex] = null;
                sectionTwin.element.inUse.Remove(sectionTwin.internalIndex);
                sectionTwin.element.ConnectorPairs.Remove(sectionTwin.internalIndex);
            }
        });

        section.ConnectorPairs.Clear();
        section.inUse.Clear();
    }

    private void TransformSection(Transform secondaryTransform, Connector primaryConnector, Connector secondaryConnector)
    {
        float4x4 transformMatrix = CalculateSectionMatrix(primaryConnector, secondaryConnector);
        secondaryTransform.SetPositionAndRotation(transformMatrix.Translation(), transformMatrix.Rotation());
        if(secondaryTransform.TryGetComponent(out TunnelSection section))
        {
            AddSection(section, transformMatrix);
        }
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

    internal List<MapTreeElement> GetMothballedSections()
    {
        HashSet<MapTreeElement> sections = new(new MapTreeElementComparer());

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
        return update ? instanceIdToBakedData[id].UpdateRule() : instanceIdToBakedData[id].Spawnable;
    }

    private int GetSpawnDebt(int id)
    {
        return instanceIdToBakedData[id].SpawnDebt;
    }

    private TunnelSection InstinateSection(int index)
    {
        return InstinateSection(instanceIdToSection[tunnelSectionsByInstanceID[index]]);
    }

    private TunnelSection InstinateSection(TunnelSection tunnelSection, Vector3? position = null, Quaternion? rotation = null)
    {
        TunnelSection section = position.HasValue && rotation.HasValue
            ? Instantiate(tunnelSection, position.Value, rotation.Value, transform)
            : Instantiate(tunnelSection, transform);
        section.gameObject.SetActive(true);
        HandleNewSectionInstance(tunnelSection);
        return section;
    }

    private void HandleNewSectionInstance(TunnelSection tunnelSection)
    {
        if (instanceIdToBakedData.TryGetValue(tunnelSection.orignalInstanceId, out BakedTunnelSection original))
        {
            original.InstanceCount++;
            original.Spawned();
        }
    }

    private void DestroySection(TunnelSection section)
    {
        ClearConnectors(section.treeElementParent);

        if (instanceIdToBakedData.ContainsKey(section.orignalInstanceId))
        {
            instanceIdToBakedData[section.orignalInstanceId].InstanceCount--;
        }
        else
        {
            Debug.LogException(new KeyNotFoundException(string.Format("Key: {0} not present in dictionary instanceIdToSection!", section.orignalInstanceId)), gameObject);
            Debug.LogErrorFormat(gameObject, "Likely section {0} has incorrect instance id of {1}", section.gameObject.name, section.orignalInstanceId);
        }
        
        Destroy(section.gameObject);
    }

    public void LinkSections(MapTreeElement primaryTreeElement, MapTreeElement newTreeElement, int priConnInternalIndex,int newConnInternalIndex)
    {
        primaryTreeElement.ConnectorPairs[priConnInternalIndex] = new(newTreeElement, newConnInternalIndex);
        newTreeElement.ConnectorPairs[newConnInternalIndex] = new(primaryTreeElement, priConnInternalIndex);
        primaryTreeElement.inUse.Add(priConnInternalIndex);
        newTreeElement.inUse.Add(newConnInternalIndex);
    }

    private void TransformSectionAndLink(MapTreeElement primary, MapTreeElement secondary, Connector primaryConnector, Connector secondaryConnector)
    {
        TransformSection(secondary.sectionInstance.transform, primaryConnector, secondaryConnector);

        LinkSections(primary,secondary,primaryConnector.internalIndex,secondaryConnector.internalIndex);
    }

    public Connector GetRandomConnectorFromSection(List<Connector> validConnectors, out int index)
    {
        index = randomNG.NextInt(0, validConnectors.Count);
        return validConnectors[index];
    }

    private List<Connector> FilterConnectorsByOriginalOnly(int instanceId)
    {
        List<Connector> connectors = new(instanceIdToBakedData[instanceId].connectors);

        return connectors;
    }

    private List<Connector> FilterConnectorsByInuse(MapTreeElement element)
    {
        List<Connector> connectors = new(element.Connectors);

        if (element.inUse.Count <= 0)
        {
            return connectors;
        }

        for (int i = connectors.Count - 1; i >= 0; i--)
        {
            if (element.inUse.Contains(connectors[i].internalIndex))
            {
                connectors.RemoveAt(i);
            }
        }

        return connectors;
    }

    private List<int> FilterSections(int originalInstanceId)
    {
        List<int> nextSections = new(tunnelSectionsByInstanceID);
        BakedTunnelSection data = instanceIdToBakedData[originalInstanceId];
        if (data.ExcludePrefabConnectionsIds.Count > 0)
        {
            data.ExcludePrefabConnectionsIds.ForEach(item => nextSections.RemoveAll(element => element == item));
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

    private int GetTotalFreeConnectorCount(List<MapTreeElement> sections)
    {
        int freeConnectors = 0;
        for (int i = 0; i < sections.Count; i++)
        {
            freeConnectors += sections[i].FreeConnectors;
        }
        return freeConnectors;
    }

    private void CheckForSectionPromotions()
    {
        Debug.LogFormat(gameObject, "promoteList: {0} promoteDict: {1}", promoteSectionsList.Count, promoteSectionsDict.Count);
        if (mothBalledSections.Count > 0)
        {
            List<MapTreeElement> mothBalledSections = new(this.mothBalledSections.Keys);
            mothBalledSections.ForEach(section =>
            {
                CheckForPromotable(section);
            });
        }
        Debug.LogFormat(gameObject, "promoteList: {0} promoteDict: {1}", promoteSectionsList.Count, promoteSectionsDict.Count);
    }

    private void CheckForPromotable(MapTreeElement section)
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
                promoteSectionsDict.Add(curDst, new List<MapTreeElement>() { section });
            }
            this.mothBalledSections.Remove(section);
        }
        else
        {
            if (promoteSectionsDict.ContainsKey(curDst) && promoteSectionsDict[curDst].Contains(section))
            {
                HashSet<MapTreeElement> promoteSet = new(promoteSectionsDict[curDst], new MapTreeElementComparer());
                promoteSet.Remove(section);
                promoteSectionsDict[curDst] = new(promoteSet);
            }
        }
    }
}
