using Fungus;
using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SanctumPart : MapResource
{
    [SerializeField] private Flowchart dialogue;
    [SerializeField] private TunnelSection sectionParent;
    [SerializeField] private CaveMessageInteractable sanctumMachine;

    public static PlayerExplorationStatistics explorationStatistics;


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
        SetRainbowOpacity(0);
        Inventory.Instance.TryMoveItemToHand(ItemStats.type);
        PlayerAnimationController.Instance.OnEquipEnd += ExecuteDialogue;
    }

    private void ExecuteDialogue()
    {
        if(sanctumMachine != null && sanctumMachine.Read)
        {
            dialogue.SetBooleanVariable("ReadNote", true);
        }
        if (dialogue != null)
        {
            dialogue.ExecuteBlock("SpeakToLarmiar");
        }
        PlayerAnimationController.Instance.OnEquipEnd -= ExecuteDialogue;
    }

    public void HeartNodeReadNote()
    {
        if (sanctumMachine != null)
        {
            sanctumMachine.Interact();
            sanctumMachine.onNoteClose += OnSanctumNoteClose;
        }
    }

    private void OnSanctumNoteClose()
    {
        InteractMessage.Instance.SetObjective("Ask Larimar about the Heart Node");
    }

    public void SetObjective()
    {
        InteractMessage.Instance.SetObjective("Talk to Larimar about the Heart Node");
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
