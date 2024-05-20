using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeGame.Input
{
    [Serializable]
    public class ButtonEventContainer
    {
        public Action OnButtonPressed;
        public Action OnButtonReleased;
        public Action OnButtonHeld;
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
            if(bound) return;
            bound = true;
            action.started += OnButtonStart;
            action.canceled += OnButtonStop;
        }

        internal void Unbind()
        {
            if (!bound) return;
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
}