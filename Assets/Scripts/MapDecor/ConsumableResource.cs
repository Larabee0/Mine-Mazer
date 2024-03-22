using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsumableResource : MapResource
{
    [SerializeField] private bool consumeable = true;
    public override bool PlaceItem()
    {
        if (consumeable && Inventory.Instance.TryRemoveItem(ItemStats.type, 1, out MapResource item))
        {
            Hunger.Instance.SetToFull();
            Destroy(item.gameObject);
            return true;
        }
        return false;
    }
}
