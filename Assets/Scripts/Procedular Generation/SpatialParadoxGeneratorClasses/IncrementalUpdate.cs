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
            if (incrementalBuilder)
            {
                nextPlayerSection = newSection;
                if (incrementalUpdateProcess == null)
                {
                    incrementalUpdateProcess = StartCoroutine(MakeRootNodeIncremental(newSection));
                }
                else queuedUpdateProcess ??= StartCoroutine(AwaitCurrentIncrementalComplete());
            }
            else 
            {
                double startTime = Time.realtimeSinceStartupAsDouble;
                MakeRootNode(newSection);
                if (mapProfiling) Debug.LogFormat("Map Update Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
            }
        }
#else
            MakeRootNode(newSection);
#endif
        mapUpdateProcess = null;
        AmbientController.Instance.FadeAmbientLight(newSection.AmbientLightLevel);
        AmbientController.Instance.ChangeTune(newSection.AmbientNoise);
        OnMapUpdate?.Invoke();
        //yield return null;
        //Debug.Break();
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

    private void RegenRing(int regenTarget)
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
                        section.CollidersEnabled = false;
                        DestroySection(section);
                    }
                }
            }
            mapTree.RemoveAt(mapTree.Count - 1);
        }
        Physics.SyncTransforms();
        CheckForSectionsPromotions();
        reRingInters = 1;
        RecursiveBuilder();
        reRingInters = 0;
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
                        section.CollidersEnabled = false;
                        DestroySection(section);
                    }
                }
            }
            mapTree.RemoveAt(mapTree.Count - 1);
        }
        CheckForSectionsPromotions();
        reRingInters = 1;
        yield return IncrementalBuilder();
        reRingInters = 0;
    }

    private IEnumerator MakeRootNodeIncremental(TunnelSection newRoot)
    {
        if (curPlayerSection == newRoot)
        {
            nextPlayerSection = null;
            incrementalUpdateProcess = null;
            yield break;
        }
        UpdateMothBalledSections(newRoot);
        List<List<MapTreeElement>> newTree = RebuildTree(newRoot);

        if (mapProfiling) Debug.LogFormat("New Tree Size {0}", newTree.Count);
        if (mapProfiling) Debug.LogFormat("Original Tree Size {0}", mapTree.Count);

        bool forceGrow = newTree.Count < mapTree.Count;

        if (mapProfiling) Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
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
            newTree[^1].Clear();
            newTree.RemoveAt(newTree.Count - 1);
        }
        mapTree.Clear();
        mapTree.AddRange(newTree);

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
        curPlayerSection = newTree[0][0].sectionInstance;
        if (curPlayerSection == null)
        {
            Debug.LogWarning("cur player section null, attempting to resolve..");
            ResolvePlayerSection();
        }

        if (ringRenderDst < maxDst)
        {
            for (int i = 0; i < ringRenderDst; i++)
            {
                mapTree[i].ForEach(section => section.SetRenderersEnabled(true));
            }
        }

        if (newRoot == nextPlayerSection)
        {
            nextPlayerSection = null;
        }
        incrementalUpdateProcess = null;

        OnMapUpdate?.Invoke();
    }

    private List<List<MapTreeElement>> RebuildTree(TunnelSection newRoot)
    {
        List<List<TunnelSection>> tempTree = new() { new() { newRoot } };
        HashSet<TunnelSection> exceptWith = new(tempTree[^1]);

        RecursiveTreeBuilder(tempTree, exceptWith);
        List<List<MapTreeElement>> newTree = new(tempTree.Count);
        tempTree.ForEach(element =>
        {
            newTree.Add(new(element.Count));
            element.ForEach(leaf => newTree[^1].Add(leaf));
        });

        tempTree.Clear();
        tempTree = null;
        return newTree;
    }

    private void MakeRootNode(TunnelSection newRoot)
    {
        UpdateMothBalledSections(newRoot);

        List<List<MapTreeElement>> newTree = RebuildTree(newRoot);
        if (mapProfiling) Debug.LogFormat("New Tree Size {0}", newTree.Count);
        if (mapProfiling) Debug.LogFormat("Original Tree Size {0}", mapTree.Count);

        bool forceGrow = newTree.Count < mapTree.Count;

        if (mapProfiling) Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
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
            newTree[^1].Clear();
            newTree.RemoveAt(newTree.Count - 1);
        }
        Physics.SyncTransforms();

        mapTree.Clear();
        mapTree.AddRange(newTree);

        if (mapProfiling) Debug.Log("Growing Tree..");
        int oldSize = 0;
        if (forceGrow)
        {
            RecursiveBuilder();
        }
        else
        {
            oldSize = mapTree[^1].Count;
            FillSectionConnectors(mapTree[^2]);
        }
        if (mapProfiling) Debug.LogFormat("Grew {0} leaves", mapTree[^1].Count - oldSize);
        curPlayerSection = newTree[0][0].sectionInstance;
        if (curPlayerSection == null)
        {
            Debug.LogWarning("cur player section null, attempting to resolve..");
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
                Debug.LogFormat("Updated mothballed section distance: {1} DST: {0}", newRootDstData.dst, section.name);
                this.mothBalledSections[section] = newRootDstData;
            });

            CheckForSectionsPromotions();
        }
    }
}
