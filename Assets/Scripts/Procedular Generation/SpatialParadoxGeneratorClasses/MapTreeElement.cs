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
    public bool alive = true;
    public bool cancel = false;
    public bool deadEnd = false;
    public TunnelSection sectionInstance;
    public BakedTunnelSection dataFromBake;
    public SectionQueueItem queuedSection;
    public HashSet<int> inUse = new();

    private Dictionary<int, SectionAndConnector> connectorPairs = new();
    public Dictionary<int, SectionAndConnector> ConnectorPairs
    {
        get
        {
            if (connectorPairs == null)
            {
                connectorPairs = new Dictionary<int, SectionAndConnector>(ConnectorCount);
                for (int i = 0; i < ConnectorCount; i++)
                {
                    connectorPairs.Add(Connectors[i].internalIndex, null);
                }
            }
            return connectorPairs;
        }
    }

    public float4x4 LocalToWorld
    {
        get
        {

            if (sectionInstance != null)
            {
                return (float4x4)sectionInstance.transform.localToWorldMatrix;
            }
            else if (queuedSection != null)
            {
                return queuedSection.secondaryMatrix;
            }
            else
            {
                Debug.LogErrorFormat("UID {0} (Dead end? {1}) missing queuedSection item  & section instance for LocalToWorld request. Something fatal has occured.", UID, deadEnd);
                return float4x4.identity;
            }
        }
    }

    public Vector3 WaypointPosition => sectionInstance != null ? sectionInstance.WaypointPosition : LocalToWorld.TransformPoint(dataFromBake.StrongKeepPosition);

    public string GameObjectName => sectionInstance != null ? sectionInstance.gameObject.name : queuedSection.pickedPrefab.gameObject.name;
    public string WaypointName => sectionInstance != null ? sectionInstance.WaypointName: dataFromBake.WaypointName;
    public int ConnectorCount => Connectors.Length;
    public int FreeConnectors => ConnectorCount - inUse.Count;

    public int OriginalInstanceId => sectionInstance != null ? sectionInstance.orignalInstanceId : dataFromBake.OriginalInstanceId;

    public Connector[] Connectors => dataFromBake.connectors;

    private bool renderersEnabled = true;

    public bool Explored => sectionInstance != null ? sectionInstance.explored : false;

    public bool Instantiated => sectionInstance != null;
    public bool Keep => sectionInstance != null ?  sectionInstance.Keep: dataFromBake.StrongKeep;

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
            instance.treeElementOwner = this;
            sectionInstance = instance;

            SetRenderersEnabled(renderersEnabled);
        }
    }

    public ConnectorMask GetConnectorMask(Connector connector)
    {
        return dataFromBake.GetConnectorMask(connector);
    }

}
