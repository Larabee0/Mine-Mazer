using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
//VOIDLYNX. 2022. 'Unity Script: FPS Player Camera'. Voidlynx March 2022. Available at: https://www.voidlynx.com/2022/03/unity-script-fps-player-camera.html [accessed 24 October 2023].
public class Camera_Movement : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    private float sensitivityMultiplier = 1f;
    private bool invertH;
    private bool invertV;
    public Transform playerBody;
    float xRotation = 0f;


    void Start()
    {
        if(PlayerSettings.Instance != null)
        {
            PlayerSettings.Instance.OnSettingsChanged += UpdateUserSettings;
            UpdateUserSettings();
        }
        /// new input system additions, will use old input system if there is no <see cref="InputManager"/> instance
        if(InputManager.Instance != null)
        {
            enabled = false; // disable update, will recieve input events directly.
            InputManager.Instance.lookAxis.OnAxis += OnLookEvent; // subscribe to Look Event from new input system
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
        if (PlayerSettings.Instance != null)
        {
            PlayerSettings.Instance.OnSettingsChanged -= UpdateUserSettings;
        }
        if (InputManager.Instance != null)
        {
            InputManager.Instance.lookAxis.OnAxis -= OnLookEvent; // cleanup event binding by unsubscribing
        }
    }

    private void UpdateUserSettings()
    {
        if (PlayerSettings.Instance == null)
        {
            return;
        }

        sensitivityMultiplier = PlayerSettings.Instance.userSettings.cameraSens;
        invertH = PlayerSettings.Instance.userSettings.cameraHInvert;
        invertV = PlayerSettings.Instance.userSettings.cameraVInvert;
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
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }
        deltaX *= sensitivityMultiplier *mouseSensitivity * Time.deltaTime;
        deltaY *= sensitivityMultiplier * mouseSensitivity * Time.deltaTime;

        deltaX = invertH ? -deltaX : deltaX;
        deltaY = invertV ? -deltaY : deltaY;

        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * deltaX);
    }
}
