using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Eudie_Item : MapResource
{
    [HideInInspector] public bool pickUpEudie =false;
    [HideInInspector] public bool putDownEudieToolTip = false;
    public Action OnEudiePlaced;
    public override string GetToolTipText()
    {
        if(putDownEudieToolTip)
        {
            if (InputManager.GamePadPresent)
            {
                return string.Format("LT to Place {0} on Floor", ToolTipName);
            }
            else
            {
                return string.Format("Right Click to Place {0} on Floor", ToolTipName);
            }
        }
        if (pickUpEudie)
        {
            return base.GetToolTipText();
        }
        return null;
    }

    public override void Interact()
    {
        
    }

    public override void SetMapResourceActive(bool active)
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].enabled = active;
        }
    }

    public override bool PlaceItem()
    {
        if (putDownEudieToolTip)
        {
            if (base.PlaceItem())
            {
                Interact_Example[] npcs = transform.parent.GetComponentsInChildren<Interact_Example>();

                for (int i = 0;i < npcs.Length; i++)
                {
                    if (npcs[i].gameObject.name == gameObject.name)
                    {
                        continue;
                    }
                    npcs[i].TryShowInteractBubble();
                }

                OnEudiePlaced?.Invoke();
                InteractMessage.Instance.SetObjective("Talk to Eudies friends in the colony.");
                return true;
            }
        }
        return false;
    }

    public void PickUpEudieItem()
    {
        GetComponent<NavMeshAgent>().enabled = false;
        InteractMessage.Instance.SetObjective("Find the Lumenite Colony & Take Eudie to it.");
        base.Interact();
        Inventory.Instance.TryMoveItemToHand(Item.Eudie);
    }

    public void MakePlaceable()
    {
        Placeable = true;
        if (InputManager.GamePadPresent)
        {
            InteractMessage.Instance.SetObjective("Place Eudie in the colony with LT");
        }
        else
        {
            InteractMessage.Instance.SetObjective("Place Eudie in the colony with Right Click");
        }
    }
}
