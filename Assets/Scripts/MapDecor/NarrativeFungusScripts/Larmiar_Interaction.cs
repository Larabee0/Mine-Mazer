using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Larmiar_Interaction : NPCTrade
{
    [SerializeField] private ItemStats[] itemsOfInterestKeys;
    [SerializeField] protected MapResource SanctumMachine;
    public static PlayerExplorationStatistics explorationStatistics;

    public void GetPlayerExplorationStatistics()
    {
        explorationStatistics = FindObjectOfType<PlayerExplorationStatistics>();
    }

    public override void Interact()
    {
        for (int i = 0; i < itemsOfInterestKeys.Length; i++)
        {
            if (Inventory.Instance.CanTrade(itemsOfInterestKeys[i].type))
            {
                Dialogue.SetBooleanVariable(itemsOfInterestKeys[i].name, true);
            }
            else
            {
                Dialogue.SetBooleanVariable(itemsOfInterestKeys[i].name, false);
            }
        }

        base.Interact();
    }

    protected override void TradeClose(bool newValue)
    {
        base.TradeClose(newValue);
    }

    public void TakeSanctumMachineToMQ()
    {
        explorationStatistics.SetAllowSanctumPartSpawn(false, true);
        InteractMessage.Instance.SetObjective("Take Sanctum Machine to Mother Quartz");
        Inventory.Instance.AddItem(Item.SanctumMachine, 1, Instantiate( SanctumMachine), true);
        Inventory.Instance.TryMoveItemToHand(Item.SanctumMachine);
    }

    public void Larmiar_SetMinesMove()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
            if (explorationStatistics == null)
            {
                Debug.LogError("Larmiar_SetMinesMove, explorationStatistics is null!", gameObject);
                return;
            }
        }

        explorationStatistics.SetLarmiar_MinesMove();
    }

    public void Larimar_EnableSancumtParts()
    {
        if (explorationStatistics == null)
        {
            GetPlayerExplorationStatistics();
            if (explorationStatistics == null)
            {
                Debug.LogError("Larmiar_SetMinesMove, explorationStatistics is null!", gameObject);
                return;
            }
        }
        explorationStatistics.SetAllowSanctumPartSpawn(true);
        InteractMessage.Instance.SetObjective("Look out for strange items.");
        Inventory.Instance.OnItemPickUp += OnItemPickUpLarimar;
    }

    private void OnItemPickUpLarimar(Item item, int arg2)
    {
        if (item != Item.SanctumMachine && itemsOfInterestKeys.Any(key => key.type == item))
        {
            interacted = false;
            TryShowInteractBubble();
        }
    }
}
