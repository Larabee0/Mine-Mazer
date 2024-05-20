using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class SettingsMenuController : UIToolkitBase
{
    private VisualElement settingsMain;
    private Slider cameraSensitivitySlider;
    private Toggle cameraHInvert;
    private Toggle cameraVInvert;
    private Toggle moveHInvert;
    private Toggle moveVInvert;

    private Toggle skipIntro;
    private Toggle skipTutorial;

    private Button calibrateScreenButton;
    private Button mainMenuButton;



    private VisualElement brightnessContrastWindow;
    private Slider brightnessSlider;
    private Slider contrastSlider;
    private Button finishButton;
    private Button resetBrightnessAndConstrastButton;
    private Toggle hideStars;

    private Volume volume ;
    private Slider focusedSlider;

    public Action OnSettingsMenuClose;

    public SettingsMenuController(VisualElement root) : base(root)
    {
        Query();
        Bind();
        settingsMain.style.display = DisplayStyle.Flex;
        brightnessContrastWindow.style.display = DisplayStyle.None;
        GetSettings();
    }

    public override void Query()
    {
        cameraSensitivitySlider = RootQ<Slider>("CameraSensitivty");
        cameraHInvert = RootQ<Toggle>("InvertCameraHAxis");
        cameraVInvert = RootQ<Toggle>("InvertCameraVAxis");
        moveHInvert = RootQ<Toggle>("InverMoveHAxis");
        moveVInvert = RootQ<Toggle>("InvertMoveVAxis");


        skipIntro = RootQ<Toggle>("SkipIntro");
        skipTutorial = RootQ<Toggle>("SkipTutorial");

        calibrateScreenButton = RootQ<Button>("CalibrateScreen");
        mainMenuButton = RootQ<Button>("MainMenuButton");

        settingsMain = RootQ("SettingsContainer");



        hideStars = RootQ<Toggle>("HideStars");
        brightnessSlider = RootQ<Slider>("BrightnessSlider");
        contrastSlider = RootQ<Slider>("ConstrastSlider");

        finishButton = RootQ<Button>("BackToSettings");
        resetBrightnessAndConstrastButton = RootQ<Button>("ResetConstrastBrightnessButton");

        brightnessContrastWindow = RootQ("BrightnessConstrastContainer");
    }

    public override void Bind()
    {
        cameraSensitivitySlider.RegisterCallback<FocusInEvent>(ev => FocusSlider(cameraSensitivitySlider));
        cameraSensitivitySlider.RegisterCallback<FocusOutEvent>(ev => UnFocusSlider(cameraSensitivitySlider));
        
        brightnessSlider.RegisterCallback<FocusInEvent>(ev => FocusSlider(brightnessSlider));
        brightnessSlider.RegisterCallback<FocusOutEvent>(ev => UnFocusSlider(brightnessSlider));
        
        contrastSlider.RegisterCallback<FocusInEvent>(ev => FocusSlider(contrastSlider));
        contrastSlider.RegisterCallback<FocusOutEvent>(ev => UnFocusSlider(contrastSlider));

        contrastSlider.RegisterValueChangedCallback(ev =>SetContrast(ev.newValue));
        brightnessSlider.RegisterValueChangedCallback(ev =>SetBrightness(ev.newValue));

        hideStars.RegisterValueChangedCallback(ev=>SetStarsEnable(ev.newValue));

        DoubleBindButton(mainMenuButton, delegate () { CloseSettings(); });
        DoubleBindButton(calibrateScreenButton, delegate () { CalibrateScreen(); });

        DoubleBindButton(resetBrightnessAndConstrastButton, delegate () { DefaultContrastBrightness(); });
        DoubleBindButton(finishButton, delegate () { CloseContrastBrightnessMenu(); });
    }

    private void SetStarsEnable(bool newValue)
    {
        var obj = GameObject.FindAnyObjectByType<BrightnessConstrastImage>(FindObjectsInactive.Include);
        obj.gameObject.SetActive(!newValue);
    }

    private void SetBrightness(float newValue)
    {
        if (volume == null)
        {
            volume = GameObject.FindAnyObjectByType<Volume>();
        }
        VolumeComponent comp = volume.profile.components.Find(comp => comp.GetType() == typeof(ColorAdjustments));
        if (comp != null && comp is ColorAdjustments adjustments)
        {
            adjustments.postExposure.value=(newValue);
        }
    }

    private void SetContrast(float newValue)
    {
        if (volume == null)
        {
            volume = GameObject.FindAnyObjectByType<Volume>();
        }
        VolumeComponent comp = volume.profile.components.Find(comp => comp.GetType() == typeof(ColorAdjustments));
        if (comp != null && comp is ColorAdjustments adjustments)
        {
            adjustments.contrast.value=(newValue);
        }
    }

    private void CalibrateScreen()
    {
        Debug.Log("CalibrateScreen open");
        var obj = GameObject.FindAnyObjectByType<BrightnessConstrastImage>(FindObjectsInactive.Include);
        obj.gameObject.SetActive(true);
        brightnessContrastWindow.style.display = DisplayStyle.Flex;
        settingsMain.style.display = DisplayStyle.None;
        finishButton.Focus();
    }

    private void GetSettings()
    {
        var settings = PlayerSettings.Instance.userSettings;
        cameraHInvert.value = settings.cameraHInvert;
        cameraVInvert.value = settings.cameraVInvert;
        moveHInvert.value = settings.moveHInvert;
        moveVInvert.value = settings.moveVInvert;
        cameraSensitivitySlider.value = settings.cameraSens;
        contrastSlider.value  = settings.constrastAdj;
        brightnessSlider.value = settings.postExposureAdj;
        skipIntro.value = settings.skipIntro;
        skipTutorial.value = settings.skipTutorial;
    }

    private void CloseSettings()
    {
        RootVisualElement.style.display = DisplayStyle.None;
        SetSettings();
        OnSettingsMenuClose?.Invoke();
    }

    private void SetSettings()
    {
        var settings = PlayerSettings.Instance.userSettings;
        settings.cameraHInvert = cameraHInvert.value;
        settings.cameraVInvert = cameraVInvert.value;
        settings.moveHInvert = moveHInvert.value;
        settings.moveVInvert = moveVInvert.value;
        settings.cameraSens = cameraSensitivitySlider.value;
        settings.constrastAdj = contrastSlider.value;
        settings.postExposureAdj = brightnessSlider.value;
        settings.skipIntro = skipIntro.value;
        settings.skipTutorial = skipTutorial.value;
        PlayerSettings.Instance.OnSettingsChanged?.Invoke();
    }

    public void OpenSettings()
    {
        GetSettings();
        RootVisualElement.style.display = DisplayStyle.Flex;
        calibrateScreenButton.Focus();
    }

    private void CloseContrastBrightnessMenu()
    {
        GameObject.FindAnyObjectByType<BrightnessConstrastImage>(FindObjectsInactive.Include).gameObject.SetActive(false);
        SetSettings();
        brightnessContrastWindow.style.display = DisplayStyle.None;
        settingsMain.style.display = DisplayStyle.Flex;
    }

    private void DefaultContrastBrightness()
    {
        var defaultSettings = new SettingsForDisk();
        contrastSlider.value = defaultSettings.constrastAdj;
        brightnessSlider.value = defaultSettings.postExposureAdj;
    }

    private void FocusSlider(Slider slider)
    {
        if (focusedSlider == null)
        {
            focusedSlider = slider;
            InputManager.Instance.EventSystemInput.scrollWheel.action.performed += IncrementSlider;
        }
    }

    private void UnFocusSlider(Slider slider)
    {
        if(focusedSlider == slider)
        {
            focusedSlider = null;
            InputManager.Instance.EventSystemInput.scrollWheel.action.performed -= IncrementSlider;
        }
    }

    private void IncrementSlider(InputAction.CallbackContext context)
    {
        Vector2 inputValue = context.ReadValue<Vector2>();

        Debug.Log(inputValue);

        focusedSlider.value += inputValue.x;
    }
}
