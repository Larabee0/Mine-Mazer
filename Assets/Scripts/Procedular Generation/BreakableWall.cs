using Fungus;
using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BreakableWall : MonoBehaviour, IInteractable, IHover
{
    [SerializeField] private Rigidbody[] bodies;
    [SerializeField] private MeshRenderer[] meshRenderers;
    [SerializeField] protected MapResource[] droppableItems;
    [SerializeField] private Transform droppableSpawnArea;
    [SerializeField] private Vector2Int minMaxDrop;
    [SerializeField] private Vector2 rockLingerTimeRange;
    [SerializeField] protected Color onSelectColour = Color.yellow;
    public Connector connector;
    private bool broken = false;

    public Action OnWallBreak;
    private void Awake()
    {
        SetOutlineFader(true);
    }

    private void BreakWall()
    {
        HoverOff();
        broken = true;
        SetOutlineFader(false);
        OnWallBreak?.Invoke();
        transform.DetachChildren();
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = false;
            Destroy(bodies[i].gameObject, Random.Range(rockLingerTimeRange.x, rockLingerTimeRange.y));
        }
        DumpDroppables();
        Destroy(droppableSpawnArea.gameObject);
        Destroy(gameObject);
    }

    private void DumpDroppables()
    {
        int drop = Random.Range(minMaxDrop.x,minMaxDrop.y);
        drop = droppableItems.Length > 0 ? drop : 0;

        for (int i = 0; i < drop; i++)
        {
            MapResource chosenItem = droppableItems[Random.Range(0, droppableItems.Length)];
            MapResource Instance = Instantiate(chosenItem, droppableSpawnArea.position,Quaternion.identity,transform.parent);
            Instance.SetRequiresPickaxe(false);
            Instance.gameObject.AddComponent<Rigidbody>();
            Instance.gameObject.AddComponent<ItemDrop>();
        }
    }

    public void Interact()
    {
        if (PlayerCanBreak())
        {
            BreakWall();
        }
    }

    private bool PlayerCanBreak()
    {
        if (Inventory.Instance)
        {
            if(Inventory.Instance.CurHeldItem == Item.Pickaxe)
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = TunnelSection.GetLTWConnectorMatrix(transform.localToWorldMatrix, connector);

        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(Vector3.zero, 0.5f * Vector3.one);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward);
    }

    public string GetToolTipText()
    {
        bool canMine = PlayerCanBreak();
        

        if (canMine)
        {
            string control = InputManager.GamePadPresent switch
            {
                true => "RT",
                false => "Left Click"
            };
            return string.Format("{0} to Unblock", control);
        }
        else
        {
            return "Blocked Tunnel, Unblock with Pickaxe";
        }

        
    }

    public bool RequiresPickaxe()
    {
        return true;
    }

    public void HoverOn()
    {
        if (broken)
        {
            return;
        }
        SetOutlineColour(onSelectColour);
        SetOutlineFader(false);
    }

    public void HoverOff()
    {
        if (broken)
        {
            return;
        }
        SetOutlineColour(Color.black);
        SetOutlineFader(true);
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
        if(meshRenderers != null && meshRenderers.Length > 0)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer renderer = meshRenderers[i];
                if (renderer == null) continue;
                List<Material> materials = new();
                renderer.GetMaterials(materials);
                materials.ForEach(mat => mat.SetColor("_OutlineColour", colour));
            }

        }
    }


    public void SetOutlineFader(bool fading)
    {

        if (meshRenderers != null && meshRenderers.Length > 0)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer renderer = meshRenderers[i];
                if (renderer == null) continue;
                List<Material> materials = new();
                renderer.GetMaterials(materials);
                materials.ForEach(mat => mat.SetInt("_OutlineFading", fading ? 1 : 0));
            }
        }
    }
}
