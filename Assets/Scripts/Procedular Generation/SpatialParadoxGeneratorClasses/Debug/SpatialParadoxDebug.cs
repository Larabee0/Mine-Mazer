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
    [HideInInspector] public bool transformDebugging = false;
    [SerializeField, Min(0.5f)] private float transformHoldTime = 2.5f;
    [SerializeField] private GameObject santiziedCube;
    private GameObject primaryObj;
    private GameObject secondaryObj;

    private void Debugging()
    {
        if (santiziedCube == null)
        {
            Debug.LogWarning("Santizied cube unassigned!");
            return;
        }
        if (transformDebugging)
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

    private IEnumerator BreakEditor()
    {
        yield return null;
        yield return null;
        Debug.Break();
    }
}
#endif
