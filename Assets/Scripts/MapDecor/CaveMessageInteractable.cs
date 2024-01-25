using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveMessageInteractable : MonoBehaviour, IInteractable
{
    [SerializeField, Tooltip("If left blank, will be random")] protected string messageAssetName = "";
    [SerializeField] private bool interactable = true;
    [SerializeField] private Collider interactableCollider;

    private bool read = false;
    private Hash128 cachedMessage;

    private void Awake()
    {
        if(interactableCollider != null)
        {
            interactableCollider.enabled = interactableCollider;
        }
        else
        {
            Debug.LogError("Note collider not assigned!", gameObject);
        }
        read = false;
    }

    public string GetToolTipText()
    {
        if(!interactable) return string.Empty;
        if (InputManager.GamePadPresent)
        {
            return "B to Read";
        }
        else
        {
            return "E to Read";
        }
    }

    public void Interact()
    {
        if (interactable)
        {
            if (read)
            {
                CaveMessageController.Instance.ShowMessage(cachedMessage);
                return;
            }
            if (string.IsNullOrEmpty(messageAssetName) || string.IsNullOrWhiteSpace(messageAssetName))
            {
                read = CaveMessageController.Instance.ShowRandomMessage(out cachedMessage);
            }
            else
            {
                CaveMessageController.Instance.TryShowMessageByName(messageAssetName);
            }
        }
    }
}
