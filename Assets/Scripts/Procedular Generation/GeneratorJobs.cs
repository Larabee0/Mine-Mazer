using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct BigMatrixJob : IJobFor
{
    [ReadOnly]
    public NativeReference<Connector> connector;

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
        Connector localPrimary = connector.Value;

        for (int i = 0; i < connectors.Length; i++)
        {
            Connector connector = connectors[i];
            connector.UpdateWorldPos(float4x4.identity);
            float4x4 parentMatrix = CalculateSectionMatrix(localPrimary, connector);
            UnsafeList<BoxTransform> curTransforms = boxTransforms[i];
            for (int j = 0; j < boxes.Length; j++)
            {
                float4x4 childMatrix = math.mul(parentMatrix, boxes[j].Matrix);
                curTransforms[j] = new BoxTransform
                {
                    pos = childMatrix.Translation(),
                    rotation = childMatrix.Rotation()
                };
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
    public float4x4 sectionLTW;
    public NativeReference<Connector> connector;

    public void Execute()
    {
        Connector primary = connector.Value;
        primary.UpdateWorldPos(sectionLTW);
        connector.Value = primary;
    }
}

