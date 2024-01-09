using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class StagnationBeaconAnimation : MonoBehaviour
{
    [SerializeField] private Transform sphere;
    [SerializeField] private Transform cylinder;
    [SerializeField] private Transform cubeA;
    [SerializeField] private Transform cubeB;
    [SerializeField] private float3 dst = new(0, 0.5f, 0);
    [SerializeField] private float time = 10f;
    private float3x2 sphereStartEnd;
    private float3x2 cylinderStartEnd;

    public string BeaconName
    {
        get => gameObject.name;
        set => gameObject.name = value;
    }

    private void OnEnable()
    {
        sphereStartEnd = new()
        {
            c0 = (float3)sphere.localPosition - (dst * 0.5f),
            c1 = (float3)sphere.localPosition + (dst * 0.5f),
        };
        cylinderStartEnd = new()
        {
            c0 = (float3)cylinder.localPosition - (dst * 0.5f),
            c1 = (float3)cylinder.localPosition + (dst * 0.5f),
        };
        sphere.transform.localPosition = sphereStartEnd.c1;
        cylinder.transform.localPosition = cylinderStartEnd.c0;

        StartCoroutine(Animation());
    }

    private void OnDisable()
    {
        StopAllCoroutines();    
    }

    private IEnumerator Animation()
    {
        while(true)
        {
            float t = 0;
            for(; t<= time; t += Time.deltaTime)
            {
                yield return null;
                float s = math.unlerp(0, time, t);
                sphere.transform.localPosition = math.lerp(sphereStartEnd.c1, sphereStartEnd.c0, s);
                cylinder.transform.localPosition = math.lerp(cylinderStartEnd.c0, cylinderStartEnd.c1, s);

                Vector3 eulerA = cubeA.transform.localRotation.eulerAngles;
                eulerA.y += time * Time.deltaTime;
                cubeA.transform.localRotation = Quaternion.Euler(eulerA);

                Vector3 eulerB = cubeB.transform.localRotation.eulerAngles;
                eulerB.y -= time * Time.deltaTime;
                cubeB.transform.localRotation = Quaternion.Euler(eulerB);
            }

            for (; t > 0; t -= Time.deltaTime)
            {
                yield return null;
                float s = math.unlerp(0, time, t);
                sphere.transform.localPosition = math.lerp(sphereStartEnd.c1, sphereStartEnd.c0, s);
                cylinder.transform.localPosition = math.lerp(cylinderStartEnd.c0, cylinderStartEnd.c1, s);

                Vector3 eulerA = cubeA.transform.localRotation.eulerAngles;
                eulerA.y += time * Time.deltaTime;
                cubeA.transform.localRotation = Quaternion.Euler(eulerA);


                Vector3 eulerB = cubeB.transform.localRotation.eulerAngles;
                eulerB.y -= time * Time.deltaTime;
                cubeB.transform.localRotation = Quaternion.Euler(eulerB);
            }
        }
    }
}
