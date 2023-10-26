using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();

    [SerializeField] private StartEnd lastUsed = StartEnd.Unused;

    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;

    [SerializeField] private BoxBounds[] boundingBoxes;
    [SerializeField] private CapsuleBounds[] boundingCaps;

    public BoxBounds[] BoundingBoxes => boundingBoxes;
    public CapsuleBounds[] BoundingCaps=> boundingCaps;

    public Transform StartPos => startPos;
    public Transform EndPos => endPos;
    public StartEnd LastUsed
    {
        get => lastUsed;
        set => lastUsed = value;
    }

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
        for (int i = 0; i < boundingCaps.Length; i++)
        {
            boundingCaps[i].height = boundingCaps[i].height >= (2 * boundingCaps[i].radius) ? boundingCaps[i].height : 2 * boundingCaps[i].radius;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 angleMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Handles.matrix.lossyScale);
        Gizmos.matrix = angleMatrix;
        Handles.matrix = angleMatrix;
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
