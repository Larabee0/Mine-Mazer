using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class SpatialParadoxGenerator
{
    /// <summary>
    /// Based off the givne primary section, this picks a new section prefab, Instinates it and connects (transforms) it to the primay section
    /// </summary>
    /// <param name="primary">Given root node of this connection</param>
    /// <returns>New Section Instance</returns>
    private TunnelSection PickInstinateConnect(TunnelSection primary)
    {
        TunnelSection pickedSection = null;
        TunnelSection pickedInstance = null;
        Connector priPref = Connector.Empty, secPref = Connector.Empty;
        if (promoteSectionsList.Count > 0)
        {
            List<int> internalSections = new(promoteSectionsList.Count);
            promoteSectionsList.ForEach(section => internalSections.Add(section.orignalInstanceId));
            pickedSection = PickSection(primary, internalSections, out priPref, out secPref);
            if (!internalSections.Contains(pickedSection.GetInstanceID())) // returned dead end.
            {
                List<int> nextSections = FilterSections(primary);
                pickedSection = PickSection(primary, nextSections, out priPref, out secPref);
                pickedInstance = InstinateSection(pickedSection);
            }
            else // reinstance section
            {
                int index = internalSections.IndexOf(pickedSection.GetInstanceID());
                pickedInstance = promoteSectionsList[index];
                pickedInstance.gameObject.SetActive(true);
                pickedInstance.CollidersEnabled = true;
                promoteSectionsList.RemoveAt(index);
            }
        }
        else
        {
            List<int> nextSections = FilterSections(primary);
            pickedSection = PickSection(primary, nextSections, out priPref, out secPref);
            pickedInstance = InstinateSection(pickedSection);
        }

        TransformSection(primary, pickedInstance, priPref, secPref); // transform the new section

        InstantiateBreakableWalls(pickedSection, pickedInstance, priPref);

        Physics.SyncTransforms(); /// push changes to physics world now instead of next fixed update, required for <see cref="RunIntersectionTests(TunnelSection, TunnelSection, out Connector, out Connector)"/>
        return pickedInstance;
    }

    public class PickIntstinateConnectDelayed
    {
        public MapTreeElement treeEleement;
        public SectionDelayedOuts pickSectionDelayedData;
    }

    private IEnumerator PickInstinateConnectDelayed(TunnelSection primary, PickIntstinateConnectDelayed pickedResult)
    {
        TunnelSection pickedSection = null;
        TunnelSection pickedInstance = null;
        Connector priPref = Connector.Empty, secPref = Connector.Empty;
        if (promoteSectionsList.Count > 0)
        {
            List<int> internalSections = new(promoteSectionsList.Count);
            promoteSectionsList.ForEach(section => internalSections.Add(section.orignalInstanceId));
            yield return PickSectionDelayed(primary, internalSections, pickedResult.pickSectionDelayedData);
            pickedSection = pickedResult.pickSectionDelayedData.pickedSection;
            if (!internalSections.Contains(pickedSection.GetInstanceID())) // returned dead end.
            {
                List<int> nextSections = FilterSections(primary);
                yield return PickSectionDelayed(primary, nextSections, pickedResult.pickSectionDelayedData);

                pickedResult.treeEleement = EnqueueSection(primary, pickedResult.pickSectionDelayedData.pickedSection, priPref, secPref);

                //pickedInstance = InstinateSection(pickedSection);
            }
            else // reinstance section
            {
                int index = internalSections.IndexOf(pickedSection.GetInstanceID());
                pickedInstance = promoteSectionsList[index];
                pickedInstance.gameObject.SetActive(true);
                pickedInstance.CollidersEnabled = true;
                promoteSectionsList.RemoveAt(index);
            }
        }
        else
        {
            List<int> nextSections = FilterSections(primary);
            yield return PickSectionDelayed(primary, nextSections, pickedResult.pickSectionDelayedData);
            pickedSection = pickedResult.pickSectionDelayedData.pickedSection;
            pickedResult.treeEleement = EnqueueSection(primary, pickedResult.pickSectionDelayedData.pickedSection, priPref, secPref);

            // pickedInstance = InstinateSection(pickedSection);
        }
        if (pickedInstance != null)
        {
            TransformSection(primary, pickedResult.pickSectionDelayedData.pickedSection, priPref, secPref); // transform the new section

            InstantiateBreakableWalls(pickedSection, pickedResult.pickSectionDelayedData.pickedSection, priPref);
        }
        //pickedResult.outs.result = pickedResult.outs.result;
        
    }

    private void InstantiateBreakableWalls(TunnelSection pickedSection, TunnelSection pickedInstance, Connector priPref)
    {
        if (pickedSection != deadEndPlug && !rejectBreakableWallAtConnections && (forceBreakableWallAtConnections || Random.value < breakableWallAtConnectionChance))
        {
            BreakableWall breakableInstance = Instantiate(breakableWall, pickedInstance.transform);
            Connector conn = breakableInstance.connector;
           //  priPref.UpdateWorldPos(Unity.Mathematics.float4x4.identity);
            conn.UpdateWorldPos(breakableWall.transform.localToWorldMatrix);
            TransformSection(breakableInstance.transform, priPref, conn);
        }
    }

    /// <summary>
    /// Based on a given root, this method figures out what prefabs can connect to it and then chooses, semi-randomly, a prefab from that list that fits in the world.
    /// In the event no section will fit in the world, a dead end is placed instead.
    /// </summary>
    /// <param name="primary">Root Section</param>
    /// <param name="primaryPreference">Root section connector target</param>
    /// <param name="secondaryPreference">New section connector target</param>
    /// <returns>Chosen Prefab</returns>
    private TunnelSection PickSection(TunnelSection primary, List<int> nextSections, out Connector primaryPreference, out Connector secondaryPreference)
    {
        primaryPreference = Connector.Empty;
        secondaryPreference = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);

        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.TempJob);

        int iterations = maxInterations;
        TunnelSection targetSection = null;

        Physics.SyncTransforms();
        while (targetSection == null && primaryConnectors.Count > 0)
        {
            primaryPreference = GetConnectorFromSection(primaryConnectors, out int priIndex);

            NativeReference<BurstConnector> priConn = new(new(primaryPreference), Allocator.TempJob);
            if (!priConn.IsCreated)
            {
                Debug.LogError("Failed to create priConnector native reference!", gameObject);
                continue;
            }
            JobHandle handle = new BurstConnectorMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());

            var bmj = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectorContainers,
                boxBounds = sectionBoxMatrices,
                matrices = sectionBoxTransforms
            };
            int length = nextSections.Count;
            int batches = Mathf.Max(4, length / SystemInfo.processorCount);
            handle = parallelMatrixCalculations
                ? bmj.ScheduleParallel(length, batches, handle)
                : bmj.Schedule(length, handle);

            handle = priConn.Dispose(handle);
            

            // Debug.Log(length);

            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(primaryPreference), nextSections);
            if (BigParallelIntersectTests)
            {
                //handle.Complete();
                if (ParallelRandomiseIntersection(primary,
                    ref primaryPreference,
                    ref secondaryPreference,
                    primaryConnectors,
                    ref iterations,
                    ref targetSection,
                    priIndex,
                    internalNextSections,
                    handle
                ))
                {
                    break;
                }
            }
            else
            {
                handle.Complete();
                if (RandomiseIntersection(primary, ref primaryPreference, ref secondaryPreference, primaryConnectors, ref iterations, ref targetSection, priIndex, internalNextSections))
                {
                    break;
                }
            }
        }

        nativeNexSections.Dispose();

        if (targetSection == null)
        {
            ConnectorMultiply(primary, ref primaryPreference, ref secondaryPreference);
            secondaryPreference = deadEndPlug.connectors[0];
            secondaryPreference.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            targetSection = deadEndPlug;
            Debug.LogWarning("Unable to find usable section, ending the tunnel.", primary);
        }
        return targetSection;
    }

    public class SectionDelayedOuts
    {
        public Connector primaryPreference;
        public Connector secondaryPreference;
        public TunnelSection pickedSection;
    }

    private IEnumerator PickSectionDelayed(TunnelSection primary, List<int> nextSections,
        SectionDelayedOuts outs)
    {
        outs.primaryPreference = Connector.Empty;
        outs.secondaryPreference = Connector.Empty;

        List<Connector> primaryConnectors = FilterConnectors(primary);

        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.Persistent);

        int iterations = maxInterations;
        TunnelSection targetSection = null;

        Physics.SyncTransforms();
        while (targetSection == null && primaryConnectors.Count > 0)
        {
            outs.primaryPreference = GetConnectorFromSection(primaryConnectors, out int priIndex);

            NativeReference<BurstConnector> priConn = new(new(outs.primaryPreference), Allocator.Persistent);
            if (!priConn.IsCreated)
            {
                Debug.LogError("Failed to create priConnector native reference!", gameObject);
                continue;
            }
            JobHandle handle = new BurstConnectorMulJob
            {
                connector = priConn,
                sectionLTW = primary.transform.localToWorldMatrix
            }.Schedule(new JobHandle());

            var bmj = new BigMatrixJob
            {
                connector = priConn,
                sectionIds = nativeNexSections,
                sectionConnectors = sectionConnectorContainers,
                boxBounds = sectionBoxMatrices,
                matrices = sectionBoxTransforms
            };
            int length = nextSections.Count;
            int batches = Mathf.Max(4, length / SystemInfo.processorCount);
            handle = parallelMatrixCalculations
                ? bmj.ScheduleParallel(length, batches, handle)
                : bmj.Schedule(length, handle);

            handle = priConn.Dispose(handle);

            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(outs.primaryPreference), nextSections);
            if (BigParallelIntersectTests)
            {
                ParallelRandInter iteratorData = new()
                {
                    handle = handle,
                    iterations = iterations,
                    primaryPreference = outs.primaryPreference,
                    secondaryPreference = outs.secondaryPreference,
                    targetSection = targetSection,
                };
                yield return ParallelRandomiseIntersection(primary, primaryConnectors, priIndex, internalNextSections, iteratorData);
                //handle.Complete();
                targetSection = iteratorData.targetSection;
                if (iteratorData.success)
                {
                    break;
                }
                //if (ParallelRandomiseIntersection(primary,
                //    ref primaryPreference,
                //    ref secondaryPreference,
                //    primaryConnectors,
                //    ref iterations,
                //    ref targetSection,
                //    priIndex,
                //    internalNextSections,
                //    handle
                //))
                //{
                //    break;
                //}
            }
            else
            {
                handle.Complete();
                if (RandomiseIntersection(primary, ref outs.primaryPreference, ref outs.secondaryPreference, primaryConnectors, ref iterations, ref targetSection, priIndex, internalNextSections))
                {
                    break;
                }
            }
        }

        nativeNexSections.Dispose();

        if (targetSection == null)
        {
            ConnectorMultiply(primary, ref outs.primaryPreference, ref outs.secondaryPreference);
            outs.secondaryPreference = deadEndPlug.connectors[0];
            outs.secondaryPreference.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            targetSection = deadEndPlug;
            Debug.LogWarning("Unable to find usable section, ending the tunnel.", primary);
        }
        outs.pickedSection = targetSection;
    }

    private bool RandomiseIntersection(TunnelSection primary, ref Connector primaryPreference, ref Connector secondaryPreference, List<Connector> primaryConnectors, ref int iterations, ref TunnelSection targetSection, int priIndex, List<int> internalNextSections)
    {
        while (internalNextSections.Count > 0)
        {
            int curInstanceID = internalNextSections.ElementAt(Random.Range(0, internalNextSections.Count));
            targetSection = instanceIdToSection[curInstanceID];

            bool intersectionTest = parallelIntersectTests
                ? ParallelIntersectTest(primary, targetSection, ref primaryPreference, out secondaryPreference)
                : RunIntersectionTests(primary, targetSection, ref primaryPreference, out secondaryPreference);

            if (intersectionTest)
            {
                break;
            }
            internalNextSections.Remove(curInstanceID);
            targetSection = null;
            iterations--;
            if (iterations <= 0)
            {
                Debug.LogException(new System.StackOverflowException("Intersection test exceeded max iterations"), this);
            }
        }
        if (targetSection != null)
        {
            return true;
        }
        primaryConnectors.RemoveAt(priIndex);
        return false;
    }
}
