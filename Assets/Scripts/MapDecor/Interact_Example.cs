using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Fungus;
using MazeGame.Input;

public class Interact_Example : MonoBehaviour, IInteractable
{
    public Flowchart Dialogue;

    protected virtual void Start()
    {
        Dialogue = GetComponentInChildren<Flowchart>();
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

    public virtual void Interact()
    {
        Dialogue.ExecuteBlock("NPC Interact");
    }

    public virtual string GetToolTipText()
    {
        if (InputManager.GamePadPresent)
        {
            return string.Format("B to Interact with {0}", Dialogue.GetComponent<Character>().NameText);
        }
        else
        {
            return string.Format("E to Interact with {0}",Dialogue.GetComponent<Character>().NameText);
        }
    }
}
