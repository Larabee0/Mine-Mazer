using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator
{
    public class PickIntstinateConnectDelayed
    {
        public MapTreeElement treeEleement;
        public SectionDelayedOuts pickSectionDelayedData;
    }

    public class SectionDelayedOuts
    {
        public Connector primaryPreference;
        public Connector secondaryPreference;
        public TunnelSection pickedSection;
    }

    private IEnumerator PickInstinateConnectDelayed(MapTreeElement primary, PickIntstinateConnectDelayed pickedResult)
    {
        TunnelSection pickedSection;
        MapTreeElement pickedInstance = null;
        Connector priPref;
        Connector secPref;
        if (promoteSectionsList.Count > 0)
        {
            yield return PickFromMothballed(primary, pickedResult);
            if (pickedResult.treeEleement.Instantiated)
            {
                pickedInstance = pickedResult.treeEleement;
                pickedSection = instanceIdToSection[pickedInstance.OriginalInstanceId];
            }
            else
            {
                pickedSection = pickedResult.pickSectionDelayedData.pickedSection;
            }
            priPref = pickedResult.pickSectionDelayedData.primaryPreference;
            secPref = pickedResult.pickSectionDelayedData.secondaryPreference;
        }
        else
        {
            List<int> nextSections = FilterSections(primary.OriginalInstanceId, out bool junction);
            yield return PickSectionDelayed(primary, nextSections, pickedResult.pickSectionDelayedData);
            pickedSection = pickedResult.pickSectionDelayedData.pickedSection;
            priPref = pickedResult.pickSectionDelayedData.primaryPreference;
            secPref = pickedResult.pickSectionDelayedData.secondaryPreference;
            pickedResult.treeEleement = EnqueueSection(primary, pickedResult.pickSectionDelayedData.pickedSection, priPref, secPref);
        }

        if (pickedInstance != null && primary != null)
        {
            TransformSectionAndLink(primary, pickedResult.treeEleement, priPref, secPref);
            InstantiateBreakableWalls(pickedSection, pickedResult.treeEleement.sectionInstance, priPref);
        }

        if(pickedSection == deadEndPlug)
        {
            deadEnds.Add(pickedResult.treeEleement);
            pickedResult.treeEleement.deadEnd = true;
        }
    }

    private IEnumerator PickFromMothballed(MapTreeElement primary, PickIntstinateConnectDelayed pickedResult)
    {
        List<int> internalSections = new(promoteSectionsList.Count);
        promoteSectionsList.ForEach(section => internalSections.Add(section.OriginalInstanceId));

        // pick from mothballed sections
        yield return PickSectionDelayed(primary, internalSections, pickedResult.pickSectionDelayedData);

        var pickedSection = pickedResult.pickSectionDelayedData.pickedSection;

        if (!internalSections.Contains(pickedSection.orignalInstanceId)) // return new section.
        {
            List<int> nextSections = FilterSections(primary.OriginalInstanceId, out bool junction);
            // pick from all valid sections
            yield return PickSectionDelayed(primary, nextSections, pickedResult.pickSectionDelayedData);
            // schedule new section spawn
            pickedResult.treeEleement = EnqueueSection(primary, pickedResult.pickSectionDelayedData.pickedSection,
                pickedResult.pickSectionDelayedData.primaryPreference,
                pickedResult.pickSectionDelayedData.secondaryPreference);
        }
        else // reload section
        {
            // prepare mothballed section for re-enabling.
            int index = internalSections.IndexOf(pickedSection.orignalInstanceId);
            var pickedInstance = promoteSectionsList[index];
            pickedInstance.sectionInstance.gameObject.SetActive(true);
            pickedInstance.sectionInstance.CollidersEnabled = true;
            promoteSectionsList.RemoveAt(index);
            SetSectionInActivePhysicsWorld(pickedInstance.UID, false);
            pickedResult.treeEleement = pickedInstance;
        }
    }

    private void InstantiateBreakableWalls(TunnelSection pickedSection, TunnelSection pickedInstance, Connector priPref)
    {
        if (pickedSection != deadEndPlug && !rejectBreakableWallAtConnections && (forceBreakableWallAtConnections || randomNG.NextFloat() < breakableWallAtConnectionChance))
        {
            BreakableWall breakableInstance = Instantiate(breakableWall, pickedInstance.transform);
            Connector conn = breakableInstance.connector;
           //  priPref.UpdateWorldPos(Unity.Mathematics.float4x4.identity);
            conn.UpdateWorldPos(breakableWall.transform.localToWorldMatrix);
            TransformSection(breakableInstance.transform, priPref, conn);
        }
    }

    private IEnumerator PickSectionDelayed(MapTreeElement primaryElement, List<int> nextSections, SectionDelayedOuts outs, List<Connector> primaryConnectors = null)
    {
        outs.primaryPreference = Connector.Empty;
        outs.secondaryPreference = Connector.Empty;

        primaryConnectors ??= FilterConnectorsByInuse(primaryElement);

        NativeArray<int> nativeNexSections = new(nextSections.ToArray(), Allocator.Persistent);

        int iterations = maxInterations;
        TunnelSection targetSection = null;

        while (targetSection == null && primaryConnectors.Count > 0)
        {
            outs.primaryPreference = GetRandomConnectorFromSection(primaryConnectors, out int priIndex);

            NativeReference<BurstConnector> priConn = new(new(outs.primaryPreference), Allocator.Persistent);
            if (!priConn.IsCreated)
            {
                Debug.LogError("Failed to create priConnector native reference!", gameObject);
                continue;
            }
            JobHandle handle = ScheduleMatrixCalculations(primaryElement, nextSections, nativeNexSections, priConn);

            List<int> internalNextSections = FilterSectionsByConnector(primaryElement.GetConnectorMask(outs.primaryPreference), nextSections);

            ParallelRandInter iteratorData = new()
            {
                handle = handle,
                iterations = iterations,
                primaryPreference = outs.primaryPreference,
                secondaryPreference = outs.secondaryPreference,
                targetSection = targetSection,
            };
            yield return ParallelRandomiseIntersection(primaryElement, primaryConnectors, priIndex, internalNextSections, iteratorData);

            targetSection = iteratorData.targetSection;

            outs.primaryPreference = iteratorData.primaryPreference;
            outs.secondaryPreference = iteratorData.secondaryPreference;
            iterations = iteratorData.iterations;
            if (iteratorData.success)
            {
                break;
            }
        }

        nativeNexSections.Dispose();

        if (targetSection == null)
        {
            Connector priPref = outs.primaryPreference, secPref = outs.secondaryPreference;
            ConnectorMultiply(primaryElement.LocalToWorld, ref priPref, ref secPref);
            outs.primaryPreference = priPref; outs.secondaryPreference = secPref;
            outs.secondaryPreference = deadEndPlug.DataFromBake.connectors[0];
            outs.secondaryPreference.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            targetSection = deadEndPlug;
            Debug.LogWarning("Unable to find usable section, ending the tunnel.");
        }
        outs.pickedSection = targetSection;
    }

    private JobHandle ScheduleMatrixCalculations(MapTreeElement primaryElement, List<int> nextSections, NativeArray<int> nativeNexSections, NativeReference<BurstConnector> priConn)
    {
        JobHandle handle = new BurstConnectorMulJob
        {
            connector = priConn,
            sectionLTW = primaryElement.LocalToWorld
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

        if (!BigMatrix)
        {
            handle = bmj.Schedule(length, handle);
        }
        else
        {
            int batches = Mathf.Max(4, length / SystemInfo.processorCount);
            handle = bmj.ScheduleParallel(length, batches, handle);
        }

        handle = priConn.Dispose(handle);
        return handle;
    }
}
