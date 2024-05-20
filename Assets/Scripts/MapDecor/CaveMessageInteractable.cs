using MazeGame.Input;
using System;
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

    public Action onNoteClose;
    public bool Read => read;

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
            return "LT to Read";
        }
        else
        {
            return "Right Click to Read";
        }
    }

    public void Interact()
    {
        if (interactable)
        {
            if (read)
            {
                CaveMessageController.Instance.ShowMessage(cachedMessage);
                CaveMessageController.Instance.onMessageClosed += OnMessageClose;
                return;
            }
            if (string.IsNullOrEmpty(messageAssetName) || string.IsNullOrWhiteSpace(messageAssetName))
            {
                read = CaveMessageController.Instance.ShowRandomMessage(out cachedMessage);
                CaveMessageController.Instance.onMessageClosed += OnMessageClose;
            }
            else
            {
                CaveMessageController.Instance.TryShowMessageByName(messageAssetName);
                CaveMessageController.Instance.onMessageClosed += OnMessageClose;
            }
        }
    }

    private void OnMessageClose()
    {
        onNoteClose?.Invoke();
        CaveMessageController.Instance.onMessageClosed -= OnMessageClose;
    }

    public bool RequiresPickaxe()
    {
        return false;
    }
}
