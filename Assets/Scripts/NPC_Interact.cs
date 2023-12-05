using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
//RYTECH. 2023. How to Make a Flexible Interaction System in 2 Minutes [C#] [Unity3D]. Available at: https://www.youtube.com/watch?v=K06lVKiY-sY [accessed 28 November 2023].
interface IInteractable
{
    public void Interact();
}

public class NPC_Interact : MonoBehaviour
{
    public Transform InteractorSource;
    public float InteractRange;

    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
            if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange))
            {
                if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
                {
                    interactObj.Interact();
                }
            }
        }
    }
}
