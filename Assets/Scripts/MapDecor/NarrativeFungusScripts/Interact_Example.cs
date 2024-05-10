using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;
using MazeGame.Input;
using MazeGame.Navigation;
using UnityEngine.UIElements;

public class Interact_Example : MonoBehaviour, IInteractable
{
    public Flowchart Dialogue;
    public bool interacted = false;
    public bool showInteractionBubbleOnStart = false;
    public int interactionIconIndex = 4;
    private WorldWayPoint interactionBubble;
    protected virtual void Start()
    {
        Dialogue = GetComponentInChildren<Flowchart>();

        if (showInteractionBubbleOnStart)
        {
            TryShowInteractBubble();
        }
    }

    protected virtual void OnEnable()
    {
        if (interactionBubble != null)
        {
            interactionBubble.wayPointRoot.style.display = DisplayStyle.Flex;
        }
    }

    protected virtual void OnDisable()
    {
        if (interactionBubble != null)
        {
            interactionBubble.wayPointRoot.style.display =  DisplayStyle.None;
        }
    }

    public virtual void TryShowInteractBubble()
    {
        if (WorldWayPointsController.Instance != null)
        {
            interactionBubble = WorldWayPointsController.Instance.AddwayPoint(Dialogue.GetComponentInChildren<Character>().NameText, transform, Color.white, interactionIconIndex);
            interactionBubble.wayPointRoot.style.display = gameObject.activeInHierarchy ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public virtual void TryHideInteractBubble()
    {
        if(interactionBubble != null)
        {
            if(WorldWayPointsController.Instance != null)
            {
                WorldWayPointsController.Instance.RemoveWaypoint(interactionBubble);
            }
            
            interactionBubble = null;
        }
    }

    public void UnlockPointer()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.UnlockPointer();
        }
    }

    public void LockPointer()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.LockPointer();
        }
    }

    public virtual void Interact()
    {
        Dialogue.ExecuteBlock("NPC Interact");
        interacted = true;
    }

    public virtual string GetToolTipText()
    {
        if (InputManager.GamePadPresent)
        {
            return string.Format("A to Interact with {0}", Dialogue.GetComponent<Character>().NameText);
        }
        else
        {
            return string.Format("Click to Interact with {0}",Dialogue.GetComponent<Character>().NameText);
        }
    }

    public virtual bool RequiresPickaxe()
    {
        return false;
    }
}
