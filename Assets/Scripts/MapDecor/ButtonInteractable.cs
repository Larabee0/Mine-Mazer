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
    [SerializeField] private Color selectColour = Color.yellow;
    [SerializeField] private AudioSource gateOpen;
    [SerializeField] private AudioSource gateClose;

    private MeshRenderer[] meshRenderers;
    public bool interactable = false;
    public bool fader = false;

    private bool hoverOn;
    private float doorTarget;

    public Action OnSuccessfulActivation;
    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        doorTarget = doorEndOfTravelPoints.x;
        doorPivot.localEulerAngles = new Vector3(0, doorTarget, 0);
        StartCoroutine(DoorPivot());
    }

    public string GetToolTipText()
    {
        if (!interactable) return "";
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
        if (interactable && Inventory.Instance.CurHeldItem == Item.Torch)
        {
            SetRainbowOpacity(0);
            SetOutlineFader(false);
            OnSuccessfulActivation?.Invoke();
        }
    }

    public bool RequiresPickaxe()
    {
        return false;
    }

    public void HoverOn()
    {
        if (interactable && !hoverOn)
        {
            if (doorTarget == doorEndOfTravelPoints.x)
            {
                doorTarget = doorEndOfTravelPoints.y;
            }
            SetOutlineColour(selectColour);
            fader = false;
            SetOutlineFader(false);
            if (!gateOpen.isPlaying) { gateOpen.Play(); }
            if (gateClose.isPlaying) { gateClose.Stop(); }
            hoverOn = true;
        }
    }

    public void HoverOff()
    {
        if (interactable && hoverOn)
        {
            if (doorTarget == doorEndOfTravelPoints.y)
            {
                doorTarget = doorEndOfTravelPoints.x;
            }
            SetOutlineColour(Color.black);
            SetOutlineFader(fader);
            if (gateOpen.isPlaying) { gateOpen.Stop(); }
            if (!gateClose.isPlaying) { gateClose.Play(); }
            hoverOn = false;
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


    public void SetRainbowOpacity(float opacity)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            List<Material> materials = new();
            renderer.GetMaterials(materials);
            materials.ForEach(mat => mat.SetFloat("_Overlay_Opacity", opacity));
        }
    }

    public void SetOutlineColour(Color colour)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            List<Material> materials = new();
            renderer.GetMaterials(materials);
            materials.ForEach(mat => mat.SetColor("_OutlineColour", colour));
        }
    }


    public void SetOutlineFader(bool fading)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            List<Material> materials = new();
            renderer.GetMaterials(materials);
            materials.ForEach(mat => mat.SetInt("_OutlineFading", fading ? 1 : 0));
        }
    }

}
