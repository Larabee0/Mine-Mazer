using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();

    public Connector[] connectors;

    public BoxBounds[] boundingBoxes;

    public BoxBounds[] BoundingBoxes => boundingBoxes;

    public HashSet<int> InUse = new();
    public Dictionary<int, System.Tuple<TunnelSection, int>> connectorPairs = new();

    public List<TunnelSection> ExcludePrefabConnections => excludePrefabConnections;

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
