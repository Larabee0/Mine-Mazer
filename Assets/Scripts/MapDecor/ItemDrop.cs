using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [SerializeField] private Rigidbody body;

    private float velocityCutoff = 0.5f;
    private bool velocityCheck = false;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        Invoke(nameof(AllowVelocityCheck), 3);
    }

    private void AllowVelocityCheck()
    {
        velocityCheck = true;
    }

    private void FixedUpdate()
    {
        if(body.velocity.magnitude  < velocityCutoff && velocityCheck)
        {
            Destroy(body);
            Destroy(this);
        }
    }
}
