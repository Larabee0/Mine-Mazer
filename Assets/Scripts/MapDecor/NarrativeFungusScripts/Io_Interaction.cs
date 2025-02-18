using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Io_Interaction : NPCTrade
{
    [SerializeField] private ItemStats ioItem;
    bool triggered = false;
    [SerializeField] private string ioLikesVariable;
    public override void Interact()
    {
        if (!triggered)
        {
            if (Inventory.Instance.CanTrade(ioItem.type))
            {
                Dialogue.SetBooleanVariable(ioLikesVariable, true);
            }
            else
            {
                Dialogue.SetBooleanVariable(ioLikesVariable, false);
            }
            //triggered = true;
        }
        else
        {
            Dialogue.SetBooleanVariable(ioLikesVariable, false);
        }
        base.Interact();
    }

    public void SetObjectiveLarimar()
    {
        if (Larmiar_Interaction.explorationStatistics == null)
        {
            InteractMessage.Instance.SetObjective("Speak to Larimar");
        }
    }

    public void SetObjectiveStagnationBeacon()
    {
        if (Larmiar_Interaction.explorationStatistics == null)
        {
            InteractMessage.Instance.SetObjective("Show Larimar the Stagnation Beacon");
        }
    }
}
