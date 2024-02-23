using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Larmiar_Interaction : NPCTrade
{
    [SerializeField] private ItemStats[] itemsOfInterestKeys;

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
}
