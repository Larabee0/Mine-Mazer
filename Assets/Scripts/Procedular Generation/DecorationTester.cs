
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
public class DecorationTester : MonoBehaviour
{
    [SerializeField] private TunnelSection target;
    [SerializeField] private List<GameObject> spawnedDecorations;
    [SerializeField] private MapResource[] interactables;
    [SerializeField] private float[] chancesI;
    [SerializeField] private MapResource[] decorations;
    [SerializeField] private float[] chancesD;
    [SerializeField] private MapResource[] fullCoverageItems;
    [SerializeField] private float[] chancesFCI;
    [SerializeField] private float decorCoverage;
    [SerializeField] private float decorInteractableRatio = 0.5f;
    [SerializeField] private float fullCoveragChance = 0.05f;
    private bool doFullCoverage = false;
    private MapResource fullCoveragePrefab = null;
    private BakedTunnelSection DataFromBake => target.DataFromBake;

    public void Redecorate()
    {
        if(spawnedDecorations.Count > 0)
        {
            spawnedDecorations.ForEach(e=> DestroyImmediate(e));
        }
        spawnedDecorations.Clear();
        chancesI = CalculateChances(interactables);
        chancesD = CalculateChances(decorations);
        chancesFCI = CalculateChances(fullCoverageItems);

        List<ProDecPoint> points = new(DataFromBake.proceduralPoints);
        int totalPoints = DataFromBake.proceduralPoints.Count;
        NativeArray<ProDecPointBurst> pointsBurst = new(DataFromBake.proceduralPoints.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        JobHandle handle = ScheduleMatrixUpdate(points, pointsBurst);
        handle.Complete();

        for (int i = 0; i < points.Count; i++)
        {
            ProDecPoint point = points[i];
            point.UpdateMatrix(pointsBurst[i]);
            points[i] = point;
        }
        pointsBurst.Dispose();

        DecorateIncremental(points, totalPoints,decorCoverage);
    }

    private static float[] CalculateChances(MapResource[] resources)
    {
        float[] chances = new float[resources.Length];
        float runningTotal = 0f;
        for (int i = 0; i < resources.Length; i++)
        {
            runningTotal += resources[i].Rarity;
            chances[i] = runningTotal;
        }
        return chances;
    }


    private void DecorateIncremental(List<ProDecPoint> points, int totalPoints, float coverage)
    {
        doFullCoverage = Random.value < fullCoveragChance;

        if (doFullCoverage)
        {
            coverage = 1f;
            float chance = Random.Range(0, chancesFCI[^1]);
            fullCoveragePrefab = PickFromArray(fullCoverageItems, chancesFCI, chance);
        }

        while ((float)points.Count / (float)totalPoints > 1f - coverage)
        {
            int index = Random.Range(0, points.Count);
            ProDecPoint point = points[index];
            point.UpdateMatrix(transform.localToWorldMatrix);

            MapResource prefab;
            prefab = PickPrefab();

            MapResource trs = Instantiate(prefab, point.WorldPos, Quaternion.identity, transform);
            spawnedDecorations.Add(trs.gameObject);
            ProcessDecoration(point, trs);

            points.RemoveAt(index);
        }
    }

    private static void ProcessDecoration(ProDecPoint point, MapResource trs)
    {
        trs.ForceInit();
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

    private MapResource PickPrefab()
    {
        MapResource prefab;
        if (doFullCoverage)
        {
            prefab = fullCoveragePrefab;
        }
        else if (decorInteractableRatio < Random.value)
        {
            float chance = Random.Range(0, chancesD[^1]);

            prefab = PickFromArray(decorations, chancesD, chance);
        }
        else
        {
            float chance = Random.Range(0, chancesI[^1]);
            prefab = PickFromArray(interactables, chancesI, chance);
        }

        return prefab;
    }

    private static MapResource PickFromArray(MapResource[] resources, float[] chances, float value)
    {
        int index = -1;
        for (int i = chances.Length - 1; i >= 1 ; i--)
        {
            if (chances[i] >= value && chances[i-1] <= value)
            {
                index = i; break;
            }
        }
        if(index < 0)
        {
            index = 0;
        }
        return resources[index];
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
}
#endif

public struct DecorContainer
{
    public MapResource[] interactables;
    public MapResource[] decorations;
    public MapResource[] fullCoverageItems;
    public float[] chancesI;
    public float[] chancesD;
    public float[] chancesFCI;


    public MapResource PickPrefab(float decorInteractableRatio, bool doFullCoverage, MapResource fullCoveragePrefab)
    {
        MapResource prefab;
        if (doFullCoverage)
        {
            prefab = fullCoveragePrefab;
        }
        else if (decorInteractableRatio < Random.value)
        {
            float chance = Random.Range(0, chancesD[^1]);

            prefab = PickFromArray(decorations, chancesD, chance);
        }
        else
        {
            float chance = Random.Range(0, chancesI[^1]);
            prefab = PickFromArray(interactables, chancesI, chance);
        }

        return prefab;
    }

    public static MapResource PickFromArray(MapResource[] resources, float[] chances, float value)
    {
        int index = -1;
        for (int i = chances.Length - 1; i >= 1; i--)
        {
            if (chances[i] >= value && chances[i - 1] <= value)
            {
                index = i; break;
            }
        }
        if (index < 0)
        {
            index = 0;
        }
        return resources[index];
    }
}
