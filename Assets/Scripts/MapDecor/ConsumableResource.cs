using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsumableResource : MapResource
{
    [SerializeField] private bool consumeable = true;
    public bool Consumable => consumeable;
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

    public override string GetToolTipText()
    {
        if (pickedUp)
        {
            if(InputManager.GamePadPresent)
            {
                return string.Format("Use with LT");
            }
            else
            {
                return string.Format("Use with Right Click");
            }
        }
        else
        {
            return base.GetToolTipText();
        }
    }
}
