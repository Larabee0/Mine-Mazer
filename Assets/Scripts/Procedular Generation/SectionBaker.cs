using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionBaker : MonoBehaviour
{
    [SerializeField] private GameObject[] boundsObjects;
    public GameObject[] BoundsObjects => boundsObjects;

    [SerializeField] private GameObject[] connectorObjects;
    public GameObject[] ConnectorObjects => connectorObjects;

    public GameObject SectionModel;
}
