using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableWall : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody[] bodies;
    [SerializeField] private Vector2 rockLingerTimeRange;
    [SerializeField] private Vector3 offsetAngleOnAwake = new(0, 90, 0);

    private void Awake()
    {
        transform.localEulerAngles = offsetAngleOnAwake;
    }

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

    private void OnGUI()
    {
        if(GUI.Button(new Rect(10, 10, 50, 50), "Destroy Wall"))
        {
            BreakWall();
        }
    }

    public void Interact()
    {
        BreakWall();
    }
}
