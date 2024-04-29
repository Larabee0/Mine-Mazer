using Fungus;
using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SanctumPart : MapResource
{
    [SerializeField] private Flowchart dialogue;
    [SerializeField] private TunnelSection sectionParent;

    protected override void Start()
    {
        sectionParent = GetComponentInParent<TunnelSection>();
    }

    public override void Interact()
    {
        if (sectionParent != null)
        {
            sectionParent.Keep = false;
        }

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
