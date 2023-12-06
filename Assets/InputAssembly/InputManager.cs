using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace MazeGame.Input
{
    public delegate void OverUIChanged(bool newValue);
    public delegate void Vector2Axis(Vector2 axis);
    public delegate void Pluse();

    public class ButtonEventContainer
    {
        public Pluse OnButtonPressed;
        public Pluse OnButtonReleased;
        public Pluse OnButtonHeld;
        public bool buttonDown = false;

    }

    /// <summary>
    /// Class to handle multi platform input.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;

        // Input Action Class and Accessors.
        private PlayerControls playerControls;
        public PlayerControls MainControls => playerControls;
        public PlayerControls.PlayerActions PlayerActions => playerControls.Player;

        // Over UI stuff, currently unused.
        private InputSystemUIInputModule eventSystemInput;
        public bool overUI = false;
        public OverUIChanged OnOverUIChanged;

        public InputSystemUIInputModule EventSystemInput => eventSystemInput;

        // Movement axis members.
        private Coroutine moveProcess;
        private bool lookActive = false;
        public Vector2Axis OnMoveAxis;
        public bool LookActive => lookActive;
        public Vector2 MoveAxis => PlayerActions.Move.ReadValue<Vector2>();

        // Look delta members.
        private Coroutine lookProcess;
        private bool moveActive = false;
        public Vector2Axis OnLookDelta;
        public bool MoveActive => moveActive;
        public Vector2 LookDelta => PlayerActions.Look.ReadValue<Vector2>();

        public ButtonEventContainer northButton = new();
        private Coroutine northButtonProcess;

        public ButtonEventContainer southButton = new();
        private Coroutine southButtonProcess;

        private void Awake()
        {
            // if no static instance, set it to this, otherwise destroy ourselves.
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
            // get event system input module
            eventSystemInput = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            // create new action class and enable the player action map
            playerControls = new PlayerControls();
            playerControls.Player.Enable();
            LockPointer();
            // bind internal controls to the action map.
            Bind();
        }

        // clean up on for when class is destroyed or application quits
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            StopAllCoroutines();
            UnBind();
        }

        // bind internal controls for coroutine execution
        private void Bind()
        {
            if (playerControls != null)
            {
                PlayerActions.Look.started += LookStarted;
                PlayerActions.Look.canceled += LookStopped;
                PlayerActions.Move.started += MoveStarted;
                PlayerActions.Move.canceled += MoveStopped;
                PlayerActions.North.started += OnNorthStart;
                PlayerActions.North.canceled += OnNorthStop;
                PlayerActions.South.started += OnSouthStart;
                PlayerActions.South.canceled += OnSouthStop;

                PlayerActions.Reload.canceled += ReloadScene;
                northButton.OnButtonReleased += Quit;
            }
        }

        // unbind internal controls
        private void UnBind()
        {
            if (playerControls != null)
            {
                PlayerActions.Look.started -= LookStarted;
                PlayerActions.Look.canceled -= LookStopped;
                PlayerActions.Move.started -= MoveStarted;
                PlayerActions.Move.canceled -= MoveStopped;
                PlayerActions.North.started -= OnNorthStart;
                PlayerActions.North.canceled -= OnNorthStop;
                PlayerActions.South.started -= OnSouthStart;
                PlayerActions.South.canceled -= OnSouthStop;

                PlayerActions.Reload.canceled -= ReloadScene;
                northButton.OnButtonReleased -= Quit;
            }
        }

        private void ReloadScene(InputAction.CallbackContext context)
        {
            SceneManager.LoadScene(0);
        }

        private void Quit()
        {
            Application.Quit();
        }

        public void UnlockPointer()
        {
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void LockPointer()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        // look is recieved as a position delta. exactly how Input.GetAxis("Mouse X/Y") works.
        // it has been restructed to run in coroutines which run everyframe the control is "active" - aka delta != Vector2.Zero
        // this avoids running any code in Update when the user is not inputting anything improving efficiency.
        #region Look
        private void LookStarted(InputAction.CallbackContext context)
        {
            if (lookProcess != null)
            {
                StopCoroutine(lookProcess);
            }

            lookProcess = StartCoroutine(LookUpdate());
            lookActive = true;
        }

        private void LookStopped(InputAction.CallbackContext context)
        {
            if (lookProcess != null)
            {
                StopCoroutine(lookProcess);
                OnLookDelta?.Invoke(Vector2.zero);
            }
            lookActive = false;
        }

        private IEnumerator LookUpdate()
        {
            while (true)
            {
                OnLookDelta?.Invoke(LookDelta);
                yield return null;
            }
        }
        #endregion

        // Move is recieved as an axis (ranging -1 to 1 with 0 being no input. exaclty how Input.GetAxis("Horizontal/Vertical") works
        // it has been restructed to run in coroutines which run everyframe the control is "active" - aka axis != Vector2.Zero.
        // this avoids running any code in Update when the user is not inputting anything improving efficiency.
        #region Move
        private void MoveStarted(InputAction.CallbackContext context)
        {
            if (moveProcess != null)
            {
                StopCoroutine(moveProcess);
            }

            moveProcess = StartCoroutine(MoveUpdate());
            moveActive = true;
        }

        private void MoveStopped(InputAction.CallbackContext context)
        {
            if (moveProcess != null)
            {
                StopCoroutine(moveProcess);
                OnMoveAxis?.Invoke(Vector2.zero);
            }
            moveActive = false;
        }


        private IEnumerator MoveUpdate()
        {
            while (true)
            {
                OnMoveAxis?.Invoke(MoveAxis);
                yield return null;
            }
        }
        #endregion

        #region NorthButton

        private void OnNorthStart(InputAction.CallbackContext context)
        {
            northButton.buttonDown = true;
            northButton.OnButtonPressed?.Invoke();
            if (northButtonProcess != null)
            {
                StopCoroutine(northButtonProcess);
            }
            northButtonProcess = StartCoroutine(OnNorthHeld());
        }

        private void OnNorthStop(InputAction.CallbackContext context)
        {
            if (northButtonProcess != null)
            {
                StopCoroutine(northButtonProcess);
                northButtonProcess = null;
            }
            northButton.buttonDown = false;
            northButton.OnButtonReleased?.Invoke();
        }

        private IEnumerator OnNorthHeld()
        {
            while (true)
            {
                northButton.OnButtonHeld?.Invoke();
                yield return null;
            }
        }

        #endregion

        #region SouthButton

        private void OnSouthStart(InputAction.CallbackContext context)
        {
            southButton.buttonDown = true;
            southButton.OnButtonPressed?.Invoke();
            if (southButtonProcess != null)
            {
                StopCoroutine(southButtonProcess);
            }
            southButtonProcess = StartCoroutine(OnSouthHeld());
        }

        private void OnSouthStop(InputAction.CallbackContext context)
        {
            if (southButtonProcess != null)
            {
                StopCoroutine(southButtonProcess);
                southButtonProcess = null;
            }
            southButton.buttonDown = false;
            southButton.OnButtonReleased?.Invoke();
        }

        private IEnumerator OnSouthHeld()
        {
            while (true)
            {
                southButton.OnButtonHeld?.Invoke();
                yield return null;
            }
        }

        #endregion
    }
}