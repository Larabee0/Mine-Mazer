#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator
{

    private IEnumerator RegenRingDebug(int regenTarget)
    {
        //yield return BreakEditor();
        while (mapTree.Count - 1 != regenTarget)
        {
            Debug.LogFormat(gameObject, "Cleaning up {0} sections", mapTree[^1].Count);
            for (int i = 0; i < mapTree[^1].Count; i++)
            {
                yield return new WaitForSeconds(intersectTestHoldTime);
                if (mapTree[^1][i].Instantiated)
                {
                    TunnelSection section = mapTree[^1][i].sectionInstance;
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

        
        
        yield return new WaitForSeconds(intersectTestHoldTime);

        Debug.Log("Building new Tree..");
        List<List<MapTreeElement>> newTree = RebuildTree(newRoot);

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
                if (newTree[^1][i].Instantiated)
                {
                    TunnelSection section = newTree[^1][i].sectionInstance;
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
        curPlayerSection = newTree[0][0].sectionInstance;
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

}
#endif
