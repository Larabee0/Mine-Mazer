#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SectionBaker : MonoBehaviour
{
    [SerializeField] private GameObject[] boundsObjects;
    [SerializeField] private GameObject[] connectorObjects;
    [SerializeField] private GameObject[] connectorTriggers;
    [SerializeField] private GameObject[] additionalChildren;

    public GameObject[] BoundsObjects => boundsObjects;
    public GameObject[] ConnectorObjects => connectorObjects;
    public GameObject[] ConnectorTriggers => connectorTriggers;
    public GameObject[] AdditionalChildren => additionalChildren;

    public string prefabsDirectory = "Assets/Prefabs";
    public string folderName = "Tunnel Assets";
    public LayerMask tunnelSectionLayerMask;
    public GameObject SectionModel;
    public bool SaveToPrefabs;
}
#endif