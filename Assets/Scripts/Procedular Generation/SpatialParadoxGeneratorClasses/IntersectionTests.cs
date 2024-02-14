using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator 
{

    /// <summary>
    /// Systematically runs through all connector cominbations of the given sections.
    /// Projects the bounding boxes of the new section into the current map to see if it will fit.
    /// Runs until all a valid combination is made or all connectors are tested.
    /// </summary>
    /// <param name="primary"></param>
    /// <param name="target"></param>
    /// <param name="primaryConnector"></param>
    /// <param name="secondaryConnector"></param>
    /// <returns></returns>
    private bool RunIntersectionTests(TunnelSection primary, TunnelSection target, ref Connector primaryConnector, out Connector secondaryConnector)
    {
        secondaryConnector = Connector.Empty;
        List<Connector> secondaryConnectors = FilterConnectors(target);

        while (secondaryConnectors.Count > 0)
        {
            secondaryConnector = GetConnectorFromSection(secondaryConnectors, out int secIndex);

            bool noIntersections = IntersectionTest(target, secondaryConnector.internalIndex);
            //bool newPhysicsTestBurst = ManualCheckBox(target.orignalInstanceId, secondaryConnector.internalIndex);
            //bool newPhysicsTestBurst = ManualCheckBoxBurst(target.orignalInstanceId, secondaryConnector.internalIndex);


            //if (noIntersections != newPhysicsTestBurst)
            //{
            //    Debug.LogFormat("Test Mismatch Burst Test: {0} Old Test: {1}", newPhysicsTestBurst, noIntersections);
            //    showIntersectionTests = true;
            //    Debug.Break();
            //    throw new System.Exception("Break loop");
            //}
            //else
            //{
            //    fromIntersecitonTests.Clear();
            //    Debug.LogFormat("Test Agree");
            //}


            if (noIntersections)
            {
                ConnectorMultiply(primary, ref primaryConnector, ref secondaryConnector);
                return true;
            }
            secondaryConnectors.RemoveAt(secIndex);
        }
        return false;
    }

    /// <summary>
    /// Tests whether hte given target section will fit into the map with computed transform from the given connectors.
    /// </summary>
    /// <param name="primary"></param>
    /// <param name="target"></param>
    /// <param name="primaryConnector"></param>
    /// <param name="secondaryConnectorIndex"></param>
    /// <returns>True if the section will fit, false if not.</returns>
    private bool IntersectionTest(TunnelSection target, int secondaryConnectorIndex)
    {
        //ConnectorMultiply(primary, ref primaryConnector, ref secondaryConnector);

        int instanceID = target.GetInstanceID();
        UnsafeList<UnsafeList<BoxTransform>> transformsContainer = sectionBoxTransforms[instanceID];
        if (transformsContainer.Length == 0)
        {
            Debug.LogError("no transforms found for connector");
            return false;
        }

        if (transformsContainer.Length <= secondaryConnectorIndex)
        {
            Debug.LogErrorFormat(gameObject, "returned transforms list does not contain index ({0}) for connector", secondaryConnectorIndex);
        }

        UnsafeList<BoxTransform> transforms = transformsContainer[secondaryConnectorIndex];
        if (transforms.Length == 0)
        {
            Debug.LogError("no box transforms found");
            return false;
        }
        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];

            BoxTransform m = transforms[i];
            float3 position = m.pos;
            Quaternion rotation = m.rot;
            if (Physics.CheckBox(position, boxBounds.size * 0.5f, rotation, tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
            {

                return false;
            }
        }
        return true;
    }

    private static void ConnectorMultiply(TunnelSection primary, ref Connector primaryConnector, ref Connector secondaryConnector)
    {
        NativeReference<Connector> pri = new(primaryConnector, Allocator.TempJob);
        NativeReference<Connector> sec = new(secondaryConnector, Allocator.TempJob);
        JobHandle handle1 = new ConnectorMulJob { connector = pri, sectionLTW = primary.transform.localToWorldMatrix }.Schedule(new JobHandle());
        JobHandle handle2 = new ConnectorMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryConnector = pri.Value;
        secondaryConnector = sec.Value;
        pri.Dispose();
        sec.Dispose();
    }
}
