using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eudie_Item : MapResource
{
    [HideInInspector] public bool pickUpEudie;

    public override string GetToolTipText()
    {
        if (pickUpEudie)
        {
            return base.GetToolTipText();
        }
        return null;
    }

    public override void Interact()
    {
        
    }

    public void PickUpEudieItem()
    {
        base.Interact();
    }
}
