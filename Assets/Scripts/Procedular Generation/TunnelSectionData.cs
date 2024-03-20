using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TunnelSectionData : MonoBehaviour
{
    public BakedTunnelSection bakedData;
    public TunnelSection targetOrigin;

}

[Serializable]
public class BakedTunnelSection
{
    [SerializeField] private int orignalInstanceId;
    [SerializeField] private TunnelSection originalInstance;
    [SerializeField] private Texture2D miniMapAsset;
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();
    public Connector[] connectors;

    public BoxBounds[] boundingBoxes;
    public List<ConnectorMask> excludeConnectorSections = new();
    [SerializeField] private Vector3 strongKeepPosition;
    [SerializeField] private string waypointName;

    [SerializeField] private SectionSpawnBaseRule spawnRule;
    [SerializeField] private float sectionLightLevel = 0.202f;
    [SerializeField] private AudioClip sectionAmbience;
    [SerializeField] private List<int> excludePrefabConnectionsIds;
    [SerializeField] private bool strongKeep = false;
    [SerializeField] private bool hasLadder = false;
    [SerializeField] private bool isColony = false;
    public float AmbientLightLevel => sectionLightLevel;
    public AudioClip AmbientNoise => sectionAmbience;

    public Texture2D MiniMapAsset => miniMapAsset;
    public Vector3 StrongKeepPosition => strongKeepPosition;
    public string WaypointName => waypointName;
    public bool StrongKeep => strongKeep;
    public bool HasLadder => hasLadder;
    public bool IsColony => isColony;
    public int OriginalInstanceId => orignalInstanceId;
    public TunnelSection OriginalInstance => originalInstance;

    public int InstanceCount
    {
        get => spawnRule.InstancesCount;
        set => spawnRule.InstancesCount = value;
    }

    public BakedTunnelSection(TunnelSection originalInstance)
    {
        this.originalInstance = originalInstance;
        miniMapAsset = originalInstance.MiniMapAsset;

        excludePrefabConnections = new(originalInstance.ExcludePrefabConnectionsTS);

        connectors = new Connector[originalInstance.connectors.Length];
        boundingBoxes = new BoxBounds[originalInstance.boundingBoxes.Length];
        Array.Copy(originalInstance.connectors, connectors, connectors.Length);
        Array.Copy(originalInstance.boundingBoxes, boundingBoxes, boundingBoxes.Length);

        strongKeepPosition = originalInstance.StrongKeepPosition;
        waypointName = originalInstance.WaypointName;
        sectionLightLevel = originalInstance.AmbientLightLevel;
        sectionAmbience = originalInstance.AmbientNoise;
        strongKeep = originalInstance.StrongKeep;
        hasLadder = originalInstance.HasLadder;
        isColony = originalInstance.IsColony;
    }

    public ConnectorMask GetConnectorMask(Connector connector)
    {
        return excludeConnectorSections[connector.internalIndex];
    }


    public void Build(SpatialParadoxGenerator generator)
    {
        this.orignalInstanceId = originalInstance.GetInstanceID();
        if (spawnRule == null)
        {
            if (!originalInstance.TryGetComponent(out spawnRule))
            {
                spawnRule = originalInstance.gameObject.AddComponent<SectionSpawnBaseRule>();
            }
        }
        if (spawnRule != null)
        {
            spawnRule.owner = orignalInstanceId;
            spawnRule.generator = generator;
            InstanceCount = 0;
            spawnRule.ResetRule();
        }
        if (excludeConnectorSections.Count != connectors.Length)
        {
            for (int i = 0; i < connectors.Length; i++)
            {
                excludeConnectorSections.Add(new() { excludeRuntime = new() });
            }
        }

        if (excludePrefabConnections == null) return;
        excludePrefabConnectionsIds = new List<int>(excludePrefabConnections.Count);
        excludePrefabConnections.ForEach(section => excludePrefabConnectionsIds.Add(section.GetInstanceID()));
        excludePrefabConnections.Clear();
        excludePrefabConnections = null;
    }

    public bool UpdateRule()
    {
        return spawnRule.UpdateSpawnStatus();
    }

    public void Spawned()
    {
        if (spawnRule != null) { spawnRule.OnSpawned(); }
    }
}
