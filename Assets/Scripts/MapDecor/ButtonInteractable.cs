using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonInteractable : MonoBehaviour, IInteractable, IHover
{
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Vector2 doorEndOfTravelPoints;
    [SerializeField] private float doorSpeed;

    private float doorTarget;

    public Action OnSuccessfulActivation;
    private void Awake()
    {
        doorTarget = doorEndOfTravelPoints.x;
        doorPivot.localEulerAngles = new Vector3(0, doorTarget, 0);
        StartCoroutine(DoorPivot());
    }

    public string GetToolTipText()
    {
        if(Inventory.Instance.CurHeldItem == Item.Torch)
        {
            if(InputManager.GamePadPresent)
            {
                return "Use Torch with A";
            }
            else
            {
                return "Use Torch with Left Click";
            }
        }
        else
        {
            return "Select Torch to Use";
        }
    }

    public void Interact()
    {
        if (Inventory.Instance.CurHeldItem == Item.Torch)
        {
            OnSuccessfulActivation?.Invoke();
        }
    }

    public bool RequiresPickaxe()
    {
        return false;
    }

    public void HoverOn()
    {
        if(doorTarget == doorEndOfTravelPoints.x)
        {
            doorTarget = doorEndOfTravelPoints.y;
        }
    }

    public void HoverOff()
    {
        if (doorTarget == doorEndOfTravelPoints.y)
        {
            doorTarget = doorEndOfTravelPoints.x;
        }
    }

    private IEnumerator DoorPivot()
    {
        while (true)
        {
            while(doorPivot.localEulerAngles.y != doorTarget)
            {
                float move = Mathf.MoveTowards(doorPivot.localEulerAngles.y, doorTarget, doorSpeed* Time.deltaTime);
                doorPivot.localEulerAngles = new Vector3(0, move, 0);
                yield return null;
            }
            yield return null;
        }
    }

}
