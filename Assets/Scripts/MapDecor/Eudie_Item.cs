using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eudie_Item : MapResource
{
    [HideInInspector] public bool pickUpEudie;
    public Pluse OnEudiePlaced;
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

    public override void SetMapResourceActive(bool active)
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].enabled = active;
        }
    }

    public override void PlaceItem()
    {
        base.PlaceItem();
        OnEudiePlaced?.Invoke();
        InteractMessage.Instance.SetObjective("Talk to Eudies friends in the colony.");
    }

    public void PickUpEudieItem()
    {

        InteractMessage.Instance.SetObjective("Find the Lumenite Colony & Take Eudie to it.");
        base.Interact();
    }

    public void MakePlaceable()
    {
        Placeable = true;
    }
}
