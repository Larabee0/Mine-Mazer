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
    [SerializeField] private float InteractRange;
    [SerializeField] private LayerMask npcLayer;
    [SerializeField] private Texture2D interactionIcon;
    [SerializeField] private float boxCastSize = 0.5f;
    private WorldWayPoint tooltip;

    private bool hitInteractable = false;
    public bool HitInteractable => hitInteractable;
    private bool closedToolTip = true;
    private IInteractable interactable;

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

    private void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.interactButton.OnButtonReleased += Interact;
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
        }
    }

    private void OnApplicationQuit()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.interactButton.OnButtonReleased -= Interact;
        }
    }
    private void Interact()
    {
        if(interactable== null && Inventory.Instance.CurHeldAsset != null)
        {
            Inventory.Instance.CurHeldAsset.PlaceItem();
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

    private bool InteractCast(RaycastHit hitInfo)
    {
        if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactable))
        {
            if (interactable != this.interactable)
            {
                this.interactable = interactable;
            }
            InteractableToolTip();
            closedToolTip = false;
            hitInteractable = true;
            return true;
        }
        else if (hitInfo.collider.gameObject.GetComponentInParent<IInteractable>() != null)
        {
            interactable = hitInfo.collider.gameObject.GetComponentInParent<IInteractable>();
            if (interactable != this.interactable)
            {
                this.interactable = interactable;
            }
            InteractableToolTip();
            closedToolTip = false;
            hitInteractable = true;
            return true;
        }
        return false;
    }
}
