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

        if (pickedSection != deadEndPlug && !rejectBreakableWallAtConnections)
        {
            if (forceBreakableWallAtConnections || Random.value < breakableWallAtConnectionChance)
            {
                BreakableWall breakableInstance = Instantiate(breakableWall, pickedInstance.transform);
                Connector conn = breakableInstance.connector;
                conn.UpdateWorldPos(breakableWall.transform.localToWorldMatrix);
                TransformSection(breakableInstance.transform, priPref, conn);
            }
        }

        Physics.SyncTransforms(); /// push changes to physics world now instead of next fixed update, required for <see cref="RunIntersectionTests(TunnelSection, TunnelSection, out Connector, out Connector)"/>
        return pickedInstance;
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

            handle = parallelMatrixCalculations
                ? bmj.ScheduleParallel(nextSections.Count, 8, handle)
                : bmj.Schedule(nextSections.Count, handle);

            priConn.Dispose(handle).Complete();


            List<int> internalNextSections = FilterSectionsByConnector(primary.GetConnectorMask(primaryPreference), nextSections);
            while (internalNextSections.Count > 0)
            {
                int curInstanceID = internalNextSections.ElementAt(Random.Range(0, internalNextSections.Count));
                targetSection = instanceIdToSection[curInstanceID];
                if (RunIntersectionTests(primary, targetSection, ref primaryPreference, out secondaryPreference))
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
                break;
            }
            primaryConnectors.RemoveAt(priIndex);
        }

        nativeNexSections.Dispose();

        if (targetSection == null)
        {
            secondaryPreference = deadEndPlug.connectors[0];
            secondaryPreference.UpdateWorldPos(deadEndPlug.transform.localToWorldMatrix);
            targetSection = deadEndPlug;
            Debug.LogWarning("Unable to find usable section, ending the tunnel.", primary);
        }
        return targetSection;
    }

}
