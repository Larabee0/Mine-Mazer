using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : MapResource
{
    [SerializeField] private float maxDst;
    [SerializeField] private float transitionSpeed = 2;
    [SerializeField] private LampRange[] lights;
    [SerializeField] private LayerMask layerMask;
    RaycastHit hitInfo;
    float t;

    private void OnEnable()
    {
        if(Inventory.Instance.CurHeldItem == Item.Torch)
        {
            AmbientController.Instance.AmbientTorchLightBoost(true);
        }
    }

    private void Update()
    {
        Ray ray = new(Camera.main.transform.position, Camera.main.transform.forward);
        Physics.Raycast(ray, out hitInfo, float.MaxValue,layerMask,QueryTriggerInteraction.Ignore);
        Debug.DrawRay(ray.origin, ray.direction);
        float dst = hitInfo.distance;
        t = Mathf.InverseLerp(0, maxDst, dst);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].SetBrightness(t,Time.deltaTime* transitionSpeed);
        }
    }

    private void OnDisable()
    {
        if (Inventory.Instance.CurHeldItem != Item.Torch)
        {
            AmbientController.Instance.AmbientTorchLightBoost(false);
        }
    }

    public override bool PlaceItem()
    {
        
        bool result =  base.PlaceItem();
        if (result)
        {
            PlayerAnimationController.Instance.TorchDeploy();
        }
        return result; 
    }

    [Serializable]
    private struct LampRange
    {
        public float minBrightness;
        public float maxBrightness;
        public Light light;

        public void SetBrightness(float t, float speed)
        {
            float targetLight = Mathf.Lerp(minBrightness, maxBrightness, t);

            light.intensity = Mathf.MoveTowards(light.intensity, targetLight, speed);
        }
    } 
}
