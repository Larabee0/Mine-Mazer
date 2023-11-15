using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
//VOIDLYNX. 2022. 'Unity Script: FPS Player Camera'. Voidlynx March 2022. Available at: https://www.voidlynx.com/2022/03/unity-script-fps-player-camera.html [accessed 24 October 2023].
public class Camera_Movement : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    float xRotation = 0f;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        /// new input system additions, will use old input system if there is no <see cref="InputManager"/> instance
        if(InputManager.Instance != null)
        {
            enabled = false; // disable update, will recieve input events directly.
            InputManager.Instance.OnLookDelta += OnLookEvent; // subscribe to Look Event from new input system
        }
    }

    // old Input System now calls Look method, same as "look" code as before
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Look(mouseX, mouseY);
    }


    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLookDelta -= OnLookEvent; // cleanup event binding by unsubscribing
        }
    }

    // new input system event recieves the mouse XY as an event in a vector2.
    // this is easily put into the look method like so.
    // this gets called whenever the player is moving the mouse. When they aren't, no code is run.
    private void OnLookEvent(Vector2 axis)
    {
        Look(axis.x, axis.y);
    }

    // recieves Mouse X and Y deltas and converts to camera movement
    private void Look(float deltaX, float deltaY)
    {
        deltaX *= mouseSensitivity * Time.deltaTime;
        deltaY *= mouseSensitivity * Time.deltaTime;

        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * deltaX);
    }
}
