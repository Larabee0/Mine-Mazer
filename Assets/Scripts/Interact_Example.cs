using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Fungus;
using MazeGame.Input;

public class Interact_Example : MonoBehaviour, IInteractable
{
    public Flowchart Dialogue;

    private void Start()
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

    public void Interact()
    {
        Dialogue.ExecuteBlock("NPC Interact");
    }

}
