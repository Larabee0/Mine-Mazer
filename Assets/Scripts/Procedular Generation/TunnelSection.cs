using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();

    public Connector[] connectors;

    public BoxBounds[] boundingBoxes;
    public CapsuleBounds[] boundingCaps;

    public BoxBounds[] BoundingBoxes => boundingBoxes;
    public CapsuleBounds[] BoundingCaps=> boundingCaps;

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

    private void OnValidate()
    {
        if (boundingCaps != null)
        {
            for (int i = 0; i < boundingCaps.Length; i++)
            {
                boundingCaps[i].height = boundingCaps[i].height >= (2 * boundingCaps[i].radius) ? boundingCaps[i].height : 2 * boundingCaps[i].radius;
            }
        }
    }

    public static Matrix4x4 GetLTWConnectorMatrix(Matrix4x4 ltw, Connector connector)
    {
        return ltw * connector.Matrix;
    }

    private void OnDrawGizmosSelected()
    {
        if (connectors != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < connectors.Length; i++)
            {
                Gizmos.matrix = GetLTWConnectorMatrix(transform.localToWorldMatrix, connectors[i]);
                Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
            }
        }



        Matrix4x4 angleMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Handles.matrix.lossyScale);
        Handles.matrix = angleMatrix;
        Gizmos.matrix = angleMatrix;
        Handles.color = Color.red;
        Gizmos.color = Color.red;
        if(boundingBoxes != null)
        {
            for (int i = 0; i < boundingBoxes.Length; i++)
            {
                angleMatrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.Euler(boundingBoxes[i].oreintation), Handles.matrix.lossyScale);
                Gizmos.matrix = angleMatrix;
                Handles.matrix = angleMatrix;
                Gizmos.DrawWireCube(boundingBoxes[i].center, boundingBoxes[i].size);
            }
        }
        if(boundingCaps != null)
        {
            for (int i = 0; i < boundingCaps.Length; i++)
            {
                angleMatrix = Matrix4x4.TRS(transform.position+ boundingCaps[i].center, transform.rotation * Quaternion.Euler(boundingCaps[i].oreintation), Handles.matrix.lossyScale);
                Gizmos.matrix = angleMatrix;
                Handles.matrix = angleMatrix;
                ExtraUtilities.DrawWireCapsule(boundingCaps[i].radius, boundingCaps[i].height);
            }
        }
    }

    

}
