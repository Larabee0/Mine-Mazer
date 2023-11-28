using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();
    
    public GameObject stagnationBeacon;

    public Vector3 Position => transform.position;
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

    public Connector[] connectors;

    public BoxBounds[] boundingBoxes;

    public BoxBounds[] BoundingBoxes => boundingBoxes;

    public HashSet<int> InUse = new();
    public Dictionary<int, SectionAndConnector> connectorPairs = new();

    public List<ConnectorMask> excludeConnectorSections = new();

    [SerializeField] private List<int> excludePrefabConnectionsIds;
    public List<int> ExcludePrefabConnections => excludePrefabConnectionsIds;

    public int orignalInstanceId;

    public bool keep;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponentInParent<SpatialParadoxGenerator>().PlayerEnterSection(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponentInParent<SpatialParadoxGenerator>().PlayerExitSection(this);
        }
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
