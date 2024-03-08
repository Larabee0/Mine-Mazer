using Fungus;
using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SanctumPart : MapResource
{
    [SerializeField] private Flowchart dialogue;

    public override void Interact()
    {
        base.Interact();
        Inventory.Instance.TryMoveItemToHand(ItemStats.type);
        if (dialogue != null)
        {
            dialogue.ExecuteBlock("SpeakToLarmiar");
        }
    }

    public void UnlockPointer()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.UnlockPointer();
        }
    }

    public void LockPointer()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.LockPointer();
        }
    }

}
