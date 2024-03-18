using Fungus;
using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableWall : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody[] bodies;
    [SerializeField] protected MapResource[] droppableItems;
    [SerializeField] private Transform droppableSpawnArea;
    [SerializeField] private Vector2Int minMaxDrop;
    [SerializeField] private Vector2 rockLingerTimeRange;
    public Connector connector;

    public Pluse OnWallBreak;

    private void BreakWall()
    {
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
            GameObject Instance = Instantiate(chosenItem, droppableSpawnArea.position,Quaternion.identity).gameObject;
            Instance.AddComponent<Rigidbody>();
            Instance.AddComponent<ItemDrop>();
        }
    }

    public void Interact()
    {
        if (PlayerCanBreak())
        {
            Inventory.Instance.CurHeldAsset.InventoryInteract();
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
}
