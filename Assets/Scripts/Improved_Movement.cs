using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//VOIDLYNX. 2022. 'Unity Script: FPS Player Movement'. Voidlynx March 2022. Available at: https://www.voidlynx.com/2022/03/unity-script-fps-player-movement.html [accessed 24 October 2023].
public class Improved_Movement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;



    private Vector3 velocity;
    private bool isGrounded;
    private bool useUpdateMove = true;

    private void Start()
    {
        /// new input system additions, will use old input system if there is no <see cref="InputManager"/> instance
        if (InputManager.Instance != null)
        {
            useUpdateMove = false; // disable Old input system checks in update, will recieve input events directly.
            InputManager.Instance.OnMoveAxis += OnMoveEvent; // subscribe to Move Event from new input system
            InputManager.Instance.southButton.OnButtonReleased += OnJump; // subscribe to South Button Up from new input system
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        { 
            // cleanup events binding by unsubscribing
            InputManager.Instance.OnMoveAxis -= OnMoveEvent;
            InputManager.Instance.southButton.OnButtonReleased -= OnJump;
        }
    }

    private void OnJump()
    {
        if (isGrounded && enabled)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void OnMoveEvent(Vector2 axis)
    {
        if (enabled)
        {
            Move(axis.x, axis.y);
        }        
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (useUpdateMove)
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Move(x, z);
            if (Input.GetButtonDown("Jump"))
            {
                OnJump();
            }
        }

        velocity.y += gravity * Time.deltaTime;
        
        controller.Move(velocity * Time.deltaTime);
    }

    private void Move(float x, float z)
    {
        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(speed * Time.deltaTime * move);
    }
}
