using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuController : UIToolkitBase
{
    private Slider cameraSensitivitySlider;
    private Toggle cameraHInvert;
    private Toggle cameraVInvert;
    private Toggle moveHInvert;
    private Toggle moveVInvert;

    private Button calibrateScreenButton;
    private Button mainMenuButton;

    public Pluse OnSettingsMenuClose;

    public SettingsMenuController(VisualElement root) : base(root)
    {
        Query();
        Bind();
    }

    public override void Query()
    {
        cameraSensitivitySlider = RootVisualElement.Q<Slider>("CameraSensitivty");
        cameraHInvert = RootVisualElement.Q<Toggle>("InvertCameraHAxis");
        cameraVInvert = RootVisualElement.Q<Toggle>("InvertCameraVAxis");
        moveHInvert = RootVisualElement.Q<Toggle>("InverMoveHAxis");
        moveVInvert = RootVisualElement.Q<Toggle>("InvertMoveVAxis");

        calibrateScreenButton = RootVisualElement.Q<Button>("CalibrateScreen");
        mainMenuButton = RootVisualElement.Q<Button>("MainMenuButton");
    }

    public override void Bind()
    {
        cameraSensitivitySlider.RegisterCallback<FocusInEvent>(ev => FocusSlider());
        cameraSensitivitySlider.RegisterCallback<FocusOutEvent>(ev => UnFocusSlider());
        DoubleBindButton(mainMenuButton, delegate () { CloseSettings(); });
        DoubleBindButton(calibrateScreenButton, delegate () { CalibrateScreen(); });
    }

    private void CalibrateScreen()
    {
        Debug.Log("CalibrateScreen open");
    }

    private void CloseSettings()
    {
        RootVisualElement.style.display = DisplayStyle.None;
        OnSettingsMenuClose?.Invoke();
    }

    public void OpenSettings()
    {
        RootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void FocusSlider()
    {
        InputManager.Instance.EventSystemInput.scrollWheel.action.performed += IncrementSlider;
    }

    private void UnFocusSlider()
    {
        InputManager.Instance.EventSystemInput.scrollWheel.action.performed -= IncrementSlider;
    }

    private void IncrementSlider(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Vector2 inputValue = context.ReadValue<Vector2>();

        Debug.Log(inputValue);

        cameraSensitivitySlider.value += inputValue.x;
    }


}
