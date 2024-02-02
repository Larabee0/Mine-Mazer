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
            double startTime = Time.realtimeSinceStartupAsDouble;
            MakeRootNode(newSection);
            if (mapProfiling) Debug.LogFormat("Map Update Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f);
        }
#else
            MakeRootNode(newSection);
#endif
        mapUpdateProcess = null;
        AmbientLightController.Instance.FadeAmbientLight(newSection.AmbientLightLevel);
        OnMapUpdate?.Invoke();
        //yield return null;
        //Debug.Break();
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
                TunnelSection section = mapTree[^1][i];
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
            mapTree.RemoveAt(mapTree.Count - 1);
        }
        Physics.SyncTransforms();
        CheckForSectionsPromotions();
        reRingInters = 1;
        RecursiveBuilder();
        reRingInters = 0;
    }

    private void MakeRootNode(TunnelSection newRoot)
    {
        UpdateMothBalledSections(newRoot);

        List<List<TunnelSection>> newTree = new() { new() { newRoot } };
        HashSet<TunnelSection> exceptWith = new(newTree[^1]);

        RecursiveTreeBuilder(newTree, exceptWith);
        if (mapProfiling) Debug.LogFormat("New Tree Size {0}", newTree.Count);
        if (mapProfiling) Debug.LogFormat("Original Tree Size {0}", mapTree.Count);

        bool forceGrow = newTree.Count < mapTree.Count;

        if (mapProfiling) Debug.Log("Pruning Tree..");
        int leafCounter = 0;
        while (newTree.Count > maxDst + 1)
        {
            for (int i = 0; i < newTree[^1].Count; i++)
            {
                TunnelSection section = newTree[^1][i];
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
        curPlayerSection = newTree[0][0];
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
