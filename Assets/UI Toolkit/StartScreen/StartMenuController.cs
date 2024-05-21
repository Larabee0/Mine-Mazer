using UnityEngine;
using UnityEngine.UIElements;

public class StartMenuController : UIToolkitBase
{
    private StartScreen startScreen;
    private VisualElement buttonContainer;
    private Button startButton;
    private Button settingsButton;
    private Button exitButton;
    private Label loadProgress;

    public StartMenuController(VisualElement rootVisualElement, StartScreen startScreen) : base(rootVisualElement)
    {
        this.startScreen = startScreen;
        Query();
        Bind();
    }

    public override void Query()
    {
        buttonContainer = RootQ("StartScreenButtonContainer");
        startButton = RootQ<Button>("StartButton");
        settingsButton = RootQ<Button>("SettingsButton");
        exitButton = RootQ<Button>("ExitButton");

        focusOnOpen = startButton;
    }

    public override void Bind()
    {
        DoubleBindButton(startButton, delegate () { startScreen.LoadMainScene(); });
        DoubleBindButton(settingsButton, delegate () { startScreen.OpenSettingsMenu(); });
        DoubleBindButton(exitButton, delegate() { ExitApplication(); });
    }

    public void ExitApplication()
    {
        Debug.Log("Exit Button Pressed, calling Application.Quit");
        Application.Quit();
    }

    public void UpdateLoadProgress(float value)
    {

    }
}