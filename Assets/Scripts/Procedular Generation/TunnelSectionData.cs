using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class TunnelSectionData : MonoBehaviour
{
    public BakedTunnelSection bakedData;
    public TunnelSection targetOrigin;
    [SerializeField] private bool showProceduralPoints;
    [SerializeField] private float orientationRayLength = 0.2f;
    private void Awake()
    {
        targetOrigin = GetComponent<TunnelSection>();
    }


    private void OnDrawGizmosSelected()
    {
        if (bakedData != null)
        {
            if (bakedData.StrongKeep)
            {
                Gizmos.DrawCube(transform.TransformPoint(bakedData.StrongKeepPosition), Vector3.one);
            }
            if (bakedData.connectors != null)
            {
                for (int i = 0; i < bakedData.connectors.Length; i++)
                {
                    Gizmos.matrix = TunnelSection.GetLTWConnectorMatrix(transform.localToWorldMatrix, bakedData.connectors[i]);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(Vector3.zero, Vector3.forward);
                }
            }

            Gizmos.color = Color.red;
            if (bakedData.boundingBoxes != null)
            {
                for (int i = 0; i < bakedData.boundingBoxes.Length; i++)
                {
                    Gizmos.matrix = math.mul(float4x4.TRS(transform.position, transform.rotation, Vector3.one), bakedData.boundingBoxes[i].LocalMatrix);
                    Gizmos.DrawWireCube(Vector3.zero, bakedData.boundingBoxes[i].size);
                }
            }

            if (showProceduralPoints && bakedData.proceduralPoints != null && bakedData.proceduralPoints.Count > 0 )
            {
                for (int i = 0; i < bakedData.proceduralPoints.Count; i++)
                {
                    var point = bakedData.proceduralPoints[i];
                    DrawPoint(point);
                }
            }
        }
    }

    private void DrawPoint(ProDecPoint point)
    {
        point.UpdateMatrix(transform.localToWorldMatrix);
        Gizmos.matrix = point.LTWMatrix;
        Gizmos.color = Color.gray;
        Gizmos.DrawCube(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(point.WorldPos, point.Forward * orientationRayLength);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(point.WorldPos, point.Up * orientationRayLength);
    }
}

[Serializable]
public class BakedTunnelSection
{
    [SerializeField] private int orignalInstanceId;
    [SerializeField] private TunnelSection originalInstance;
    
    [Header("Procedural Decorator")]
    [SerializeField] private MapResource[] resources;
    [SerializeField] private float coverage = 1f;
    public List<ProDecPoint> proceduralPoints;
    
    [Header("Linking and Spawning")]
    public Connector[] connectors;
    [SerializeField] private List<TunnelSection> excludePrefabConnections = new();
    public List<ConnectorMask> excludeConnectorSections = new();
    [SerializeField] private List<int> excludePrefabConnectionsIds;
    [SerializeField] private SectionSpawnBaseRule spawnRule;

    [Header("Virtual Physics Environment")]
    public BoxBounds[] boundingBoxes;

    [Header("Ambiance")]
    [SerializeField] private float sectionLightLevel = 0.202f;
    [SerializeField] private AudioClip sectionAmbience;

    [Header("Misc")]
    [SerializeField] private Texture2D miniMapAsset;
    [SerializeField] private Vector3 strongKeepPosition;
    [SerializeField] private string waypointName;

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
    public bool Spawnable => SpawnRule.Spawnable;
    public int OriginalInstanceId => orignalInstanceId;
    public int SpawnDebt => SpawnRule.SpawnDebt;
    public TunnelSection OriginalInstance => originalInstance;
    public SectionSpawnBaseRule SpawnRule => spawnRule;
    public List<int> ExcludePrefabConnectionsIds => excludePrefabConnectionsIds;

    public int InstanceCount
    {
        get => spawnRule.InstancesCount;
        set => spawnRule.InstancesCount = value;
    }


    public ConnectorMask GetConnectorMask(Connector connector)
    {
        return excludeConnectorSections[connector.internalIndex];
    }

    public void SetSpawnRule(SectionSpawnBaseRule rule)
    {
        spawnRule = rule;
    }


    public void Build(SpatialParadoxGenerator generator,int originalInstanceId, bool oldMode = false)
    {
        this.orignalInstanceId = originalInstanceId;

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
        excludePrefabConnections.ForEach(section => excludePrefabConnectionsIds.Add(section.orignalInstanceId));
        if (!oldMode)
        {
            excludePrefabConnections.Clear();
            excludePrefabConnections = null;
        }
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
