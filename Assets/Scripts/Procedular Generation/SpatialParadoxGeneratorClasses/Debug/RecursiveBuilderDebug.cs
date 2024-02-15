#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpatialParadoxGenerator
{

    /// <summary>
    /// Slow step through increasing the <see cref="mapTree"/> until it is the size of maxDst.
    /// </summary>
    /// <returns></returns>
    private IEnumerator RecursiveBuilderDebug()
    {
        while (mapTree.Count <= maxDst)
        {
            yield return new WaitForSeconds(distanceListPauseTime);
            List<MapTreeElement> startSections = mapTree[^1];

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
    private IEnumerator FillSectionConnectorsDebug(List<MapTreeElement> startSections)
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
            if (startSections[i].Instantiated)
            {
                TunnelSection section = startSections[i].sectionInstance;
                int freeConnectors = section.connectors.Length - section.InUse.Count;
                for (int j = 0; j < freeConnectors; j++)
                {
                    // pick a new section to connect to
                    yield return PickInstinateConnectDebug(section);
                }
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

}

#endif