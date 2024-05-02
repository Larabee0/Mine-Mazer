using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemAudio : MonoBehaviour
{
    [SerializeField] private AudioSource itemPickUp;
    [SerializeField] private AudioSource itemPutDown;


    void Start()
    {
        Inventory.Instance.OnItemPickUpSfx += OnPickUpItem;
        Inventory.Instance.OnItemRemoveSfx += OnPutDownItem;
    }

    private void OnPickUpItem()
    {
        itemPickUp.Play();
    }

    private void OnPutDownItem()
    {
        itemPickUp.Play();
    }
}
