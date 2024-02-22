using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpatialParadoxGenerator
{

    private void RecursiveBuilder(bool initialArea = false)
    {
        while (mapTree.Count <= maxDst)
        {
            List<MapTreeElement> startSections = mapTree[^1];
            mapTree.Add(new());
            FillSectionConnectors(startSections);
            if (initialArea)
            {
                rejectBreakableWallAtConnections = false;
            }
        }
    }
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
        //yield return PostProcessQueue();
        //yield return BreakEditor();
    }


    private IEnumerator FillSectionConnectorsIncremental(List<MapTreeElement> startElements)
    {
        if (promoteSectionsDict.Count > 0 && promoteSectionsDict.ContainsKey(mapTree.Count - 1))
        {
            int freeConnectors = GetFreeConnectorCount(startElements);
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
        if (GetFreeConnectorCount(mapTree[^1]) == 0)
        {
            while (SectionsInProcessingQueue)
            {
                yield return null;
            }
            yield return RegenRingIncremental(mapTree.Count - 2);
            yield break;
        }
        if (ringRenderDst < maxDst && mapTree.Count > ringRenderDst)
        {
            mapTree[^1].ForEach(section => section.SetRenderersEnabled(false));
        }
    }


    private void FillSectionConnectors(List<MapTreeElement> startSections)
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
            if (startSections[i].Instantiated)
            {
                TunnelSection section = startSections[i].sectionInstance;
                int freeConnectors = section.connectors.Length - section.InUse.Count;
                for (int j = 0; j < freeConnectors; j++)
                {
                    TunnelSection sectionInstance = PickInstinateConnect(section);
                    mapTree[^1].Add(sectionInstance);
                }
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

    private void RecursiveTreeBuilder(List<List<TunnelSection>> recursiveConstruction, HashSet<TunnelSection> exceptWith)
    {
        exceptWith.UnionWith(recursiveConstruction[^1]);

        HashSet<TunnelSection> dstOne = new();
        List<TunnelSection> curList = recursiveConstruction[^1];
        for (int i = 0; i < curList.Count; i++)
        {
            if (curList[i])
            {
                TunnelSection sec = curList[i];
                List<SectionAndConnector> SandCs = new(sec.connectorPairs.Values);
                for (int j = 0; j < SandCs.Count; j++)
                {
                    SectionAndConnector SandC = SandCs[j];
                    if (!exceptWith.Contains(SandC.SectionInstance))
                    {
                        dstOne.Add(SandC.SectionInstance);
                    }
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

    private IEnumerator IncrementalTreeBuilder(List<List<MapTreeElement>> recursiveConstruction, HashSet<MapTreeElement> exceptWith)
    {
        exceptWith.UnionWith(recursiveConstruction[^1]);

        HashSet<MapTreeElement> dstOne = new();
        List<MapTreeElement> curList = recursiveConstruction[^1];
        for (int i = 0; i < curList.Count; i++)
        {
            if (curList[i].Instantiated)
            {
                TunnelSection sec = curList[i].sectionInstance;
                List<SectionAndConnector> SandCs = new(sec.connectorPairs.Values);
                for (int j = 0; j < SandCs.Count; j++)
                {
                    SectionAndConnector SandC = SandCs[j];
                    if (!exceptWith.Contains(SandC.SectionInstance))
                    {
                        dstOne.Add(SandC.SectionInstance);
                    }
                }
            }
        }
        dstOne.ExceptWith(exceptWith);
        if (dstOne.Count > 0)
        {
            recursiveConstruction.Add(new(dstOne));
            yield return IncrementalTreeBuilder(recursiveConstruction, exceptWith);
        }
    }
}
