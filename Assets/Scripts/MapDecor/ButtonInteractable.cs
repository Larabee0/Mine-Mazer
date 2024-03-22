using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    public Action OnSuccessfulActivation;

    public string GetToolTipText()
    {
        if(Inventory.Instance.CurHeldItem == Item.Torch)
        {
            if(InputManager.GamePadPresent)
            {
                return "Use Torch with A";
            }
            else
            {
                return "Use Torch with Left Click";
            }
        }
        else
        {
            return "Select Torch to Use";
        }
    }

    public void Interact()
    {
        if (Inventory.Instance.CurHeldItem == Item.Torch)
        {
            OnSuccessfulActivation?.Invoke();
        }
    }

    public bool RequiresPickaxe()
    {
        return false;
    }
}
