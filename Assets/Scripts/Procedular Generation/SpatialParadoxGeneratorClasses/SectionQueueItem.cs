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

public class MapTreeElement : IEqualityComparer<MapTreeElement>
{
    public TunnelSection sectionInstance;
    public SectionQueueItem queuedSection;
    
    private bool renderersEnabled = true;

    public bool Instantiated => sectionInstance != null;


    public static implicit operator MapTreeElement(TunnelSection section) => new() { sectionInstance = section, renderersEnabled = section.RenderersEnabled };
    public static implicit operator MapTreeElement(SectionQueueItem section)
    {
        MapTreeElement element = new() { queuedSection = section };
        section.AddToTree(element);
        return element;
    }

    public void SetRenderersEnabled(bool enabled)
    {
        renderersEnabled = enabled;
        if (sectionInstance != null)
        {
            sectionInstance.SetRenderersEnabled(enabled);
        }
    }

    public void SetInstance(TunnelSection instance)
    {
        if (queuedSection != null)
        {
            queuedSection = null;
            sectionInstance = instance;
            SetRenderersEnabled(renderersEnabled);
        }
    }

    public bool Equals(MapTreeElement x, MapTreeElement y)
    {
        return x.sectionInstance.Equals(y.sectionInstance); 
        return (x.sectionInstance == y.sectionInstance && (x.queuedSection == null || y.queuedSection == null))
            || ((x.queuedSection != null || y.queuedSection != null) && x.queuedSection == y.queuedSection);
    }

    public int GetHashCode(MapTreeElement obj)
    {
        if(obj.sectionInstance != null) return obj.sectionInstance.GetHashCode();
        if(obj.queuedSection != null) return obj.queuedSection.GetHashCode();
        return GetHashCode();
    }
}


public class SectionQueueItem
{
    public MapTreeElement treeElement;
    public BurstConnector primaryConnector;
    public BurstConnector secondaryConnector;
    public TunnelSection primaryInstance;
    public TunnelSection secondaryPickedPrefab;
    public int physicsWorldTempId;
    public int priConnInternalIndex;
    public int secConnInternalIndex;

    public float4x4 secondaryMatrix;
    public bool MatrixCalculated = false;

    public SectionQueueItem(TunnelSection primaryInstance, TunnelSection prefabSecondary, Connector primaryConnector, Connector secondaryConnector, int id)
    {
        this.primaryInstance = primaryInstance;
        secondaryPickedPrefab = prefabSecondary;
        this.primaryConnector = primaryConnector;
        this.secondaryConnector = secondaryConnector;
        physicsWorldTempId = id;
        priConnInternalIndex = primaryConnector.internalIndex;
        secConnInternalIndex = secondaryConnector.internalIndex;

        StartConnection();
    }

    public void AddToTree(MapTreeElement tree)
    {
        treeElement = tree;
    }

    public void StartConnection()
    {
        primaryInstance.connectorPairs[priConnInternalIndex] = new(null, secConnInternalIndex);
        primaryInstance.InUse.Add(priConnInternalIndex);
    }

    public void FinishConnection(TunnelSection secondaryInstance)
    {
        primaryInstance.connectorPairs[priConnInternalIndex] = new(secondaryInstance, secConnInternalIndex);
        secondaryInstance.connectorPairs[secConnInternalIndex] = new(primaryInstance, priConnInternalIndex);
        secondaryInstance.InUse.Add(secConnInternalIndex);
        treeElement.SetInstance (secondaryInstance);
    }


    public BurstConnectorPair GetConnectorPair()
    {
        return new BurstConnectorPair() { id = physicsWorldTempId,primaryMatrix = primaryInstance.transform.localToWorldMatrix, primary = primaryConnector, secondary = secondaryConnector };
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