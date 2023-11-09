using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidCharacterController : MonoBehaviour
{
    private Vector3 playerMoveInput;
    private Vector2 playerMouseInput;

    private float xRot;

    [SerializeField] private Transform playerCamera;
    [SerializeField] private Rigidbody playerBody;
    [Space(10)]
    [SerializeField] private float speed;
    [SerializeField] private float sensitivity;
    [SerializeField] private float minCameraDeg;
    [SerializeField] private float maxCameraDeg;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        playerMoveInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        playerMouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));


        MovePlayerCamera();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        Vector3 moveVector = transform.TransformDirection(playerMoveInput) * speed;
        playerBody.velocity = new Vector3(moveVector.x, playerBody.velocity.y, moveVector.z);
    }

    private void MovePlayerCamera()
    {
        xRot -= playerMouseInput.y * sensitivity;
        xRot = Mathf.Clamp(xRot, minCameraDeg, maxCameraDeg);
        transform.Rotate(0f, playerMouseInput.x * sensitivity, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(xRot, 0, 0);
    }
}
