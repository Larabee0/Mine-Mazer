#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class SpatialParadoxGenerator
{

    private IEnumerator PickInstinateConnectDebug(TunnelSection primary)
    {
        TunnelSection pickedInstance = null;
        Connector priPref = Connector.Empty, secPref = Connector.Empty;
        if (promoteSectionsList.Count > 0)
        {
            List<int> internalSections = new(promoteSectionsList.Count);
            promoteSectionsList.ForEach(section => internalSections.Add(section.orignalInstanceId));
            yield return PickSectionDebug(primary, internalSections);
            if (!internalSections.Contains(targetSectionDebug.GetInstanceID())) // returned dead end.
            {
                List<int> nextSections = FilterSections(primary);
                yield return PickSectionDebug(primary, nextSections);
                pickedInstance = InstinateSection(targetSectionDebug);
            }
            else // reinstance section
            {
                int index = internalSections.IndexOf(targetSectionDebug.GetInstanceID());
                pickedInstance = promoteSectionsList[index];
                pickedInstance.gameObject.SetActive(true);
                promoteSectionsList.RemoveAt(index);
            }
        }
        else
        {
            List<int> nextSections = FilterSections(primary);
            yield return PickSectionDebug(primary, nextSections);
            pickedInstance = InstinateSection(targetSectionDebug);
        }

        TransformSection(primary, pickedInstance, primaryPreferenceDebug, secondaryPreferenceDebug); // transform the new section
        Physics.SyncTransforms(); // push changes to physics world now instead of next fixed update

        mapTree[^1].Add(pickedInstance); // add this to 2 back
    }

    private IEnumerator PickSectionDebug(TunnelSection primary, List<int> nextSections)
    {
        primaryPreferenceDebug = Connector.Empty;
        secondaryPreferenceDebug = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);
        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.Persistent);
        int iterations = maxInterations;

        targetSectionDebug = null;

        while (targetSectionDebug == null && primaryConnectors.Count > 0)
        {
            primaryPreferenceDebug = GetConnectorFromSection(primaryConnectors, out int priIndex);

            double startTime = Time.realtimeSinceStartupAsDouble;
            NativeReference<BurstConnector> priConn = new(new(primaryPreferenceDebug), Allocator.TempJob);

            JobHandle handle = new BurstConnectorMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());
            handle = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectorContainers,
                boxBounds = sectionBoxMatrices,
                matrices = sectionBoxTransforms
            }.Schedule(nextSections.Count, handle);
            priConn.Dispose(handle).Complete();
            Debug.LogFormat("Big Matrix: {1} Time {0}ms", (Time.realtimeSinceStartupAsDouble - startTime) * 1000f, nextSections.Count);

            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(primaryPreferenceDebug), nextSections);
            while (internalNextSections.Count > 0)
            {
                int curInstanceID = internalNextSections.ElementAt(Random.Range(0, internalNextSections.Count));
                targetSectionDebug = instanceIdToSection[curInstanceID];
                yield return RunIntersectionTestsDebug(primary, targetSectionDebug);
                if (intersectionTest)
                {
                    break;
                }
                internalNextSections.Remove(curInstanceID);
                targetSectionDebug = null;
                iterations--;
                if (iterations <= 0)
                {
                    Debug.LogException(new System.StackOverflowException("Intersection test exceeded max iterations"), this);
                }
            }
            if (targetSectionDebug != null)
            {
                break;
            }
            primaryConnectors.RemoveAt(priIndex);
        }

        nativeNexSections.Dispose();

        if (targetSectionDebug == null)
        {
            targetSectionDebug = deadEndPlug;
            secondaryPreferenceDebug = deadEndPlug.connectors[0];
            secondaryPreferenceDebug.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            Debug.LogWarning("Unable to find usable section, ending the tunnel.", primary);
        }
        yield return null;
    }

}

#endif