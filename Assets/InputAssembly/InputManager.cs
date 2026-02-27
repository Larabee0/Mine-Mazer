using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace MazeGame.Input
{

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
        public Action<bool> OnOverUIChanged;

        public InputSystemUIInputModule EventSystemInput => eventSystemInput;

        public static bool GamePadPresent => Gamepad.current != null;


        // scroll axis;
        public Action<int> scrollDirection;
        public Action<int> inventoryCycle;

        public AxisEventContainer moveAxis;

        public AxisEventContainer lookAxis;

        public AxisEventContainer inventoryAxis;

        public ButtonEventContainer torchButton;

        public ButtonEventContainer pickaxeButton;

        public ButtonEventContainer soupButton;

        public ButtonEventContainer southButton;

        public ButtonEventContainer interactButton;

        public ButtonEventContainer placeItemButton;

        public ButtonEventContainer mineButton;

        public ButtonEventContainer advanceDialogueButton;

        public ButtonEventContainer pauseButton;

        public ButtonEventContainer inventoryButton;

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
            // create new action class and enable the player action map
            playerControls = new PlayerControls();
            Build();
            // bind internal controls to the action map.
            Bind();
        }

        void Start()
        {
            eventSystemInput = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        }

        // clean up on for when class is destroyed or application quits
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            StopAllCoroutines();
            Unbind();
        }

        private void Build()
        {
            moveAxis = new(this,PlayerActions.Move);

            lookAxis = new(this,PlayerActions.Look);

            inventoryAxis = new(this,DialogueActions.InventorySelect);

            torchButton = new(this, PlayerActions.Torch);

            pickaxeButton = new(this, PlayerActions.Pickaxe);

            soupButton = new(this, PlayerActions.Soup);

            southButton = new(this, PlayerActions.South);

            interactButton = new(this, PlayerActions.Interact);

            placeItemButton = new(this, PlayerActions.PlaceItem);

            mineButton = new(this, PlayerActions.Mine);

            advanceDialogueButton = new(this, DialogueActions.AdvanceDialogue);

            pauseButton = new(this, PlayerActions.Pause);

            inventoryButton = new(this, PlayerActions.Inventory);

           // navigationSubmitButton = new(this, eventSystemInput.submit.action);
        }

        // bind internal controls for coroutine execution
        private void Bind()
        {
            if (playerControls != null)
            {
                moveAxis.Bind();
                lookAxis.Bind();
                inventoryAxis.Bind();

                playerControls.Player.ItemScroll.performed += ScrollInvoke;

                DialogueActions.InventoryPageCycle.performed += CycleInvoke;

                torchButton.Bind();
                pickaxeButton.Bind();
                soupButton.Bind();
                southButton.Bind();
                interactButton.Bind();
                mineButton.Bind();
                placeItemButton.Bind();
                advanceDialogueButton.Bind();
                pauseButton.Bind();
                inventoryButton.Bind();

                //northButton.OnButtonReleased += Quit;
            }
        }

        // unbind internal controls
        private void Unbind()
        {
            if (playerControls != null)
            {
                moveAxis.Unbind();
                lookAxis.Unbind();
                inventoryAxis.Unbind();

                playerControls.Player.ItemScroll.performed -= ScrollInvoke;
                DialogueActions.InventoryPageCycle.performed -= CycleInvoke;

                torchButton.Unbind();
                pickaxeButton.Unbind();
                soupButton.Unbind();
                southButton.Unbind();
                interactButton.Unbind();
                mineButton.Unbind();
                placeItemButton.Unbind();
                advanceDialogueButton.Unbind();
                pauseButton.Unbind();
                inventoryButton.Unbind();

                //northButton.OnButtonReleased -= Quit;
            }
        }

        public void UnlockPointer()
        {
            Cursor.lockState = CursorLockMode.Confined;
            PlayerActions.Disable();
            DialogueActions.Enable();
            Debug.Log("Unlock Pointer");
        }

        public void LockPointer()
        {
            Cursor.lockState = CursorLockMode.Locked;
            PlayerActions.Enable();
            DialogueActions.Disable();
            Debug.Log("Lock Pointer");
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
            PanelEventHandler[] eventHandlers = FindObjectsOfType<PanelEventHandler>();
            GameObject go = eventHandlers.FirstOrDefault(evt => evt.gameObject.name != "MiniMap").gameObject;
            EventSystem.current.SetSelectedGameObject(go);
        }

        private void ScrollInvoke(InputAction.CallbackContext context)
        {
            int value = (int)context.ReadValue<float>();
            scrollDirection?.Invoke(value);
        }

        private void CycleInvoke(InputAction.CallbackContext context)
        {
            int value = (int)context.ReadValue<float>();
            inventoryCycle?.Invoke(value);
        }
    }
}