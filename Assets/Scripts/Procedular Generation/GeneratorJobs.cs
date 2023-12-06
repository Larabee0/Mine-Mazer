using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;

[BurstCompile]
public struct SetupConnectorsJob : IJobFor
{
    [ReadOnly, DeallocateOnJobCompletion]
    public NativeArray<int> sectionIds;

    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public NativeParallelHashMap<int, UnsafeList<BurstConnector>> sectionConnectors;

    public void Execute(int index)
    {
        int id = sectionIds[index];

        UnsafeList<BurstConnector> connectors = sectionConnectors[id];

        for (int i = 0; i < connectors.Length; i++)
        {
            BurstConnector connector = connectors[i];
            BurstConnector.UpdateWorldPos(ref connector, float4x4.identity);
            connectors[i] = connector;
        }

        sectionConnectors[id] = connectors;
    }
}

[BurstCompile]
public struct BigMatrixJob : IJobFor
{
    [ReadOnly]
    public NativeReference<BurstConnector> connector;

    [ReadOnly]
    public NativeArray<int> sectionIds;
    
    [ReadOnly]
    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public NativeParallelHashMap<int, UnsafeList<BurstConnector>> sectionConnectors;
    
    [ReadOnly]
    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public NativeParallelHashMap<int, UnsafeList<float4x4>> boxBounds;
    
    [NativeDisableUnsafePtrRestriction] 
    [NativeDisableContainerSafetyRestriction]
    public UnsafeParallelHashMap<int, UnsafeList<UnsafeList<BoxTransform>>> matrices;


    public void Execute(int index)
    {
        int id = sectionIds[index];

        UnsafeList<BurstConnector> connectors = sectionConnectors[id];
        UnsafeList<float4x4> boxes = boxBounds[id];
        UnsafeList<UnsafeList<BoxTransform>> boxTransforms = matrices[id];
        BurstConnector localPrimary = connector.Value;

        for (int i = 0; i < connectors.Length; i++)
        {
            BurstConnector connector = connectors[i];
            //connector.UpdateWorldPos(float4x4.identity);
            float4x4 parentMatrix = CalculateSectionMatrix(localPrimary, connector);
            UnsafeList<BoxTransform> curTransforms = boxTransforms[i];
            for (int j = 0; j < boxes.Length; j++)
            {
                float4x4 childMatrix = math.mul(parentMatrix, boxes[j]);
                curTransforms[j] = new BoxTransform
                {
                    pos = childMatrix.Translation(),
                    rot = childMatrix.Rotation()
                };
            }
            boxTransforms[i] = curTransforms;
        }
        matrices[id] = boxTransforms;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4x4 CalculateSectionMatrix(BurstConnector primary, BurstConnector secondary)
    {
        quaternion oneEightyOffset = quaternion.Euler(math.radians(0), math.radians(180), math.radians(0));
        quaternion offsetPrimaryRot = math.mul(primary.rotation, oneEightyOffset);
        quaternion inverseSecondaryRot = math.inverse(secondary.rotation);
        quaternion newSecondaryRot = math.mul(inverseSecondaryRot, offsetPrimaryRot);

        BurstConnector.UpdateWorldPos(ref secondary, float4x4.TRS(primary.position, newSecondaryRot, new(1)));
        float priLocalY = primary.localMatrix.Translation().y;
        float secLocalY = secondary.localMatrix.Translation().y;
        float3 position = primary.position + (primary.position - secondary.position);
        position.y = primary.parentPos.y + (priLocalY - secLocalY);

        return float4x4.TRS(position, newSecondaryRot, new(1));
    }
}

[BurstCompile]
public struct BurstConnectorMulJob : IJob
{
    public float4x4 sectionLTW;
    public NativeReference<BurstConnector> connector;

    public void Execute()
    {
        BurstConnector primary = connector.Value;
        BurstConnector.UpdateWorldPos(ref primary,sectionLTW);
        connector.Value = primary;
    }
}


[BurstCompile]
public struct ConnectorMulJob : IJob
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