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
        BreakWall();
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
        if (InputManager.GamePadPresent)
        {
            return "B to Mine Wall";
        }
        else
        {
            return "E to Mine Wall";
        }
    }
}
