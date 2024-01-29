#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class SpatialParadoxGenerator
{
    // extra variables and settings for the debugger, these aren't compiled into the build.
    [Header("Debug")]
    [SerializeField] private TunnelSection prefab1;
    [SerializeField] private TunnelSection prefab2;
    [HideInInspector] public bool debugging = false;
    [HideInInspector] public bool initialAreaDebugging = false;
    [HideInInspector] public bool transformDebugging = false;
    [SerializeField, Min(0.5f)] private float intersectTestHoldTime = 2.5f;
    [SerializeField, Min(0.5f)] private float distanceListPauseTime = 5f;
    [SerializeField, Min(0.5f)] private float transformHoldTime = 2.5f;
    [SerializeField] private GameObject santiziedCube;
    private GameObject primaryObj;
    private GameObject secondaryObj;

    private TunnelSection targetSectionDebug;
    private Connector primaryPreferenceDebug;
    private Connector secondaryPreferenceDebug;
    private bool intersectionTest;

    private void Debugging()
    {
        if (santiziedCube == null)
        {
            Debug.LogWarning("Santizied cube unassigned!");
            return;
        }
        if (initialAreaDebugging)
        {
            Random.state = seed;
            StartCoroutine(GenerateInitialAreaDebug());
        }
        else if (transformDebugging)
        {
            if (prefab1 == null || prefab2 == null)
            {
                Debug.LogWarning("prefab not assigned!");
            }
            StartCoroutine(TransformDebugging());
        }
        else
        {
            Random.state = seed;
            GenerateInitialArea();
        }
    }

    /// <summary>
    /// slow step through map generation to allow visualisation.
    /// Has side effect to force physics ticks between intersection tests.
    /// </summary>
    /// <returns></returns>
    private IEnumerator GenerateInitialAreaDebug()
    {
        if (startSection == null)
        {
            int spawnIndex = Random.Range(0, tunnelSectionsByInstanceID.Count);
            curPlayerSection = InstinateSection(spawnIndex);
        }
        else
        {
            curPlayerSection = InstinateSection(startSection);
        }
        curPlayerSection.transform.position = new Vector3(0, 0, 0);

        mapTree.Add(new() { curPlayerSection });
        yield return RecursiveBuilderDebug();
        yield return new WaitForSeconds(distanceListPauseTime * 2);

        Debug.Log("Ended Initial Area Debug");
        OnMapUpdate?.Invoke();
    }

    private IEnumerator BreakEditor()
    {
        yield return null;
        yield return null;
        Debug.Break();
    }
}
#endif
