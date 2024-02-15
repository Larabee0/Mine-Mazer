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
            connector.UpdateWorldPos(float4x4.identity);
            connectors[i] = connector;
        }

        sectionConnectors[id] = connectors;
    }
}

[BurstCompile]
public struct BigMatrixJob : IJobFor
{
    private static readonly quaternion oneEightyOffset = quaternion.Euler(math.radians(0), math.radians(180), math.radians(0));

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
        quaternion offsetPrimaryRot = math.mul(primary.rotation, oneEightyOffset);
        quaternion inverseSecondaryRot = math.inverse(secondary.rotation);
        quaternion newSecondaryRot = math.mul(inverseSecondaryRot, offsetPrimaryRot);

        secondary.UpdateWorldPos(float4x4.TRS(primary.position, newSecondaryRot, new(1)));
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
        primary.UpdateWorldPos(sectionLTW);
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

[BurstCompile]
public struct ConnectorTransform : IJobFor
{
    [ReadOnly, DeallocateOnJobCompletion]
    public NativeArray<BurstConnector> primaryConnectors;
    [ReadOnly, DeallocateOnJobCompletion]
    public NativeArray<BurstConnector> secondaryConnectors;
    [WriteOnly]
    public NativeArray<float4x4> secondaryMatricies;

    public void Execute(int index)
    {
        BurstConnector primary = primaryConnectors[index];
        BurstConnector secondary = secondaryConnectors[index];

        quaternion rotation = math.mul(math.inverse(secondary.rotation), math.mul(primary.rotation, quaternion.Euler(math.radians(0), math.radians(180), math.radians(0))));
        secondary.UpdateWorldPos(float4x4.TRS(primary.position, rotation, new(1)));

        float3 position = primary.position + (primary.position - secondary.position);
        position.y = primary.parentPos.y + (primary.localMatrix.Translation().y - secondary.localMatrix.Translation().y);

        secondaryMatricies[index] = float4x4.TRS(position, rotation, new(1));
    }
}

[BurstCompile]
public struct UpdatePhysicsWorldTransforms : IJobFor
{
    [NativeDisableUnsafePtrRestriction, NativeDisableContainerSafetyRestriction]
    public UnsafeList<TunnelSectionVirtual> VirtualPhysicsWorld;

    public void Execute(int index)
    {
        if (VirtualPhysicsWorld[index].Changed)
        {
            TunnelSectionVirtual tsv = VirtualPhysicsWorld[index];
            
            for (int i = 0; i < tsv.boxes.Length; i++)
            {
                float4x4 matrix = math.mul(tsv.sectionTransform, tsv.boxes[i].boxBounds.LocalMatrix);
                tsv.boxes.ElementAt(i).GetTransformedCorners(matrix);
                tsv.boxes.ElementAt(i).TransformNormals(matrix);
            }
            VirtualPhysicsWorld.ElementAt(index).Changed = false;
            VirtualPhysicsWorld.ElementAt(index).updateCount++;
        }
    }
}

[BurstCompile]
public struct BoxCheckJob : IJobFor
{
    public static readonly float3[] normals = new float3[]
    {
        math.forward(),
        math.up(),
        math.right(),
        math.back(),
        math.down(),
        math.left(),
    };

    [ReadOnly, NativeDisableUnsafePtrRestriction, NativeDisableContainerSafetyRestriction]
    public UnsafeList<TunnelSectionVirtual> VirtualPhysicsWorld;

    [ReadOnly]
    public NativeArray<int2> sectionIds;

    [ReadOnly]
    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public UnsafeParallelHashMap<int, UnsafeList<UnsafeList<BoxTransform>>> incomingMatrices;


    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    public NativeParallelHashMap<int, UnsafeList<InstancedBox>> incomingBoxBounds;

    [WriteOnly]
    public NativeArray<bool> outGoingChecks;

    public void Execute(int index)
    {
        int id = sectionIds[index].x;
        UnsafeList<InstancedBox> sectionBoxes = incomingBoxBounds[id];
        UnsafeList<BoxTransform> sectionBoxTransforms = incomingMatrices[id][sectionIds[index].y];

        int length = sectionBoxes.Length;
        for (int i = 0; i < length; i++)
        {
            float4x4 matrix = sectionBoxTransforms[i].Matrix;
            sectionBoxes.ElementAt(i).GetTransformedCorners(matrix);
            sectionBoxes.ElementAt(i).TransformNormals(matrix);
            if (CheckBox(sectionBoxes[i]))
            {
                outGoingChecks[index] = false;
                return;
            }
        }

        outGoingChecks[index] = true;
    }

    public bool CheckBox(InstancedBox box)
    {
        int length = VirtualPhysicsWorld.Length;
        for (int i = 0; i < length; i++)
        {
            TunnelSectionVirtual sectionInstance = VirtualPhysicsWorld[i];
            if (CheckBox(sectionInstance, box))
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckBox(TunnelSectionVirtual sectionInstance, InstancedBox box)
    {
        UnsafeList<InstancedBox> instancedBoxes = sectionInstance.boxes;
        int length = instancedBoxes.Length;
        for (int i = 0;i< length; i++)
        {
            if(CheckBox(instancedBoxes[i].normals, instancedBoxes[i].corners, box.normals, box.corners))
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckBox(UnsafeList<float3> aNormals, UnsafeList<float3> aCorners, UnsafeList<float3> bNormals, UnsafeList<float3> bCorners)
    {
        int alength = aNormals.Length;
        for (int i = 0; i < alength; i++)
        {
            SATTest(aNormals[i], aCorners, out float shape1Min, out float shape1Max);
            SATTest(aNormals[i], bCorners, out float shape2Min, out float shape2Max);
            if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
            {
                return false;
            }
        }
        int blength = bNormals.Length;
        for (int i = 0; i < blength; i++)
        {
            SATTest(bNormals[i], aCorners, out float shape1Min, out float shape1Max);
            SATTest(bNormals[i], bCorners, out float shape2Min, out float shape2Max);
            if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
            {
                return false;
            }
        }

        return true;
    }

    private void SATTest(float3 axis, UnsafeList<float3> ptSet, out float minAlong, out float maxAlong)
    {
        minAlong = float.MaxValue;
        maxAlong = float.MinValue;
        int length = ptSet.Length;
        for (int i = 0; i < length; i++)
        {
            float dotVal = math.dot(ptSet[i], axis);
            minAlong = (dotVal < minAlong) ? dotVal : minAlong;
            maxAlong = (dotVal > maxAlong) ? dotVal : maxAlong;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Overlaps(float min1, float max1, float min2, float max2)
    {
        return IsBetweenOrdered(min2, min1, max1) || IsBetweenOrdered(min1, min2, max2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsBetweenOrdered(float val, float lowerBound, float upperBound)
    {
        return lowerBound <= val && val <= upperBound;
    }
}

[BurstCompile]
public struct FinalConnectorMulJob : IJobFor
{
    private static readonly quaternion oneEightyOffset = quaternion.Euler(math.radians(0), math.radians(180), math.radians(0));

    [ReadOnly]
    public NativeArray<BurstConnectorPair> connectorPairs;

    [WriteOnly]
    public NativeArray<float4x4> calculatedMatricies;

    [WriteOnly]
    public NativeArray<BurstConnector> primaryConnectors;

    public void Execute(int index)
    {
        BurstConnectorPair pair = connectorPairs[index];
        BurstConnector primary = pair.primary;
        BurstConnector secondary = pair.secondary;
        primary.UpdateWorldPos(pair.primaryMatrix);
        secondary.UpdateWorldPos(float4x4.identity);
        calculatedMatricies[index] = CalculateSectionMatrix(primary, secondary);
        primaryConnectors[index] = primary;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4x4 CalculateSectionMatrix(BurstConnector primary, BurstConnector secondary)
    {
        quaternion offsetPrimaryRot = math.mul(primary.rotation, oneEightyOffset);
        quaternion inverseSecondaryRot = math.inverse(secondary.rotation);
        quaternion newSecondaryRot = math.mul(inverseSecondaryRot, offsetPrimaryRot);

        secondary.UpdateWorldPos(float4x4.TRS(primary.position, newSecondaryRot, new(1)));
        float priLocalY = primary.localMatrix.Translation().y;
        float secLocalY = secondary.localMatrix.Translation().y;
        float3 position = primary.position + (primary.position - secondary.position);
        position.y = primary.parentPos.y + (priLocalY - secLocalY);

        return float4x4.TRS(position, newSecondaryRot, new(1));
    }
}