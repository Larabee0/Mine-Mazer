using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class SanctumMachine : MapResource
{
    public Action OnPlaced;
    public void MakePlaceable()
    {
        Placeable = true;
    }

    public override bool PlaceItem()
    {
        if (base.PlaceItem())
        {
            OnPlaced?.Invoke();
            Interactable = false;
            return true;
        }
        return false;
    }
}
