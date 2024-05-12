using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class StartSequenceMove : MonoBehaviour
{
    [SerializeField] private CinemachineSmoothPath path;
    [SerializeField] private CinemachineVirtualCamera dollyCamera;
    [SerializeField] private float cameraSpeed  = 1.0f;

    public bool EndOfTravel = false;



    void Update()
    {
        var dolly = dollyCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        dolly.m_PathPosition += Time.deltaTime * cameraSpeed;
        EndOfTravel = dolly.m_PathPosition >= 1f;
    }
}
