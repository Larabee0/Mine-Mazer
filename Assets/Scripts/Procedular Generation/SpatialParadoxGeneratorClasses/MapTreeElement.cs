using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MapTreeElementComparer : IEqualityComparer<MapTreeElement>
{
    public bool Equals(MapTreeElement x, MapTreeElement y)
    {
        return x.UID.Equals(y.UID);
    }

    public int GetHashCode(MapTreeElement obj)
    {
        return obj.UID.GetHashCode();
    }
}

public class MapTreeElement
{
    public int UID;
    public bool cancel = false;
    public TunnelSection sectionInstance;
    public SectionQueueItem queuedSection;
    public HashSet<int> inUse = new();
    private Dictionary<int, SectionAndConnector> connectorPairs = new();
    public Dictionary<int, SectionAndConnector> ConnectorPairs {
        get
        {
            if(connectorPairs == null)
            {
                connectorPairs = new Dictionary<int, SectionAndConnector>(ConnectorCount);
                for(int i = 0; i < ConnectorCount; i++)
                {
                    connectorPairs.Add(Connectors[i].internalIndex,null);
                }
            }
            return connectorPairs;
        }
    }

    public float4x4 LocalToWorld => sectionInstance != null
                ? (float4x4)sectionInstance.transform.localToWorldMatrix
                : queuedSection.secondaryMatrix;
    public Vector3 WaypointPosition => sectionInstance != null ? sectionInstance.WaypointPosition : LocalToWorld.TransformPoint(queuedSection.pickedPrefab.StrongKeepPosition);
    
    public string WaypointName => sectionInstance != null ? sectionInstance.WaypointName: queuedSection.pickedPrefab.WaypointName;
    public int ConnectorCount => Connectors.Length;
    public int FreeConnectors => ConnectorCount - inUse.Count;
    public int OriginalInstanceId => sectionInstance != null ? sectionInstance.orignalInstanceId : queuedSection.pickedPrefab.orignalInstanceId;

    public Connector[] Connectors => sectionInstance != null ? sectionInstance.connectors : queuedSection.pickedPrefab.connectors;

    private bool renderersEnabled = true;
    public bool Explored => sectionInstance != null ? sectionInstance.explored : false;

    public bool Instantiated => sectionInstance != null;
    public bool Keep => sectionInstance != null ?  sectionInstance.Keep: queuedSection.pickedPrefab.StrongKeep;

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
            instance.treeElementParent = this;
            sectionInstance = instance;

            SetRenderersEnabled(renderersEnabled);
        }
    }

    public ConnectorMask GetConnectorMask(Connector connector)
    {
        if (sectionInstance == null)
        {
            return queuedSection.pickedPrefab.GetConnectorMask(connector);
        }
        return sectionInstance.GetConnectorMask(connector);
    }

}
