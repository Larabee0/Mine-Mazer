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

    private WorldWayPoint tooltip;

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

    private void InteractableToolTip(Vector3 hitPosition)
    {
        if(interactable != null && WorldWayPointsController.Instance != null)
        {
            RemoveInteractableToolTip();
            string tooltipText = interactable.GetToolTipText();

            tooltip = WorldWayPointsController.Instance.AddwayPoint(tooltipText, hitPosition, Color.green);
        }
    }

    private void RemoveInteractableToolTip()
    {
        if (tooltip != null && WorldWayPointsController.Instance != null)
        {
            WorldWayPointsController.Instance.RemoveWaypoint(tooltip);
            tooltip = null;
        }
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
                    InteractableToolTip(hitInfo.point);
                }
                hitInteractable = true;
                return;
            }
        }

        interactable = null;
        hitInteractable = false;
        RemoveInteractableToolTip();
    }
}
