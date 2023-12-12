using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeGame.Input;

//RYTECH. 2023. How to Make a Flexible Interaction System in 2 Minutes [C#] [Unity3D]. Available at: https://www.youtube.com/watch?v=K06lVKiY-sY [accessed 28 November 2023].
interface IInteractable
{
    public void Interact();
}

public class NPC_Interact : MonoBehaviour
{
    [SerializeField] private Transform InteractorSource;
    [SerializeField] private float InteractRange;
    [SerializeField] private LayerMask npcLayer;

    private bool hitInteractable = false;
    public bool HitInteractable => hitInteractable;

    private IInteractable interactable;

    private void Start()
    {
        InputManager.Instance.interactButton.OnButtonReleased += Interact;
    }

    private void Interact()
    {
        interactable?.Interact();
    } 

    private void FixedUpdate()
    {
        Ray r = new(InteractorSource.position, InteractorSource.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange, npcLayer))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out interactable))
            {
                hitInteractable = true;
                //interactObj.Interact();
            }
            else
            {
                interactable = null;
                hitInteractable = false;
            }
        }
        else
        {
            interactable = null;
            hitInteractable = false;
        }
    }
}
