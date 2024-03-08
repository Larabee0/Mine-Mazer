using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScreenCameraMove : MonoBehaviour
{
    [SerializeField]private Vector3 AEuler;
    [SerializeField]private Vector3 BEuler;
    [SerializeField] private Vector3 APos;
    [SerializeField] private Vector3 BPos;
    [SerializeField]private float fadeTime = 10f;
    private float time = 0;

    private Quaternion ARotation;
    private Quaternion BRotation;

    private void Start()
    {
        ARotation = Quaternion.Euler(AEuler);
        BRotation = Quaternion.Euler(BEuler);
        transform.SetPositionAndRotation(APos, ARotation);
    }

    void Update()
    {
        if(time >= fadeTime)
        {
            (ARotation, BRotation) = (BRotation, ARotation);
            (APos, BPos) = (BPos, APos);
            time = 0;
        }

        float t = Mathf.InverseLerp(0, fadeTime, time);

        transform.SetPositionAndRotation(Vector3.Lerp(APos, BPos, t), Quaternion.Lerp(ARotation, BRotation, t));
        time += Time.deltaTime;
    }
}
