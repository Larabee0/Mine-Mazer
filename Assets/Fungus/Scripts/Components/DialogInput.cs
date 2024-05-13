// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using MazeGame.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fungus
{
    /// <summary>
    /// Supported modes for clicking through a Say Dialog.
    /// </summary>
    public enum ClickMode
    {
        /// <summary> Clicking disabled. </summary>
        Disabled,
        /// <summary> Click anywhere on screen to advance. </summary>
        ClickAnywhere,
        /// <summary> Click anywhere on Say Dialog to advance. </summary>
        ClickOnDialog,
        /// <summary> Click on continue button to advance. </summary>
        ClickOnButton
    }

    /// <summary>
    /// Input handler for say dialogs.
    /// </summary>
    public class DialogInput : MonoBehaviour
    {
        [Tooltip("Click to advance story")]
        [SerializeField] protected ClickMode clickMode;

        [Tooltip("Delay between consecutive clicks. Useful to prevent accidentally clicking through story.")]
        [SerializeField] protected float nextClickDelay = 0f;

        [Tooltip("Allow holding Cancel to fast forward text")]
        [SerializeField] protected bool cancelEnabled = true;

        [Tooltip("Ignore input if a Menu dialog is currently active")]
        [SerializeField] protected bool ignoreMenuClicks = true;

        protected bool dialogClickedFlag;

        protected bool nextLineInputFlag;

        protected float ignoreClickTimer;

        protected StandaloneInputModule currentStandaloneInputModule;
        protected bool newInputSystem = false;

        protected Writer writer;

        protected virtual void Awake()
        {
            writer = GetComponent<Writer>();
        }

        private void OnEnable()
        {
            CheckEventSystem();
        }

        private void OnDisable()
        {
            if (newInputSystem && clickMode == ClickMode.ClickAnywhere)
            {
                InputManager.Instance.advanceDialogueButton.OnButtonPressed -= SetNextLineFlagNewInputSystem;
                InputManager.Instance.advanceDialogueButton.OnButtonPressed -= SetClickAnywhereClickedFlag;
            }
        }

        // There must be an Event System in the scene for Say and Menu input to work.
        // This method will automatically instantiate one if none exists.
        protected virtual void CheckEventSystem()
        {
            if(EventSystem.current == null)
            {
                Debug.LogError("No event system present!");
                enabled = false;
                return;
            }

            currentStandaloneInputModule = EventSystem.current.GetComponent<StandaloneInputModule>();
            if(currentStandaloneInputModule == null && InputManager.Instance != null && InputManager.Instance.EventSystemInput != null)
            {
                newInputSystem = true;
                if(clickMode == ClickMode.ClickAnywhere)
                {
                    InputManager.Instance.advanceDialogueButton.OnButtonPressed += SetNextLineFlagNewInputSystem;
                    InputManager.Instance.advanceDialogueButton.OnButtonPressed += SetClickAnywhereClickedFlag;
                }
            }
        }
        
        private void SetNextLineFlagNewInputSystem()
        {
            bool buttonSelected = EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent<UnityEngine.UI.Button>(out _);
            if (!buttonSelected)
            {
                SetNextLineFlag();
            }
        }

        protected virtual void Update()
        {
            if (EventSystem.current == null)
            {
                return;
            }

            if (writer != null)
            {
                if (!newInputSystem && (Input.GetButtonDown(currentStandaloneInputModule.submitButton)
                    || (cancelEnabled && Input.GetButton(currentStandaloneInputModule.cancelButton))))
                {
                    SetNextLineFlag();
                }
                //else
                //{
                //    bool buttonSelected = EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent<UnityEngine.UI.Button>(out _);
                //    bool submitPressed = InputManager.Instance.EventSystemInput.submit.action.ReadValue<float>() != 0;
                //    bool cancelEnabledAndPressed = cancelEnabled && InputManager.Instance.EventSystemInput.cancel.action.ReadValue<float>() != 0;
                //    if (!buttonSelected && (submitPressed || cancelEnabledAndPressed))
                //    {
                //        SetNextLineFlag();
                //    }
                //}
            }

            switch (clickMode)
            {
                case ClickMode.Disabled:
                    break;
                case ClickMode.ClickAnywhere:
                    if (!newInputSystem&& Input.GetMouseButtonDown(0))
                    {
                        SetClickAnywhereClickedFlag();
                    }
                    break;
                case ClickMode.ClickOnDialog:
                    if (dialogClickedFlag)
                    {
                        SetNextLineFlag();
                        dialogClickedFlag = false;
                    }
                    break;
            }

            if (ignoreClickTimer > 0f)
            {
                ignoreClickTimer = Mathf.Max (ignoreClickTimer - Time.deltaTime, 0f);
            }

            if (ignoreMenuClicks)
            {
                // Ignore input events if a Menu is being displayed
                if (MenuDialog.ActiveMenuDialog != null && 
                    MenuDialog.ActiveMenuDialog.IsActive() &&
                    MenuDialog.ActiveMenuDialog.DisplayedOptionsCount > 0)
                {
                    dialogClickedFlag = false;
                    nextLineInputFlag = false;
                }
            }

            // Tell any listeners to move to the next line
            if (nextLineInputFlag)
            {
                var inputListeners = gameObject.GetComponentsInChildren<IDialogInputListener>();
                for (int i = 0; i < inputListeners.Length; i++)
                {
                    var inputListener = inputListeners[i];
                    inputListener.OnNextLineEvent();
                }
                nextLineInputFlag = false;
            }
        }

        #region Public members

        /// <summary>
        /// Trigger next line input event from script.
        /// </summary>
        public virtual void SetNextLineFlag()
        {
            if(writer.IsWaitingForInput || writer.IsWriting)
            {
                nextLineInputFlag = true;
            }
        }
        /// <summary>
        /// Set the ClickAnywhere click flag.
        /// </summary>
        public virtual void SetClickAnywhereClickedFlag()
        {
            if (ignoreClickTimer > 0f)
            {
                return;
            }
            ignoreClickTimer = nextClickDelay;

            // Only applies if ClickedAnywhere is selected
            if (clickMode == ClickMode.ClickAnywhere)
            {
                SetNextLineFlag();
            }
        }
        /// <summary>
        /// Set the dialog clicked flag (usually from an Event Trigger component in the dialog UI).
        /// </summary>
        public virtual void SetDialogClickedFlag()
        {
            // Ignore repeat clicks for a short time to prevent accidentally clicking through the character dialogue
            if (ignoreClickTimer > 0f)
            {
                return;
            }
            ignoreClickTimer = nextClickDelay;

            // Only applies in Click On Dialog mode
            if (clickMode == ClickMode.ClickOnDialog)
            {
                dialogClickedFlag = true;
            }
        }

        /// <summary>
        /// Sets the button clicked flag.
        /// </summary>
        public virtual void SetButtonClickedFlag()
        {
            // Only applies if clicking is not disabled
            if (clickMode != ClickMode.Disabled)
            {
                SetNextLineFlag();
            }
        }

        #endregion
    }
}
