using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class StagnationBeacon : SanctumPart
{
    [Header("Animations")]
    [SerializeField] private bool Animated = true;
    [SerializeField] private Transform sphere;
    [SerializeField] private Transform cylinder;
    [SerializeField] private Transform cubeA;
    [SerializeField] private Transform cubeB;
    [SerializeField] private float3 dst = new(0, 0.5f, 0);
    [SerializeField] private float time = 10f;
    public TunnelSection targetSection;
    private float3x2 sphereStartEnd;
    private float3x2 cylinderStartEnd;

    public string BeaconName
    {
        get => gameObject.name;
        set => gameObject.name = value;
    }
    protected override void Start()
    {
    }

    private void OnEnable()
    {
        if (Animated)
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

    public override void Interact()
    {
        SpatialParadoxGenerator mapGenerator = FindObjectOfType<SpatialParadoxGenerator>();
        mapGenerator.RemoveStagnationBeacon(this);
        base.Interact();
    }

    public override bool PlaceItem()
    {
        SpatialParadoxGenerator mapGenerator = FindObjectOfType<SpatialParadoxGenerator>();
        Ray r = new(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, 5))
        {
            TunnelSection upStack = hitInfo.transform.gameObject.GetComponentInParent<TunnelSection>();
            TunnelSection downStack = hitInfo.transform.gameObject.GetComponentInChildren<TunnelSection>();
            TunnelSection hitSection = upStack == null ? downStack : upStack;
            if (hitSection != null && hitSection.stagnationBeacon == null
                && !hitSection.StrongKeep
                && Inventory.Instance.TryRemoveItem(ItemStats.type, 1, out MapResource item) && item == this)
            {
                item.gameObject.transform.position = hitInfo.point + placementPositionOffset;
                item.gameObject.transform.up = Vector3.up;
                item.gameObject.transform.localScale = originalScale;
                item.SetMapResourceActive(true);
                item.SetColliderActive(true);
                mapGenerator.PlaceStatnationBeacon(hitSection, this);
                OnItemPickedUp?.Invoke();
                return true;
            }
        }
        return false;
    }
}
