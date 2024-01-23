#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator
{

    private IEnumerator RunIntersectionTestsDebug(TunnelSection primary, TunnelSection target)
    {
        secondaryPreferenceDebug = Connector.Empty;
        List<Connector> secondaryConnectors = FilterConnectors(target);

        while (secondaryConnectors.Count > 0)
        {
            secondaryPreferenceDebug = GetConnectorFromSection(secondaryConnectors, out int secIndex);
            yield return DebugIntersectionTestBurst(primary, target);
            if (intersectionTest)
            {
                yield break;
            }
            secondaryConnectors.RemoveAt(secIndex);
        }

        intersectionTest = false;
        yield break;
    }

    private IEnumerator DebugIntersectionTestBurst(TunnelSection primary, TunnelSection target)
    {
        NativeReference<Connector> pri = new(primaryPreferenceDebug, Allocator.TempJob);
        NativeReference<Connector> sec = new(secondaryPreferenceDebug, Allocator.TempJob);
        JobHandle handle1 = new ConnectorMulJob { connector = pri, sectionLTW = primary.transform.localToWorldMatrix }.Schedule(new JobHandle());
        JobHandle handle2 = new ConnectorMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryPreferenceDebug = pri.Value;
        secondaryPreferenceDebug = sec.Value;
        pri.Dispose();
        sec.Dispose();

        List<GameObject> objects = new();
        //objects.Add(Instantiate(target,pos,rot).gameObject);
        int instanceID = target.GetInstanceID();
        UnsafeList<UnsafeList<BoxTransform>> transformsContainer = sectionBoxTransforms[instanceID];
        if (transformsContainer.Length == 0)
        {
            Debug.LogError("no transforms found for connector");
            intersectionTest = false;
            yield break;
        }
        if (transformsContainer.Length <= secondaryPreferenceDebug.internalIndex)
        {
            Debug.LogErrorFormat(gameObject, "returned transforms list does not contain index ({0}) for connector", secondaryPreferenceDebug.internalIndex);
        }

        UnsafeList<BoxTransform> transforms = transformsContainer[secondaryPreferenceDebug.internalIndex];
        if (transforms.Length == 0)
        {
            Debug.LogError("no box transforms found");
            yield break;
        }
        bool noIntersections = true;
        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];
            objects.Add(Instantiate(santiziedCube));

            BoxTransform m = transforms[i];
            float3 position = m.pos;
            Quaternion rotation = m.rot;

            objects[^1].transform.SetPositionAndRotation(position, rotation);
            objects[^1].transform.localScale = boxBounds.size;
            objects[^1].GetComponent<MeshRenderer>().material.color = Color.green;
            if (Physics.CheckBox(position, boxBounds.size * 0.5f, rotation, tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
            {
                objects[^1].GetComponent<MeshRenderer>().material.color = Color.red;
                noIntersections = false;
            }
        }

        //objects[0].SetActive(true);

        yield return new WaitForSeconds(intersectTestHoldTime);
        objects.ForEach(ob => Destroy(ob));

        intersectionTest = noIntersections;
    }

}
#endif