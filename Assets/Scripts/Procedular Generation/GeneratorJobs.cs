using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.VirtualTexturing;

public static class SpatialParadoxBurst
{

    public static bool IntersectionTestBurst(TunnelSection primary, TunnelSection target, ref Connector primaryConnector, ref Connector secondaryConnector, int tunnelSectionLayerIndex)
    {
        NativeArray<BoxBounds> nativeBoundstarget = new(target.BoundingBoxes, Allocator.TempJob);
        NativeReference<float4x4> mainMatrix = new(float4x4.identity, Allocator.TempJob);
        NativeArray<float4x2> boxTransforms = new(nativeBoundstarget.Length, Allocator.TempJob);
        NativeArray<Connector> connectors = new(2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        connectors[0] = primaryConnector;
        connectors[1] = secondaryConnector;
        JobHandle handle = new MatrixMulJob
        {
            priMatrix = (float4x4)primary.transform.localToWorldMatrix,
            secMatrix = (float4x4)target.transform.localToWorldMatrix,
            parentMatrix = mainMatrix,
            connectors = connectors
        }.Schedule(new JobHandle());

        handle = new MatrixJob
        {
            boxes = nativeBoundstarget,
            parentMatrix = mainMatrix,
            boxTransform = boxTransforms
        }.Schedule(boxTransforms.Length, handle);
        mainMatrix.Dispose(handle).Complete();

        primaryConnector = connectors[0];
        secondaryConnector = connectors[1];
        connectors.Dispose();

        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];
            float4x2 m = boxTransforms[i];
            float3 position = new(m.c0.x, m.c0.y, m.c0.z);
            Quaternion rotation = new(m.c1.x, m.c1.y, m.c1.z, m.c1.w);
            if (Physics.CheckBox(position, boxBounds.size * 0.5f, rotation, tunnelSectionLayerIndex, QueryTriggerInteraction.Ignore))
            {
                boxTransforms.Dispose();
                return false;
            }
        }

        boxTransforms.Dispose();

        return true;
    }
}

[BurstCompile]
public struct BigMatrixJob : IJobFor
{
    public Connector primaryConnector;

    [ReadOnly]
    public NativeArray<int> sectionIds;
    
    [ReadOnly]
    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public NativeParallelHashMap<int, UnsafeList<Connector>> sectionConnectors;
    
    [ReadOnly]
    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public NativeParallelHashMap<int, UnsafeList<BoxBounds>> boxBounds;
    
    [NativeDisableUnsafePtrRestriction] 
    [NativeDisableContainerSafetyRestriction]
    public UnsafeParallelHashMap<int, UnsafeList<UnsafeList<BoxTransform>>> matrices;


    public void Execute(int index)
    {
        int id = sectionIds[index];

        UnsafeList<Connector> connectors = sectionConnectors[id];
        UnsafeList<BoxBounds> boxes = boxBounds[id];
        UnsafeList<UnsafeList<BoxTransform>> boxTransforms = matrices[id];
        Connector localPrimary = primaryConnector;

        for (int i = 0; i < connectors.Length; i++)
        {
            Connector connector = connectors[i];
            connector.UpdateWorldPos(float4x4.identity);
            float4x4 parentMatrix = CalculateSectionMatrix(localPrimary, connector);
            UnsafeList<BoxTransform> curTransforms = boxTransforms[i];
            for (int j = 0; j < boxes.Length; j++)
            {
                float4x4 childMatrix = math.mul(parentMatrix, boxes[j].Matrix);
                curTransforms.AddNoResize(new BoxTransform
                {
                    pos = childMatrix.Translation(),
                    rotation = childMatrix.Rotation()
                });
            }
            boxTransforms[i] = curTransforms;
        }
        matrices[id] = boxTransforms;
    }

    private static float4x4 CalculateSectionMatrix(Connector primary, Connector secondary)
    {
        quaternion rotation = math.mul(math.inverse(secondary.rotation), math.mul(primary.rotation, quaternion.Euler(math.radians(0), math.radians(180), math.radians(0))));
        secondary.UpdateWorldPos(float4x4.TRS(primary.position, rotation, Vector3.one));

        float3 position = primary.position + (primary.position - secondary.position);
        position.y = primary.parentPos.y + (primary.localPosition.y - secondary.localPosition.y);

        return float4x4.TRS(position, rotation, Vector3.one);
    }
}

[BurstCompile]
public struct MatrixMulJob : IJob
{
    public NativeArray<Connector> connectors;
    public float4x4 priMatrix;
    public float4x4 secMatrix;
    [WriteOnly]
    public NativeReference<float4x4> parentMatrix;

    public void Execute()
    {
        Connector primary = connectors[0];
        Connector secondary = connectors[1];
        primary.UpdateWorldPos(priMatrix);
        secondary.UpdateWorldPos(secMatrix);
        connectors[0] = primary;
        connectors[1] = secondary;

        quaternion rotation = math.mul(math.inverse(secondary.rotation), math.mul(primary.rotation, quaternion.Euler(math.radians(0), math.radians(180), math.radians(0))));

        secondary.UpdateWorldPos(float4x4.TRS(primary.position, rotation, new (1)));

        float3 position = primary.position + (primary.position - secondary.position);
        position.y = primary.parentPos.y + (primary.localPosition.y - secondary.localPosition.y);
        parentMatrix.Value = float4x4.TRS(position, rotation, new(1));
    }
}

[BurstCompile]
public struct MatrixJob : IJobFor
{
    [ReadOnly]
    public NativeReference<float4x4> parentMatrix;

    [ReadOnly,DeallocateOnJobCompletion]
    public NativeArray<BoxBounds> boxes;
    [WriteOnly]
    public NativeArray<float4x2> boxTransform;


    public void Execute(int index)
    {
        float4x4 m = math.mul(parentMatrix.Value, boxes[index].Matrix);
        boxTransform[index] = new()
        {
            c0 = new(m.Translation(),0),
            c1 = m.Rotation().value
        };
    }
}
