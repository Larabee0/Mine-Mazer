using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [Header("Baked data")]
    [SerializeField] private Texture2D miniMapAsset;
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();
    public Connector[] connectors;

    public BoxBounds[] boundingBoxes;
    public List<ConnectorMask> excludeConnectorSections = new();
    [SerializeField] private Vector3 strongKeepPosition;
    [SerializeField] private string waypointName;

    public int limit = -1;
    [SerializeField] private List<int> excludePrefabConnectionsIds;
    [Header("Runtime Data")]
    public GameObject stagnationBeacon;
    public int orignalInstanceId;
    private bool weakKeep = false;
    [SerializeField] private bool strongKeep = false;
    public Vector3 WaypointPosition => stagnationBeacon != null ? stagnationBeacon.transform.position : transform.TransformPoint(strongKeepPosition);
    public string WaypointName => stagnationBeacon != null ? stagnationBeacon.name : waypointName;
    public bool StrongKeep => strongKeep;
    public bool Keep
    {
        get
        {
            return weakKeep || strongKeep;
        }
        set
        {
            weakKeep = value;
        }
    }

    
    public bool explored = false;
    // accessors 
    public Texture2D MiniMapAsset => miniMapAsset;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
    private Renderer[] renderers;

    private bool renderersEnabled = true;
    public bool RenderersEnabled 
    {
        get => renderersEnabled;
        set
        {
            if(value != renderersEnabled)
            {
                SetRenderersEnabled(value);
            }
        }
    }
    private Collider[] allColliders;
    private bool collidersEnabled = true;
    public bool CollidersEnabled
    {
        get => collidersEnabled;
        set
        {
            if (value != collidersEnabled)
            {
                SetCollidersEnabled(value);
            }
        }
    }

    public BoxBounds[] BoundingBoxes => boundingBoxes;

    public HashSet<int> InUse = new();
    public Dictionary<int, SectionAndConnector> connectorPairs = new();

    public List<int> ExcludePrefabConnections => excludePrefabConnectionsIds;


    public ConnectorMask GetConnectorMask(Connector connector)
    {
        return excludeConnectorSections[connector.internalIndex];
    }

    public void Build()
    {
        if(excludeConnectorSections.Count != connectors.Length)
        {
            for (int i = 0; i < connectors.Length; i++)
            {
                excludeConnectorSections.Add(new() { excludeRuntime = new() });
            }
        }

        if (excludePrefabConnections == null) return;
        excludePrefabConnectionsIds.Clear();
        excludePrefabConnectionsIds = new List<int>(excludePrefabConnections.Count);
        excludePrefabConnections.ForEach(section=> excludePrefabConnectionsIds.Add(section.GetInstanceID()));
        //excludePrefabConnections.Clear();
        //excludePrefabConnections = null;
    }

    public static float4x4 GetLTWConnectorMatrix(float4x4 ltw, Connector connector)
    {
        return math.mul(ltw, connector.Matrix);
    }

    public void SetRenderersEnabled(bool enabled)
    {
        renderersEnabled = enabled;
        renderers ??= GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = enabled;
        }
    }

    public void SetCollidersEnabled(bool enabled)
    {
        collidersEnabled = enabled;
        allColliders ??= GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++)
        {
            allColliders[i].enabled = enabled;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(transform.TransformPoint(strongKeepPosition), Vector3.one);
        if (connectors != null)
        {
            for (int i = 0; i < connectors.Length; i++)
            {
                Gizmos.matrix = GetLTWConnectorMatrix(transform.localToWorldMatrix, connectors[i]);

                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(Vector3.zero, Vector3.forward);
            }
        }

        Gizmos.color = Color.red;
        if (boundingBoxes != null)
        {
            for (int i = 0; i < boundingBoxes.Length; i++)
            {
                Gizmos.matrix = math.mul(float4x4.TRS(transform.position, transform.rotation, Vector3.one), boundingBoxes[i].Matrix);
                Gizmos.DrawWireCube(Vector3.zero, boundingBoxes[i].size);
            }
        }

    }
}
