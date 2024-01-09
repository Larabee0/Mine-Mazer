using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveMessageController : MonoBehaviour
{
    [SerializeField] private string messageFilePath;
    [SerializeField] private SpatialParadoxGenerator mapGenerator;

    private void Awake()
    {
        
        if (mapGenerator == null || !mapGenerator.isActiveAndEnabled)
        {
            Debug.LogError("No Map Generator or Map Generator Disabled");
            enabled = false;
            return;
        }

    }

    void Update()
    {

    }
}
