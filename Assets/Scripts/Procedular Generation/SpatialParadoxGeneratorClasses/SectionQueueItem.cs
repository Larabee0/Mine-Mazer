using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class SectionQueueItem
{
    public BurstConnector primaryConnector;
    public BurstConnector secondaryConnector;
    public float4x4 secondaryMatrix = float4x4.zero;
    public GameObject primaryInstance;
    public GameObject secondaryPickedPrefab;

    public bool MatrixCalculated => secondaryMatrix.Equals(float4x4.zero);
}


public struct TunnelSectionVirtual : INativeDisposable, IComparable<TunnelSectionVirtual>, IEqualityComparer<TunnelSectionVirtual>, IEquatable<TunnelSectionVirtual>
{
    public UnsafeList<InstancedBox> boxes;
    public float4x4 sectionTransform;
    public bool Changed;
    public int boundSection;

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