using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpatialParadoxGenerator
{
    private IEnumerator IncrementalBuilder(bool initialArea = false)
    {
        yield return null;
        yield return null;
        while (mapTree.Count <= maxDst)
        {
            List<MapTreeElement> startSections = mapTree[^1];
            mapTree.Add(new());
            yield return FillSectionConnectorsIncremental(startSections);
            yield return PreProcessQueue();
            if (initialArea)
            {
                rejectBreakableWallAtConnections = false;
            }
        }
        OnMapUpdate?.Invoke();
        if (runPostProcessLast)
        {
            yield return PostProcessQueue();
        }
        if (breakEditorAfterInitialGen)
        {
            Debug.Log(randomNG.state);
            yield return BreakEditor();
        }
    }

    private IEnumerator FillSectionConnectorsIncremental(List<MapTreeElement> startElements)
    {
        if (promoteSectionsDict.Count > 0 && promoteSectionsDict.ContainsKey(mapTree.Count - 1))
        {
            int freeConnectors = GetTotalFreeConnectorCount(startElements);
            if (freeConnectors < promoteSectionsDict[mapTree.Count - 1].Count)
            {
                while (SectionsInProcessingQueue)
                {
                    yield return null;
                }
                // not enough free connectors, go in a level and regenerate.
                Debug.LogWarningFormat("Regening ring {0} it does not have enough free connectors.", mapTree.Count - 2);
                yield return RegenRingIncremental(mapTree.Count - 2);
                yield break;
            }
            else
            {
                promoteSectionsList.AddRange(promoteSectionsDict[mapTree.Count - 1]);
                promoteSectionsDict.Remove(mapTree.Count - 1);
            }
        }
        yield return FillElementsMain(startElements);

        if (GetTotalFreeConnectorCount(mapTree[^1]) == 0)
        {
            while (SectionsInProcessingQueue)
            {
                yield return null;
            }
            yield return RegenRingIncremental(mapTree.Count - 2);
            yield break;
        }

        UpdateMapLOD();
    }

    private void UpdateMapLOD()
    {
        if (ringRenderDst < maxDst && mapTree.Count > ringRenderDst)
        {
            mapTree[^1].ForEach(section => section.SetRenderersEnabled(false));
        }
        if (DisableLOD)
        {
            mapTree[^1].ForEach(section => section.SetRenderersEnabled(true));
        }
    }

    private IEnumerator FillElementsMain(List<MapTreeElement> startElements)
    {
        for (int i = 0; i < startElements.Count; i++)
        {
            MapTreeElement startElement = startElements[i];
            int freeConnectors = startElement.FreeConnectors;
            for (int j = 0; j < freeConnectors; j++)
            {
                PickIntstinateConnectDelayed results = new()
                {
                    pickSectionDelayedData = new()
                };
                yield return PickInstinateConnectDelayed(startElement, results);
                
                MapTreeElement sectionElement = results.treeEleement;
                LinkSections(startElement,sectionElement, results.pickSectionDelayedData.primaryPreference.internalIndex, results.pickSectionDelayedData.secondaryPreference.internalIndex);
                mapTree[^1].Add(sectionElement);
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
    }

    private void RecursiveTreeBuilder(List<List<MapTreeElement>> recursiveConstruction, HashSet<MapTreeElement> exceptWith)
    {
        exceptWith.UnionWith(recursiveConstruction[^1]);

        HashSet<MapTreeElement> dstOne = new(new MapTreeElementComparer());
        List<MapTreeElement> curList = recursiveConstruction[^1];
        for (int i = 0; i < curList.Count; i++)
        {
            MapTreeElement sec = curList[i];
            List<SectionAndConnector> SandCs = new(sec.ConnectorPairs.Values);
            for (int j = 0; j < SandCs.Count; j++)
            {
                SectionAndConnector SandC = SandCs[j];
                if (!exceptWith.Contains(SandC.element))
                {
                    dstOne.Add(SandC.element);
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
}
