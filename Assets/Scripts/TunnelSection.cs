using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TunnelSection : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();

    [SerializeField] private StartEnd lastUsed = StartEnd.Unused;

    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;

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
}
