using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

public class SectionQueueItem
{
    public MapTreeElement newTreeElement;
    public BurstConnector primaryConnector;
    public BurstConnector newConnector;
    public MapTreeElement primaryTreeElement;
    public TunnelSection pickedPrefab;
    public int physicsWorldId;
    public int priConnInternalIndex;
    public int newConnInternalIndex;

    public float4x4 secondaryMatrix;

    public SectionQueueItem(MapTreeElement primaryInstance, TunnelSection prefabSecondary, Connector primaryConnector, Connector secondaryConnector, int id)
    {
        this.primaryTreeElement = primaryInstance;
        pickedPrefab = prefabSecondary;
        this.primaryConnector = primaryConnector;
        this.newConnector = secondaryConnector;
        physicsWorldId = id;
        priConnInternalIndex = primaryConnector.internalIndex;
        newConnInternalIndex = secondaryConnector.internalIndex;

    }


    public BurstConnectorPair GetConnectorPair()
    {
        return new BurstConnectorPair() { id = physicsWorldId,primaryMatrix = primaryTreeElement.LocalToWorld, primary = primaryConnector, secondary = newConnector };
    }
}

public struct TunnelSectionVirtual : INativeDisposable, IComparable<TunnelSectionVirtual>, IEqualityComparer<TunnelSectionVirtual>, IEquatable<TunnelSectionVirtual>
{
    public UnsafeList<InstancedBox> boxes;
    public float4x4 sectionTransform;
    public bool Changed;
    public int boundSection;
    public int updateCount;
    public bool temporary;


    public int CompareTo(TunnelSectionVirtual other)
    {
        return boundSection.CompareTo(other.boundSection);
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        JobHandle handle = new();
        for (int i = 0; i < boxes.Length; i++)
        {
            JobHandle.CombineDependencies(handle, boxes[i].Dispose(inputDeps));
        }

        return boxes.Dispose(handle);
    }

    public void Dispose()
    {
        for (int i = 0; i < boxes.Length; i++)
        {
            boxes[i].Dispose();
        }
        boxes.Dispose();
    }

    public bool Equals(TunnelSectionVirtual x, TunnelSectionVirtual y)
    {
        return x.boundSection.Equals(y.boundSection);
    }

    public bool Equals(TunnelSectionVirtual other)
    {
        return boundSection.Equals(other.boundSection);
    }

    public int GetHashCode(TunnelSectionVirtual obj)
    {
        return (int)math.hash(new float3(boundSection, boundSection, boundSection));
    }
}

public struct InstancedBox : INativeDisposable
{
    public BoxBounds boxBounds;
    public float4x4 matrix;
    public UnsafeList<float3> normals;
    public UnsafeList<float3> corners;

    public InstancedBox (BoxBounds bounds)
    {
        this.boxBounds = bounds;
        matrix = float4x4.identity;
        normals = new UnsafeList<float3>(6, Allocator.Persistent);
        normals.Resize(6);
        corners = new UnsafeList<float3>(8, Allocator.Persistent);
        corners.Resize(8);
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return JobHandle.CombineDependencies(normals.Dispose(inputDeps), corners.Dispose(inputDeps));
    }

    public void Dispose()
    {
        normals.Dispose();
        corners.Dispose();
    }
}