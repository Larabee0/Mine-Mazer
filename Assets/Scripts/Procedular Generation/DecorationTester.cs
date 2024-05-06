using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

public class DecorationTester : MonoBehaviour
{
    [SerializeField] private TunnelSection target;
    [SerializeField] private List<GameObject> spawnedDecorations;
    [SerializeField] private MapResource[] interactables;
    [SerializeField] private MapResource[] decorations;
    [SerializeField] bool decorOrInteractables = false;
    [SerializeField] int specificIndex = -1;
    [SerializeField] private float decorCoverage;

    private BakedTunnelSection DataFromBake => target.DataFromBake;

    public void Redecorate()
    {
        if(spawnedDecorations.Count > 0)
        {
            spawnedDecorations.ForEach(e=> DestroyImmediate(e));
        }

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

        MapResource[] selectedResources = decorOrInteractables ? decorations : interactables;
        MapResource[] spawnPool;
        if(specificIndex >= 0)
        {
            spawnPool = new MapResource[] { selectedResources[specificIndex] };
        }
        else
        {
            spawnPool = selectedResources;
        }

        DecorateIncremental(points, spawnPool, totalPoints);
    }


    private void DecorateIncremental(List<ProDecPoint> points, MapResource[] resources, int totalPoints)
    {
        while ((float)points.Count / (float)totalPoints > 1f - decorCoverage)
        {
            int index = Random.Range(0, points.Count);
            ProDecPoint point = points[index];
            point.UpdateMatrix(transform.localToWorldMatrix);
            MapResource trs = Instantiate(resources[Random.Range(0, resources.Length)], point.WorldPos, Quaternion.identity, transform);
            spawnedDecorations.Add(trs.gameObject);
            trs.ForceInit();
            if (trs.ItemStats.type == Item.Versicolor)
            {
                trs.transform.localRotation = Quaternion.LookRotation(point.Up, Vector3.up);
            }
            else if(trs.ItemStats.type == Item.None)
            {
                if(trs.ItemStats.name == "ceiling_lantern")
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


            points.RemoveAt(index);
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
}
