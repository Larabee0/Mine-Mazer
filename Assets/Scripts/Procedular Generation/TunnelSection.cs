using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class TunnelSection : MonoBehaviour
{
    [Header("Baked data")]

    [SerializeField] private NavMeshLink[] links;
    public BakedTunnelSection dataFromBake;



    //[SerializeField] private Texture2D miniMapAsset;
    //[SerializeField] private List<TunnelSection> excludePrefabConnections = new();
    //public  List<TunnelSection> ExcludePrefabConnectionsTS => excludePrefabConnections;
    //public Connector[] connectors;

    //public BoxBounds[] boundingBoxes;
    //public List<ConnectorMask> excludeConnectorSections = new();
    //[SerializeField] private Vector3 strongKeepPosition;
    //[SerializeField] private string waypointName;

    //[SerializeField] private SectionSpawnBaseRule spawnRule;
    //[SerializeField] private float sectionLightLevel = 0.202f;
    //[SerializeField] private AudioClip sectionAmbience;

    //[SerializeField] private List<int> excludePrefabConnectionsIds;
    //[SerializeField] private bool strongKeep = false;
    //[SerializeField] private bool hasLadder = false;
    //[SerializeField] private bool isColony = false;
    [Header("Runtime Data")]
    public MapTreeElement treeElementOwner;
    public GameObject stagnationBeacon;
    public int orignalInstanceId;
    private bool weakKeep = false;
    [SerializeField] private Transform sanctumPartsSpawnPoint;
    [SerializeField] private Transform decorations;
    [SerializeField] private float decorCoverage;
    public int decorationCount;
    // accessors 
    public Transform SanctumPartSpawnPoint=>sanctumPartsSpawnPoint;
    public Vector3 WaypointPosition => stagnationBeacon != null ? stagnationBeacon.transform.position : transform.TransformPoint(StrongKeepPosition);
    public float AmbientLightLevel => dataFromBake.AmbientLightLevel;
    public AudioClip AmbientNoise => dataFromBake.AmbientNoise;
    public string WaypointName => stagnationBeacon != null ? stagnationBeacon.name : dataFromBake.WaypointName;
    public bool StrongKeep => dataFromBake.StrongKeep;
    public bool Keep
    {
        get
        {
            return weakKeep || StrongKeep;
        }
        set
        {
            weakKeep = value;
        }
    }

    public bool HasLadder => dataFromBake.HasLadder;
    public bool IsColony => dataFromBake.IsColony;
    
    public bool explored = false;

    public int InstanceCount
    {
        get => dataFromBake.InstanceCount;
        set => dataFromBake.InstanceCount = value;
    }

    public bool Spawnable => dataFromBake.SpawnRule.Spawnable;
    public int SpawnDebt => dataFromBake.SpawnRule.SpawnDebt;

    public Texture2D MiniMapAsset => dataFromBake.MiniMapAsset;
    public Vector3 Position => transform.position;
    public Vector3 StrongKeepPosition => dataFromBake.StrongKeepPosition;
    public Quaternion Rotation => transform.rotation;
    private Renderer[] renderers;
    private Light[] lights;

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

    public BoxBounds[] BoundingBoxes => dataFromBake.boundingBoxes;

    public List<int> ExcludePrefabConnections => dataFromBake.ExcludePrefabConnectionsIds;


    public void GenerateNavMeshLinks()
    {
        links = GetComponents<NavMeshLink>();
        if (links == null || links.Length != dataFromBake.connectors.Length)
        {
            links = new NavMeshLink[dataFromBake.connectors.Length];
            for (int i = 0; i < links.Length; i++)
            {
                var link = links[i] = gameObject.AddComponent<NavMeshLink>();
                link.autoUpdate = true;
                link.startPoint = dataFromBake.connectors[i].localPosition + new Vector3(0, -1.5f, 0);
                link.endPoint = dataFromBake.connectors[i].localPosition + new Vector3(0, -1.5f, 0) + (dataFromBake.connectors[i].localRotation * Vector3.forward * 2.5f);
            }
        }
    }

    public ConnectorMask GetConnectorMask(Connector connector)
    {
        return dataFromBake.GetConnectorMask(connector);
    }

    public void Build(SpatialParadoxGenerator generator)
    {
        orignalInstanceId = GetInstanceID();
        GenerateNavMeshLinks();
        //if (spawnRule == null)
        //{
        //    if (!TryGetComponent(out spawnRule))
        //    {
        //        spawnRule = gameObject.AddComponent<SectionSpawnBaseRule>();
        //    }
        //}
        //if(spawnRule != null)
        //{
        //    spawnRule.owner = orignalInstanceId;
        //    spawnRule.generator = generator;
        //    InstanceCount = 0;
        //    spawnRule.ResetRule();
        //}
        //if(excludeConnectorSections.Count != connectors.Length)
        //{
        //    for (int i = 0; i < connectors.Length; i++)
        //    {
        //        excludeConnectorSections.Add(new() { excludeRuntime = new() });
        //    }
        //}
        //
        //if (excludePrefabConnections == null) return;
        //excludePrefabConnectionsIds.Clear();
        //excludePrefabConnectionsIds = new List<int>(excludePrefabConnections.Count);
        //excludePrefabConnections.ForEach(section=> excludePrefabConnectionsIds.Add(section.GetInstanceID()));
        ////excludePrefabConnections.Clear();
        ////excludePrefabConnections = null;
    }

    public bool UpdateRule()
    {
        return dataFromBake.UpdateRule();
    }

    public void Spawned()
    {
        dataFromBake.Spawned();
    }

    public static float4x4 GetLTWConnectorMatrix(float4x4 ltw, Connector connector)
    {
        return math.mul(ltw, connector.Matrix);
    }

    public void SetRenderersEnabled(bool enabled)
    {
        renderersEnabled = enabled;
        renderers = GetComponentsInChildren<Renderer>();
        lights = GetComponentsInChildren<Light>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = enabled;
            }
        }
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].enabled = enabled;
            }
        }
    }

    public void SetCollidersEnabled(bool enabled)
    {
        collidersEnabled = enabled;
        allColliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++)
        {
            allColliders[i].enabled = enabled;
        }
    }

    public int Decorate(float coverage, MapResource[] resources)
    {
        decorCoverage = coverage;
        if(coverage > 0)
        {
            StartCoroutine(DecorateProcess(resources));
        }
        
        return decorationCount = (int)(dataFromBake.proceduralPoints.Count * coverage);
    }

    private IEnumerator DecorateProcess(MapResource[] resources)
    {
        List<ProDecPoint> points = new(dataFromBake.proceduralPoints);

        int totalPoints = dataFromBake.proceduralPoints.Count;
        NativeArray<ProDecPointBurst> pointsBurst = new(dataFromBake.proceduralPoints.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        JobHandle handle = ScheduleMatrixUpdate(points, pointsBurst);
        while (!handle.IsCompleted)
        {
            yield return null;
        }
        handle.Complete();
        yield return null;

        for (int i = 0; i < points.Count; i++)
        {
            ProDecPoint point = points[i];
            point.UpdateMatrix(pointsBurst[i]);
            points[i] = point;
        }
        pointsBurst.Dispose();
        yield return null;

        double interationTime = Time.realtimeSinceStartupAsDouble;
        yield return DecorateIncremental(points, resources, totalPoints, interationTime);

        SetRenderersEnabled(renderersEnabled);
    }

    private IEnumerator DecorateIncremental(List<ProDecPoint> points, MapResource[] resources,int totalPoints, double interationTime)
    {
        while ((float)points.Count / (float)totalPoints > 1f - decorCoverage)
        {
            int index = Random.Range(0, points.Count);
            ProDecPoint point = points[index];
            points.RemoveAt(index);
            point.UpdateMatrix(transform.localToWorldMatrix);
            MapResource trs = Instantiate(resources[Random.Range(0, resources.Length)], point.WorldPos, Quaternion.identity, decorations);
            trs.transform.up = point.Up;
            if (trs.ItemStats.type == Item.Versicolor)
            {
                continue;
            }
            // trs.transform.RotateAroundLocal(Vector3.up, Random.Range(0, 359f));
            trs.transform.Rotate(Vector3.up, Random.Range(0, 359f), Space.Self);
            double timeCheck = (Time.realtimeSinceStartupAsDouble - interationTime) * 1000;
            if (timeCheck > 4)
            {
                //Debug.LogFormat("Spent {0}ms Instantiating, waiting for next frame", timeCheck);
                yield return null;
                interationTime = Time.realtimeSinceStartupAsDouble;
            }
            yield return null;
        }
    }

    private JobHandle ScheduleMatrixUpdate(List<ProDecPoint> points, NativeArray<ProDecPointBurst> pointsBurst)
    {
        for (int i = 0; i < points.Count; i++)
        {
            pointsBurst[i] = points[i];
        }

        int batches = Mathf.Max(2, points.Count / SystemInfo.processorCount);

        return new ProDecPointMatrixJob
        {
            parentMatrix = transform.localToWorldMatrix,
            points = pointsBurst
        }.ScheduleParallel(points.Count, batches, new JobHandle());
    }

    private void OnDrawGizmosSelected()
    {
        if (dataFromBake != null)
        {
            if (StrongKeep)
            {
                Gizmos.DrawCube(transform.TransformPoint(dataFromBake.StrongKeepPosition), Vector3.one);
            }
            if (dataFromBake.connectors != null)
            {
                for (int i = 0; i < dataFromBake.connectors.Length; i++)
                {
                    Gizmos.matrix = GetLTWConnectorMatrix(transform.localToWorldMatrix, dataFromBake.connectors[i]);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(Vector3.zero, Vector3.forward);
                }
            }

            Gizmos.color = Color.red;
            if (dataFromBake.boundingBoxes != null)
            {
                for (int i = 0; i < dataFromBake.boundingBoxes.Length; i++)
                {
                    Gizmos.matrix = math.mul(float4x4.TRS(transform.position, transform.rotation, Vector3.one), dataFromBake.boundingBoxes[i].LocalMatrix);
                    Gizmos.DrawWireCube(Vector3.zero, dataFromBake.boundingBoxes[i].size);
                }
            }
        }
    }
}
