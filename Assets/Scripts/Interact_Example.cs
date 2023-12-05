using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Fungus;

public class Interact_Example : MonoBehaviour, IInteractable
{
    public Flowchart Dialogue;
    // Start is called before the first frame update
    public void Interact()
    {
        Dialogue.ExecuteBlock("NPC Interact");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
