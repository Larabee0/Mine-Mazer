using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
namespace MazeGame.Input
{
    public delegate void BoolPluse(bool newValue);
    public delegate void Vector2Axis(Vector2 axis);
    public delegate void Pluse();
    public delegate void IntAxis(int axis);

    [Serializable]
    public class ButtonEventContainer
    {
        public Pluse OnButtonPressed;
        public Pluse OnButtonReleased;
        public Pluse OnButtonHeld;
        public bool ButtonDown => buttonDown;
        public bool Bound => bound;

        [SerializeField] private bool buttonDown = false;
        private Coroutine buttonProcess;
        private readonly InputManager inputManager;
        private readonly InputAction action;
        private bool bound = false;

        public ButtonEventContainer(InputManager inputManager, InputAction action)
        {
            this.inputManager = inputManager;
            this.action = action;
        }

        internal void Bind()
        {
            if(bound) { return; }
            bound = true;
            action.started += OnButtonStart;
            action.canceled += OnButtonStop;
        }

        internal void Unbind()
        {
            if (!bound) { return; }
            bound = false;
            action.started -= OnButtonStart;
            action.canceled -= OnButtonStop;
        }

        private void OnButtonStart(InputAction.CallbackContext context)
        {
            buttonDown = true;
            OnButtonPressed?.Invoke();
            if (buttonProcess != null)
            {
                inputManager.StopCoroutine(buttonProcess);
            }
            buttonProcess = inputManager.StartCoroutine(OnHeld());
        }

        private void OnButtonStop(InputAction.CallbackContext context)
        {
            if (buttonProcess != null)
            {
                inputManager.StopCoroutine(buttonProcess);
                buttonProcess = null;
            }
            buttonDown = false;
            OnButtonReleased?.Invoke();
        }

        private IEnumerator OnHeld()
        {
            while (true)
            {
                OnButtonHeld?.Invoke();
                yield return null;
            }
        }
    }

    /// <summary>
    /// Class to handle multi platform input.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        private static InputManager instance;
        public static InputManager Instance
        {
            get
            {
                if(instance == null)
                {
                    Debug.LogWarning("Expected Input Manager instance not found. Order of operations issue? Or Input Manager is disabled/missing.");
                }
                return instance;
            }
            private set
            {
                if (value != null && instance == null)
                {
                    instance = value;
                }
            }
        }

        // Input Action Class and Accessors.
        private PlayerControls playerControls;
        public PlayerControls MainControls => playerControls;
        public PlayerControls.PlayerActions PlayerActions => playerControls.Player;
        public PlayerControls.DialogueControlsActions DialogueActions => playerControls.DialogueControls;

        // Over UI stuff, currently unused.
        private InputSystemUIInputModule eventSystemInput;
        public bool overUI = false;
        public BoolPluse OnOverUIChanged;

        public InputSystemUIInputModule EventSystemInput => eventSystemInput;

        public static bool GamePadPresent => Gamepad.current != null;

        // Movement axis members.
        private Coroutine moveProcess;
        private bool lookActive = false;
        public Vector2Axis OnMoveAxis;
        public bool LookActive => lookActive;
        public Vector2 MoveAxis => PlayerActions.Move.ReadValue<Vector2>();

        // scroll axis;
        public IntAxis scrollDirection;

        // Look delta members.
        private Coroutine lookProcess;
        private bool moveActive = false;
        public Vector2Axis OnLookDelta;
        public bool MoveActive => moveActive;
        public Vector2 LookDelta => PlayerActions.Look.ReadValue<Vector2>();

        public ButtonEventContainer northButton;
        
        public ButtonEventContainer southButton;

        public ButtonEventContainer interactButton;

        public ButtonEventContainer advanceDialogueButton;

        public ButtonEventContainer pauseButton;

        private void Awake()
        {
            // if no static instance, set it to this, otherwise destroy ourselves.
            if (instance == null)
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
            Build();

            LockPointer();
            // bind internal controls to the action map.
            Bind();

            // Debug.LogFormat("Player Input Enabled: {0}", playerControls.Player.enabled);
            // Debug.LogFormat("UI Input Enabled: {0}", playerControls.UI.enabled);
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

        private void Build()
        {
            northButton = new(this, PlayerActions.North);

            southButton = new(this, PlayerActions.South);

            interactButton = new(this, PlayerActions.Interact);

            advanceDialogueButton = new(this, DialogueActions.AdvanceDialogue);

            pauseButton = new(this, PlayerActions.Pause);

           // navigationSubmitButton = new(this, eventSystemInput.submit.action);
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

                playerControls.Player.ItemScroll.performed += ScrollInvoke;

                northButton.Bind();
                southButton.Bind();
                interactButton.Bind();
                advanceDialogueButton.Bind();
                pauseButton.Bind();

                PlayerActions.Reload.canceled += ReloadScene;
                //northButton.OnButtonReleased += Quit;
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

                playerControls.Player.ItemScroll.performed -= ScrollInvoke;

                northButton.Unbind();
                southButton.Unbind();
                interactButton.Unbind();
                advanceDialogueButton.Unbind();
                pauseButton.Unbind();

                PlayerActions.Reload.canceled -= ReloadScene;
                //northButton.OnButtonReleased -= Quit;
            }
        }

        private void ReloadScene(InputAction.CallbackContext context)
        {
            SceneManager.LoadScene(0);
        }

        public void UnlockPointer()
        {
            Cursor.lockState = CursorLockMode.Confined;
            PlayerActions.Disable();
            DialogueActions.Enable();
        }

        public void LockPointer()
        {
            Cursor.lockState = CursorLockMode.Locked;
            PlayerActions.Enable();
            DialogueActions.Disable();
        }

        public void SetPointerLocked(bool locked)
        {
            if(locked)
            {
                LockPointer();
            }
            else
            {
                UnlockPointer();
            }
        }

        public void SetUIToolkitFocus()
        {
            EventSystem.current.SetSelectedGameObject(FindObjectOfType<PanelEventHandler>().gameObject);
        }

        private void ScrollInvoke(InputAction.CallbackContext context)
        {
            int value = (int)context.ReadValue<float>();
            scrollDirection?.Invoke(value);
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

    }
}