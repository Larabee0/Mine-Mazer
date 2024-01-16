using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeGame.Input;
using MazeGame.Navigation;

//RYTECH. 2023. How to Make a Flexible Interaction System in 2 Minutes [C#] [Unity3D]. Available at: https://www.youtube.com/watch?v=K06lVKiY-sY [accessed 28 November 2023].
interface IInteractable
{
    public void Interact();
    public string GetToolTipText();
}

public class NPC_Interact : MonoBehaviour
{
    [SerializeField] private Transform InteractorSource;
    [SerializeField] private float InteractRange;
    [SerializeField] private LayerMask npcLayer;
    [SerializeField] private Texture2D interactionIcon;
    private WorldWayPoint tooltip;

    private bool hitInteractable = false;
    public bool HitInteractable => hitInteractable;
    private bool closedToolTip = true;
    private IInteractable interactable;

    private void Start()
    {
        InputManager.Instance.interactButton.OnButtonReleased += Interact;
    }

    private void Interact()
    {
        if(interactable== null)
        {
            Inventory.Instance.CurHeldAsset.PlaceItem();
        }
        else
        {
            interactable?.Interact();
        }
    }

    private void InteractableToolTip(Vector3 hitPosition)
    {
        if (interactable != null && InteractMessage.Instance != null)
        {
            string tooltipText = interactable.GetToolTipText();
            if (!string.IsNullOrEmpty(tooltipText) && !string.IsNullOrWhiteSpace(tooltipText))
            {
                InteractMessage.Instance.ShowInteraction(tooltipText, interactionIcon, Color.yellow);
            }
        }
    }

    private void RemoveInteractableToolTip()
    {
        if (InteractMessage.Instance == null || closedToolTip)
        {
            return;
        }
        closedToolTip = true;
        InteractMessage.Instance.HideInteraction();
    }

    private void Update()
    {
        Ray r = new(InteractorSource.position, InteractorSource.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange, npcLayer))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactable))
            {
                if(interactable != this.interactable)
                {
                    this.interactable = interactable;
                }
                InteractableToolTip(hitInfo.point);
                closedToolTip = false;
                hitInteractable = true;
                return;
            }
            else if(hitInfo.collider.gameObject.GetComponentInParent<IInteractable>() != null)
            {
                interactable= hitInfo.collider.gameObject.GetComponentInParent<IInteractable>();
                if (interactable != this.interactable)
                {
                    this.interactable = interactable;
                }
                InteractableToolTip(hitInfo.point);
                closedToolTip = false;
                hitInteractable = true;
                return;
            }
        }

        interactable = null;
        hitInteractable = false;
        RemoveInteractableToolTip();
    }
}
