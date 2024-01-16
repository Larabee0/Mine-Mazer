using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiVariantResource : MapResource
{
    [Space]
    [Header("Multi-Variant Resource")]
    [SerializeField] private GameObject[] variants;
    [Header("Runtime Selected")]
    [SerializeField] private GameObject chosenVariant;

    protected override void Awake()
    {
        base.Awake();
        if (variants != null && variants.Length > 0)
        {
            for (int i = 0; i < variants.Length; i++)
            {
                variants[i].SetActive(false);
            }
            chosenVariant = variants[Random.Range(0, variants.Length)];
            chosenVariant.SetActive(true);
            itemCollider = chosenVariant.GetComponentInChildren<Collider>();
        }
        else
        {
            Debug.LogError("No variants assigned to MultiVariantResource!", gameObject);
        }
    }
}
