using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TunnelSectionTrigger : MonoBehaviour
{
    private TunnelSection sectionOwner;
    private SpatialParadoxGenerator generator;

    private void Awake()
    {
        sectionOwner = GetComponentInParent<TunnelSection>();
        if (sectionOwner == null)
        {
            Debug.LogError("Unable to resolve section owner reference", this);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        generator = sectionOwner.GetComponentInParent<SpatialParadoxGenerator>();
        if (generator == null)
        {
            Debug.LogError("Unable to resolve generator reference", this);
            enabled = false;
        }

        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            generator.PlayerEnterSection(sectionOwner);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            generator.PlayerExitSection(sectionOwner);
        }
    }
}
