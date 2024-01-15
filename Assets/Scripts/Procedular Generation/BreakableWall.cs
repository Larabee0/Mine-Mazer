using Fungus;
using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableWall : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody[] bodies;
    [SerializeField] private Vector2 rockLingerTimeRange;
    public Connector connector;
    
    private void BreakWall()
    {
        transform.DetachChildren();
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = false;
            Destroy(bodies[i].gameObject, Random.Range(rockLingerTimeRange.x, rockLingerTimeRange.y));
        }
        Destroy(gameObject);
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
                true => "B",
                false => "E"
            };
            return string.Format("{0} to Unblock", control);
        }
        else
        {
            return "Blocked Tunnel, Unblock with Pickaxe";
        }

        
    }
}
