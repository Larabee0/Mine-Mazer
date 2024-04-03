using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator
{
    private void UpdateMap()
    {
        if (lastExit == curPlayerSection && lastEnter != curPlayerSection && mapUpdateProcess == null)
        {
            mapUpdateProcess = StartCoroutine(DelayedMapGen(lastEnter));
        }
        lastEnter = null;
        lastExit = null;
    }

    private IEnumerator DelayedMapGen(MapTreeElement newSection)
    {
        yield return null;

        nextPlayerSection = newSection;
        if (incrementalUpdateProcess == null)
        {
            incrementalUpdateProcess = StartCoroutine(MakeRootNodeIncremental(newSection));
        }
        else queuedUpdateProcess ??= StartCoroutine(AwaitCurrentIncrementalComplete());

        mapUpdateProcess = null;
        AmbientController.Instance.FadeAmbientLight(newSection.sectionInstance.AmbientLightLevel);
        AmbientController.Instance.ChangeTune(newSection.sectionInstance.AmbientNoise);
        OnMapUpdate?.Invoke();
    }

    private IEnumerator AwaitCurrentIncrementalComplete()
    {
        while (incrementalUpdateProcess != null)
        {
            yield return null;
        }
        incrementalUpdateProcess = StartCoroutine(MakeRootNodeIncremental(nextPlayerSection));
        queuedUpdateProcess = null;
    }

    private IEnumerator RegenRingIncremental(int regenTarget)
    {
        regenTarget -= reRingInters;

        if (regenTarget < 2)
        {
            throw new System.InvalidOperationException(string.Format("Regeneration target set to {0} Cannot regenerate root node! Something catastrophic occured!", regenTarget));
        }

        while (mapTree.Count - 1 != regenTarget)
        {
            for (int i = 0; i < mapTree[^1].Count; i++)
            {
                if (mapTree[^1][i].Instantiated)
                {
                    MapTreeElement section = mapTree[^1][i];
                    MothballInRing(section);
                }
            }
            mapTree.RemoveAt(mapTree.Count - 1);
        }
        CheckForSectionPromotions();
        reRingInters = 1;
        yield return IncrementalBuilder();
        reRingInters = 0;
    }

    private void MothballInRing(MapTreeElement section)
    {
        if (section.Keep)
        {
            section.sectionInstance.gameObject.SetActive(false);
            section.sectionInstance.transform.parent = sectionGraveYard;
            ClearConnectors(section);
            mothBalledSections.Add(section, new(math.distancesq(curPlayerSection.LocalToWorld.Translation(), section.LocalToWorld.Translation()), mapTree.Count - 1));

            totalDecorations -= section.sectionInstance.decorationCount;
        }
        else
        {
            section.sectionInstance.CollidersEnabled = false;
            DestroySectionPhysicsWorld(section.UID);
            DestroySection(section.sectionInstance);
        }
    }

    private IEnumerator MakeRootNodeIncremental(MapTreeElement newRoot)
    {
        if (curPlayerSection == newRoot)
        {
            nextPlayerSection = null;
            incrementalUpdateProcess = null;
            yield break;
        }

        UpdateMothBalledSections(newRoot);

        List<List<MapTreeElement>> newTree = RebuildTreeNew(newRoot);

        if (mapProfiling) Debug.LogFormat("New Tree Size {0}", newTree.Count);
        if (mapProfiling) Debug.LogFormat("Original Tree Size {0}", mapTree.Count);

        bool forceGrow = newTree.Count < mapTree.Count;

        PruneTree(newRoot, newTree);

        yield return GrowTree(forceGrow);

        UpdatePlayerSection(newTree);
        
        LODMap();

        if (newRoot == nextPlayerSection)
        {
            nextPlayerSection = null;
        }
        incrementalUpdateProcess = null;

        OnMapUpdate?.Invoke();
    }

    private void LODMap()
    {
        if (ringRenderDst < maxDst)
        {
            for (int i = 0; i < ringRenderDst; i++)
            {
                mapTree[i].ForEach(section => section.SetRenderersEnabled(true));
            }
        }
    }

    private void UpdatePlayerSection(List<List<MapTreeElement>> newTree)
    {
        curPlayerSection = newTree[0][0];
        if (curPlayerSection == null)
        {
            Debug.LogWarning("cur player section null, attempting to resolve..");
            ResolvePlayerSection();
        }
    }

    private IEnumerator GrowTree(bool forceGrow)
    {
        if (mapProfiling) Debug.Log("Growing Tree..");
        int oldSize = 0;
        if (forceGrow)
        {
            yield return IncrementalBuilder();
        }
        else
        {
            oldSize = mapTree[^1].Count;
            yield return FillSectionConnectorsIncremental(mapTree[^2]);
            yield return PreProcessQueue();
        }
        if (mapProfiling) Debug.LogFormat("Grew {0} leaves", mapTree[^1].Count - oldSize);
    }

    private void PruneTree(MapTreeElement newRoot, List<List<MapTreeElement>> newTree)
    {
        if (mapProfiling) Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
                if (newTree[^1][i].Instantiated)
                {
                    MapTreeElement section = newTree[^1][i];
                    if (section.Keep)
                    {
                        section.sectionInstance.gameObject.SetActive(false);
                        section.sectionInstance.transform.parent = sectionGraveYard;
                        if (!mothBalledSections.ContainsKey(section))
                        {
                            ClearConnectors(newTree[^1][i]);
                            mothBalledSections.Add(newTree[^1][i], new(math.distancesq(newRoot.sectionInstance.Position, section.sectionInstance.Position), newTree.Count - 1));
                        }
                    }
                    else
                    {
                        leafCounter++;
                        DestroySectionPhysicsWorld(newTree[^1][i].UID);
                        DestroySection(section.sectionInstance);
                    }
                }
                else
                {
                    newTree[^1][i].cancel = true;
                }
            }
            newTree[^1].Clear();
            newTree.RemoveAt(newTree.Count - 1);
        }
        mapTree.Clear();
        mapTree.AddRange(newTree);
    }

    private List<List<MapTreeElement>> RebuildTreeNew(MapTreeElement newRoot)
    {
        List<List<MapTreeElement>> newTree = new() { new() { newRoot } };
        HashSet<MapTreeElement> exceptWith = new(newTree[^1], new MapTreeElementComparer());

        RecursiveTreeBuilder(newTree, exceptWith);
        return newTree;
    }

    private void UpdateMothBalledSections(MapTreeElement newRoot)
    {
        if (mothBalledSections.Count > 0)
        {
            List<MapTreeElement> mothBalledSections = new(this.mothBalledSections.Keys);
            mothBalledSections.ForEach(section =>
            {
                SectionDstData cur = this.mothBalledSections[section];
                SectionDstData newRootDstData = new(math.distancesq(newRoot.LocalToWorld.Translation(), section.LocalToWorld.Translation()), cur.dst);

                newRootDstData.dst += cur.sqrDst < newRootDstData.sqrDst ? 1 : -1;
                Debug.LogFormat("Updated mothballed section distance: {1} DST: {0}", newRootDstData.dst, section.sectionInstance.name);
                this.mothBalledSections[section] = newRootDstData;
            });

            CheckForSectionPromotions();
        }
    }
}
