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
    [SerializeField] private BakedTunnelSection dataFromBake;
    public BakedTunnelSection DataFromBake
    {
        get { return dataFromBake; }
        set { dataFromBake = value; }
    }

    [Header("Runtime Data")]
    public MapTreeElement treeElementOwner;
    public GameObject stagnationBeacon;
    public int orignalInstanceId;
    [SerializeField] private bool weakKeep = false;
    [SerializeField] private Transform decorations;
    [SerializeField] private float decorCoverage;
    [SerializeField] private float decorInteractableRatio;
    public int decorationCount;
    private bool doFullCoverage = false;
    private MapResource fullCoveragePrefab = null;
    // accessors 
    public Vector3 WaypointPosition => stagnationBeacon != null ? stagnationBeacon.transform.position : transform.TransformPoint(StrongKeepPosition);
    public float AmbientLightLevel => DataFromBake.AmbientLightLevel;
    public AudioClip AmbientNoise => DataFromBake.AmbientNoise;
    public string WaypointName => stagnationBeacon != null ? stagnationBeacon.name : DataFromBake.WaypointName;
    public bool StrongKeep => DataFromBake.StrongKeep;
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

    public bool HasLadder => DataFromBake.HasLadder;
    public bool IsColony => DataFromBake.IsColony;
    
    public bool explored = false;

    public int InstanceCount
    {
        get => DataFromBake.InstanceCount;
        set => DataFromBake.InstanceCount = value;
    }

    public bool Spawnable => DataFromBake.SpawnRule.Spawnable;
    public int SpawnDebt => DataFromBake.SpawnRule.SpawnDebt;

    public Texture2D MiniMapAsset => DataFromBake.MiniMapAsset;
    public Vector3 Position => transform.position;
    public Vector3 StrongKeepPosition => DataFromBake.StrongKeepPosition;
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

    public BoxBounds[] BoundingBoxes => DataFromBake.boundingBoxes;

    public List<int> ExcludePrefabConnections => DataFromBake.ExcludePrefabConnectionsIds;


    public void GenerateNavMeshLinks()
    {
        links = GetComponents<NavMeshLink>();
        if (links == null || links.Length != DataFromBake.connectors.Length)
        {
            links = new NavMeshLink[DataFromBake.connectors.Length];
            for (int i = 0; i < links.Length; i++)
            {
                var link = links[i] = gameObject.AddComponent<NavMeshLink>();
                link.autoUpdate = true;
                link.startPoint = DataFromBake.connectors[i].localPosition + new Vector3(0, -1.5f, 0) + ((DataFromBake.connectors[i].localRotation * Vector3.forward) * -0.5f);
                link.endPoint = DataFromBake.connectors[i].localPosition + new Vector3(0, -1.5f, 0) + (DataFromBake.connectors[i].localRotation * Vector3.forward * 2.5f);
            }
        }
    }

    public ConnectorMask GetConnectorMask(Connector connector)
    {
        return DataFromBake.GetConnectorMask(connector);
    }

    public void Build(SpatialParadoxGenerator generator)
    {
        orignalInstanceId = GetInstanceID();
        GenerateNavMeshLinks();
    }

    public bool UpdateRule()
    {
        return DataFromBake.UpdateRule();
    }

    public void Spawned()
    {
        DataFromBake.Spawned();
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

    public int Decorate(float coverage,float decorInteractableRatio, float fullCoverageChance, DecorContainer decorContainer)
    {
        decorCoverage = coverage;
        doFullCoverage = Random.value < fullCoverageChance;
        this.decorInteractableRatio = decorInteractableRatio;
        if (doFullCoverage)
        {
            coverage = 1f;
            float chance = Random.Range(0, decorContainer.chancesFCI[^1]);
            fullCoveragePrefab = DecorContainer.PickFromArray(decorContainer.fullCoverageItems, decorContainer.chancesFCI, chance);
        }

        if (coverage > 0)
        {
            StartCoroutine(DecorateProcess(decorContainer));
        }
        
        return decorationCount = (int)(DataFromBake.proceduralPoints.Count * coverage);
    }

    private IEnumerator DecorateProcess(DecorContainer decorContainer)
    {
        List<ProDecPoint> points = new(DataFromBake.proceduralPoints);

        int totalPoints = DataFromBake.proceduralPoints.Count;
        NativeArray<ProDecPointBurst> pointsBurst = new(DataFromBake.proceduralPoints.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
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
        yield return DecorateIncremental(points, decorContainer, totalPoints, interationTime);

        SetRenderersEnabled(renderersEnabled);
    }

    private IEnumerator DecorateIncremental(List<ProDecPoint> points, DecorContainer decorContainer,int totalPoints, double interationTime)
    {
        while ((float)points.Count / (float)totalPoints > 1f - decorCoverage)
        {
            int index = Random.Range(0, points.Count);
            ProDecPoint point = points[index];
            point.UpdateMatrix(transform.localToWorldMatrix);
            MapResource prefab = decorContainer.PickPrefab(decorInteractableRatio, doFullCoverage, fullCoveragePrefab);
            MapResource trs = Instantiate(prefab, point.WorldPos, Quaternion.identity, decorations);
            TransformDecoration(point, trs);
            points.RemoveAt(index);
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



    private static void TransformDecoration(ProDecPoint point, MapResource trs)
    {
        if (trs.ItemStats.type == Item.Versicolor)
        {
            trs.transform.localRotation = Quaternion.LookRotation(point.Up, Vector3.up);
        }
        else if (trs.ItemStats.type == Item.None)
        {
            if (trs.ItemStats.name == "ceiling_lantern")
            {
                trs.transform.localRotation = Quaternion.LookRotation(point.Forward, Vector3.up);
            }
            else if(trs.ItemStats.name == "Fungal Steak")
            {
                trs.transform.localRotation = Quaternion.LookRotation(point.Up, Vector3.up);
            }
            else
            {
                trs.transform.up = point.Up;
                trs.transform.Rotate(Vector3.up, Random.Range(0, 359f), Space.Self);
            }
        }
        else
        {
            trs.transform.up = point.Up;
            trs.transform.Rotate(Vector3.up, Random.Range(0, 359f), Space.Self);
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
        if (DataFromBake != null)
        {
            if (StrongKeep)
            {
                Gizmos.DrawCube(transform.TransformPoint(DataFromBake.StrongKeepPosition), Vector3.one);
            }
            if (DataFromBake.connectors != null)
            {
                for (int i = 0; i < DataFromBake.connectors.Length; i++)
                {
                    Gizmos.matrix = GetLTWConnectorMatrix(transform.localToWorldMatrix, DataFromBake.connectors[i]);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(Vector3.zero, Vector3.forward);
                }
            }

            Gizmos.color = Color.red;
            if (DataFromBake.boundingBoxes != null)
            {
                for (int i = 0; i < DataFromBake.boundingBoxes.Length; i++)
                {
                    Gizmos.matrix = math.mul(float4x4.TRS(transform.position, transform.rotation, Vector3.one), DataFromBake.boundingBoxes[i].LocalMatrix);
                    Gizmos.DrawWireCube(Vector3.zero, DataFromBake.boundingBoxes[i].size);
                }
            }
        }
    }
}
