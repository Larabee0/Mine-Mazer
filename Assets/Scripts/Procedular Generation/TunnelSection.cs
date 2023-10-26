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
    [SerializeField] private CapsuleBounds[] capsuleBounds;

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

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 angleMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Handles.matrix.lossyScale);
        Gizmos.matrix = angleMatrix;
        Handles.matrix = angleMatrix;
        Handles.color = Color.red;
        Gizmos.color = Color.red;
        for (int i = 0; i < boundingBoxes.Length; i++)
        {
            Gizmos.DrawWireCube(boundingBoxes[i].center, boundingBoxes[i].size);
        }
        for (int i = 0; i < capsuleBounds.Length; i++)
        {
            ExtraUtilities.DrawWireCapsule(capsuleBounds[i].center,capsuleBounds[i].radius, capsuleBounds[i].hieght);
        }
    }

    

}
