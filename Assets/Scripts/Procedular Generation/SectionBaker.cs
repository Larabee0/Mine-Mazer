#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SectionBaker : MonoBehaviour
{
    [SerializeField] private GameObject[] boundsObjects;
    public GameObject[] BoundsObjects => boundsObjects;

    [SerializeField] private GameObject[] connectorObjects;
    public GameObject[] ConnectorObjects => connectorObjects;
    public string prefabsDirectory = "Assets/Prefabs";
    public string folderName = "Tunnel Assets";
    public LayerMask tunnelSectionLayerMask;
    public GameObject SectionModel;
    public bool SaveToPrefabs;
}
#endif