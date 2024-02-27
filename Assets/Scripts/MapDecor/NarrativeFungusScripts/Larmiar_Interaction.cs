using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Larmiar_Interaction : NPCTrade
{
    [SerializeField] private ItemStats[] itemsOfInterestKeys;

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
        if(newValue && curOption != null)
        {
            explorationStatistics.SetAllowSanctumPartSpawn(false, true);
            InteractMessage.Instance.SetObjective("Take Sanctum Machine to Mother Quartz");
        }
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
    }
}
