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
    public HashSet<int> temporaryInUse = new();
    public Dictionary<int,SectionAndConnector> temporayPairs = new();

    public HashSet<int> InUse
    {
        get
        {
            if(sectionInstance == null)
            {
                return temporaryInUse;
            }
            return sectionInstance.InUse;
        }
        set
        {
            if(sectionInstance == null)
            {
                temporaryInUse = value;
            }
            sectionInstance.InUse = value;
        }
    }

    public Dictionary<int, SectionAndConnector> ConnectorPairs
    {
        get
        {
            if (sectionInstance == null)
            {
                return temporayPairs;
            }
            return sectionInstance.connectorPairs;
        }
        set
        {
            if (sectionInstance == null)
            {
                temporayPairs = value;
            }
            sectionInstance.connectorPairs = value;
        }
    }

    public float4x4 LocalToWorld => sectionInstance != null
                ? (float4x4)sectionInstance.transform.localToWorldMatrix
                : queuedSection.secondaryMatrix;

    public int ConnectorCount => sectionInstance != null
                ? sectionInstance.connectors.Length
                : queuedSection.pickedPrefab.connectors.Length;
    public int FreeConnectors => ConnectorCount - InUse.Count;
    public int OriginalInstanceId => sectionInstance != null ? sectionInstance.orignalInstanceId : queuedSection.pickedPrefab.orignalInstanceId;

    public Connector[] Connectors => sectionInstance != null ? sectionInstance.connectors : queuedSection.pickedPrefab.connectors;

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
            sectionInstance.InUse.UnionWith(temporaryInUse);
            temporaryInUse.Clear();

            foreach (var pair in temporayPairs)
            {
                sectionInstance.connectorPairs.TryAdd(pair.Key,pair.Value);
            }
            temporayPairs.Clear();
            
            SetRenderersEnabled(renderersEnabled);
        }
    }

    public ConnectorMask GetConnectorMask(Connector connector)
    {
        if(sectionInstance == null)
        {
            return queuedSection.pickedPrefab.GetConnectorMask(connector);
        }
        return sectionInstance.GetConnectorMask(connector);
    }

    public bool Equals(MapTreeElement x, MapTreeElement y)
    {
        return x.sectionInstance.Equals(y.sectionInstance); 
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
    public MapTreeElement newTreeElement;
    public BurstConnector primaryConnector;
    public BurstConnector newConnector;
    public MapTreeElement primaryTreeElement;
    public TunnelSection pickedPrefab;
    public int physicsWorldTempId;
    public int priConnInternalIndex;
    public int newConnInternalIndex;

    public float4x4 secondaryMatrix;

    public SectionQueueItem(MapTreeElement primaryInstance, TunnelSection prefabSecondary, Connector primaryConnector, Connector secondaryConnector, int id)
    {
        this.primaryTreeElement = primaryInstance;
        pickedPrefab = prefabSecondary;
        this.primaryConnector = primaryConnector;
        this.newConnector = secondaryConnector;
        physicsWorldTempId = id;
        priConnInternalIndex = primaryConnector.internalIndex;
        newConnInternalIndex = secondaryConnector.internalIndex;

    }

    public void AddToTree(MapTreeElement tree)
    {
        newTreeElement = tree;
        LinkSections();
    }

    public void StartConnection()
    {
        primaryTreeElement.ConnectorPairs[priConnInternalIndex] = new(null, newConnInternalIndex);
        primaryTreeElement.InUse.Add(priConnInternalIndex);
    }

    public void LinkSections()
    {
        primaryTreeElement.ConnectorPairs[priConnInternalIndex] = new(newTreeElement, priConnInternalIndex);
        newTreeElement.ConnectorPairs[priConnInternalIndex] = new(primaryTreeElement, priConnInternalIndex);
        primaryTreeElement.InUse.Add(priConnInternalIndex);
        newTreeElement.InUse.Add(newConnInternalIndex);
    }

    public void FinishConnection(TunnelSection newInstance)
    {
        primaryTreeElement.ConnectorPairs[priConnInternalIndex] = new(newInstance, newConnInternalIndex);
        newInstance.connectorPairs[newConnInternalIndex] = new(primaryTreeElement, priConnInternalIndex);
        newInstance.InUse.Add(newConnInternalIndex);
        newTreeElement.SetInstance(newInstance);
    }


    public BurstConnectorPair GetConnectorPair()
    {
        return new BurstConnectorPair() { id = physicsWorldTempId,primaryMatrix = primaryTreeElement.LocalToWorld, primary = primaryConnector, secondary = newConnector };
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