using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeGame.Input
{
    [Serializable]
    public class AxisEventContainer
    {
        private readonly InputAction action;
        private readonly InputManager inputManager;
        private Coroutine axisProcess;
        private bool axisActive = false;
        private bool bound = false;

        public Action<Vector2> OnAxis;
        public Action<float> OnAxisAngle;
        public bool AxisActive => axisActive;
        public Vector2 AxisValue => action.ReadValue<Vector2>();
        public float AxisAngle => (Mathf.Atan2(-AxisValue.y, AxisValue.x) * Mathf.Rad2Deg);//+ 180;

        public AxisEventContainer(InputManager inputManager, InputAction action)
        {
            this.inputManager = inputManager;
            this.action = action;
        }

        internal void Bind()
        {
            if (bound) return;
            bound = true;
            action.started += AxisStarted;
            action.canceled += AxisStopped;
        }

        internal void Unbind()
        {
            if (!bound) return;
            bound = false;
            action.started -= AxisStarted;
            action.canceled -= AxisStopped;
        }

        private void AxisStarted(InputAction.CallbackContext context)
        {
            if (axisProcess != null)
            {
                inputManager.StopCoroutine(axisProcess);
            }

            axisProcess = inputManager.StartCoroutine(AxisUpdate());
            axisActive = true;
        }

        private void AxisStopped(InputAction.CallbackContext context)
        {
            if (axisProcess != null)
            {
                inputManager.StopCoroutine(axisProcess);
                OnAxis?.Invoke(Vector2.zero);
                //OnAxisAngle?.Invoke(0);                
            }
            axisActive = false;
        }


        private IEnumerator AxisUpdate()
        {
            while (true)
            {
                OnAxis?.Invoke(AxisValue);
                OnAxisAngle?.Invoke(AxisAngle);                
                yield return null;
            }
        }
    }
}