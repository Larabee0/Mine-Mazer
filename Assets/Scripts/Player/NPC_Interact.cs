using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeGame.Input;
using MazeGame.Navigation;
using System;
using System.Runtime.InteropServices;

//RYTECH. 2023. How to Make a Flexible Interaction System in 2 Minutes [C#] [Unity3D]. Available at: https://www.youtube.com/watch?v=K06lVKiY-sY [accessed 28 November 2023].
interface IInteractable
{
    public void Interact();
    public bool RequiresPickaxe();
    public string GetToolTipText();

}

interface IHover
{
    public void HoverOn();
    public void HoverOff();
}

public class NPC_Interact : MonoBehaviour
{
    private static NPC_Interact instance;
    public static NPC_Interact Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected NPC_Interact instance not found. Order of operations issue? Or NPC_Interact is disabled/missing.");
            }
            return instance;
        }
        private set
        {
            if (value != null && instance == null)
            {
                instance = value;
            }
        }
    }

    [SerializeField] private Transform InteractorSource;
    public float InteractRange;
    [SerializeField] private LayerMask npcLayer;
    [SerializeField] private Texture2D interactionIcon;
    [SerializeField] private float boxCastSize = 0.5f;
    private WorldWayPoint tooltip;

    private bool hitInteractable = false;
    public bool HitInteractable => hitInteractable;
    private bool closedToolTip = true;
    private IInteractable interactable;
    private IHover hoverable;

    private RaycastHit hitInfo;
    public RaycastHit InteractInfo => hitInfo;

    private void Awake()
    {
        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.interactButton.OnButtonReleased += Interact;
            InputManager.Instance.mineButton.OnButtonReleased += InteractMine;
            InputManager.Instance.placeItemButton.OnButtonReleased += InteractPlace;
        }
    }

    private void Update()
    {
        if (!Physics.BoxCast(InteractorSource.position, transform.localScale * boxCastSize, InteractorSource.forward, out hitInfo, InteractorSource.rotation, InteractRange, npcLayer)
            || !InteractCast(hitInfo))
        {
            interactable = null;
            hitInteractable = false;
            RemoveInteractableToolTip();
            if(hoverable != null)
            {
                hoverable.HoverOff();
                hoverable = null;
            }
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.interactButton.OnButtonReleased -= Interact;
            InputManager.Instance.mineButton.OnButtonReleased -= InteractMine;
            InputManager.Instance.placeItemButton.OnButtonReleased -= InteractPlace;
        }
    }

    private void InteractPlace()
    {
        if (interactable == null && Inventory.Instance.CurHeldAsset != null)
        {
            Inventory.Instance.CurHeldAsset.PlaceItem();
        }
    }

    private void InteractMine()
    {
        if (interactable != null && interactable.RequiresPickaxe() && Inventory.Instance.CurHeldItem == Item.Pickaxe)
        {
            Inventory.Instance.CurHeldAsset.InventoryInteract();
            interactable?.Interact();
        }
    }

    private void Interact()
    {
        if (interactable != null && interactable.RequiresPickaxe())
        {
            return;
        }
        else if(interactable == null && Inventory.Instance.CurHeldAsset != null)
        {
            return;
            
        }
        else
        {
            interactable?.Interact();
        }
    }

    private void InteractableToolTip()
    {
        if (interactable != null && InteractMessage.Instance != null)
        {
            string tooltipText = interactable.GetToolTipText();
            if (!string.IsNullOrEmpty(tooltipText) && !string.IsNullOrWhiteSpace(tooltipText))
            {
                if(interactable.GetType().IsSubclassOf(typeof(Interact_Example)))
                {
                    InteractMessage.Instance.ShowInteraction(tooltipText, 1, Color.white);
                }
                else
                {
                    InteractMessage.Instance.ShowInteraction(tooltipText, 2, Color.white);
                }
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

    private bool InteractCast(RaycastHit hitInfo)
    {
        if (hitInfo.collider.gameObject.TryGetComponent(out IHover hoverable))
        {
            hoverable.HoverOn();
        }
        else if (hitInfo.collider.gameObject.GetComponentInParent<IHover>() != null)
        {
            hoverable = hitInfo.collider.gameObject.GetComponentInParent<IHover>();
            hoverable.HoverOn();
        }

        if(hoverable != null && this.hoverable != null && hoverable != this.hoverable)
        {
            this.hoverable.HoverOff();
            this.hoverable = hoverable;
        }
        else if( hoverable != null &&  this.hoverable == null)
        {
            this.hoverable = hoverable;
        }

        if(hoverable == null && this.hoverable != null)
        {
            this.hoverable.HoverOff();
            this.hoverable = null;
        }

        if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactable))
        {
            AttemptInteraction(interactable);
            return true;
        }
        else if (hitInfo.collider.gameObject.GetComponentInParent<IInteractable>() != null)
        {
            interactable = hitInfo.collider.gameObject.GetComponentInParent<IInteractable>();
            AttemptInteraction(interactable);
            return true;
        }
        return false;
    }

    private void AttemptInteraction(IInteractable interactable)
    {
        if (interactable != this.interactable)
        {
            this.interactable = interactable;
        }
        InteractableToolTip();
        closedToolTip = false;
        hitInteractable = true;
    }
}
