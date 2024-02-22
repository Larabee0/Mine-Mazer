using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MotherQuartz_Interaction : Larmiar_Interaction
{
    public override void Interact()
    {
        base.Interact();
    }
    public void EndGame()
    {
        SceneManager.LoadScene(0);
    }
}
