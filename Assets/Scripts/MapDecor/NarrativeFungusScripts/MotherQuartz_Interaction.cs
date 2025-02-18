using Fungus;
using MazeGame.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MotherQuartz_Interaction : Larmiar_Interaction
{
    private bool inventorySubscribeLockout = false;

    protected override void Start()
    {
        Dialogue = GetComponentInChildren<Flowchart>();
        TryShowInteractBubble();
    }

    public override void Interact()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
        }
        if (explorationStatistics == null)
        {
            Debug.LogError("explorationStatistics is null on mother quartz!", gameObject);
        }
        else
        {
            Dialogue.SetBooleanVariable("PlayerKnows", explorationStatistics.PlayerLearnedFromlarimar);
        }
        base.Interact();
    }

    public void EndGame()
    {
        PlayerUIController.Instance.SetHungerVisible(false);
        PlayerUIController.Instance.ShowCrosshair = false;
        PlayerUIController.Instance.SetMiniMapVisible(false);
        InteractMessage.Instance.ClearObjective();
        InteractMessage.Instance.HideInteraction(true);
        WorldWayPointsController.Instance.ClearWaypoints();
        Application.Quit();
    }

    public void MQ_SpokenTo()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
            if (explorationStatistics == null)
            {
                Debug.LogError("explorationStatistics is null on mother quartz!", gameObject);
                return;
            }
        }
        explorationStatistics.SetMQ_SpokenTo();
    }

    public void MQ_LeaveBad()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
            if (explorationStatistics == null)
            {
                Debug.LogError("explorationStatistics is null on mother quartz!", gameObject);
                return;
            }
        }
        explorationStatistics.SetMQ_LeaveBad();
        InteractMessage.Instance.SetObjective("Find a way to help Mother Quartz");
    }

    public void MQ_LeaveGood()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
            if (explorationStatistics == null)
            {
                Debug.LogError("explorationStatistics is null on mother quartz!", gameObject);
                return;
            }
        }
        explorationStatistics.SetMQ_LeaveGood();
        InteractMessage.Instance.SetObjective("Find a way to help Mother Quartz");
    }

    public void MQ_ISee()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
            if (explorationStatistics == null)
            {
                Debug.LogError("explorationStatistics is null on mother quartz!", gameObject);
                return;
            }
        }
        explorationStatistics.SetMQ_ISee();
    }

    public void OnPlayerLeave()
    {
        if (!inventorySubscribeLockout)
        {
            inventorySubscribeLockout = true;
            Inventory.Instance.OnItemPickUp += OnItemPickUpMQ;
        }
        TryHideInteractBubble();
    }

    private void OnItemPickUpMQ(Item item, int arg2)
    {
        if(item == Item.SanctumMachine)
        {
            interacted = false;
            TryShowInteractBubble();
        }
    }

    public void PlayerPlaceSanctum()
    {
        TryHideInteractBubble();
        inventorySubscribeLockout = true;
        if (Inventory.Instance.TryGetItem(Item.SanctumMachine, out _))
        {
            Inventory.Instance.TryMoveItemToHand(Item.SanctumMachine);
            if( Inventory.Instance.CurHeldAsset is SanctumMachine sm)
            {
                sm.MakePlaceable();
                sm.OnPlaced += OnSanctumPlaced;
            }
        }
    }

    public void OnSanctumPlaced()
    {
        Dialogue.ExecuteBlock("ItsWorking");
    }
}
