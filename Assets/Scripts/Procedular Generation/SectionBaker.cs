using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionBaker : MonoBehaviour
{
    [SerializeField] private GameObject[] boundsObjects;
    public GameObject[] BoundsObjects => boundsObjects;

    public GameObject SectionModel;
    public GameObject SectionStart;
    public GameObject SectionEnd;
}
