using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();

    [SerializeField] private StartEnd lastUsed = StartEnd.Unused;

    public Connector startConnector = Connector.Empty;
    public Connector endConnector = Connector.Empty;

    public BoxBounds[] boundingBoxes;
    public CapsuleBounds[] boundingCaps;

    public BoxBounds[] BoundingBoxes => boundingBoxes;
    public CapsuleBounds[] BoundingCaps=> boundingCaps;

    public StartEnd LastUsed
    {
        get => lastUsed;
        set => lastUsed = value;
    }

    public List<TunnelSection> ExcludePrefabConnections => excludePrefabConnections;

    public Vector3 GetConnectorWorldPos(Connector connector, out Quaternion rotation)
    {
        return GetWorldPosFromMatrix(GetLTWConnectorMatrix(connector), out rotation);
    }

    public static Vector3 GetWorldPosFromMatrix(Matrix4x4 matrix,out Quaternion rotation)
    {
        rotation = matrix.rotation;
        return matrix.GetPosition();
    }

    public Matrix4x4 GetLTWConnectorMatrix(Connector connector)
    {
        return GetLTWConnectorMatrix(transform.localToWorldMatrix, connector);
    }
    public static Matrix4x4 GetLTWConnectorMatrix(Matrix4x4 ltw,Connector connector)
    {
        return ltw * connector.Matrix;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = GetLTWConnectorMatrix(transform.localToWorldMatrix, startConnector);
        Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
        Gizmos.matrix = GetLTWConnectorMatrix(transform.localToWorldMatrix, endConnector);
        Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);


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
