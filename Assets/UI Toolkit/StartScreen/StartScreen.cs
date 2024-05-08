using MazeGame.Input;
using MazeGame.Navigation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private UIDocument startScreenUI;
    [SerializeField] private VisualTreeAsset settingsMenuPrefab;
    [SerializeField] private TutorialStarter tutorialStarter;
    [SerializeField] private bool autoLoadOnPlay;
    private VisualElement RootVisualElement => startScreenUI.rootVisualElement;

    StartMenuController startScreenController;
    SettingsMenuController settingsMenuController;

    private void Start()
    {
        DontDestroyOnLoad(EventSystem.current.gameObject);
        DontDestroyOnLoad(tutorialStarter.gameObject);
        DontDestroyOnLoad(PlayerUIController.Instance.gameObject);
        startScreenController = new(RootVisualElement,this);
        InputManager.Instance.UnlockPointer();
        InputManager.Instance.SetUIToolkitFocus();
        startScreenController.Focus();
        PlayerUIController.Instance.ShowCrosshair = false;
        PlayerUIController.Instance.SetMiniMapVisible(false);
        startScreenController.SetActive(true);
        tutorialStarter.FadeIn();
        if (autoLoadOnPlay)
        {
            LoadMainScene();
        }
    }

    public void LoadMainScene()
    {
        StartCoroutine(OpenSceneCoroutine());
    }

    
    private IEnumerator OpenSceneCoroutine()
    {
        /// Run tutorial start during scene load.
        /// Perhaps look at blocking final scene load with allowSceneActivation if it completes
        /// before the tutorial would unfade the camera.
        startScreenController. SetActive(false);
        tutorialStarter.StopAllCoroutines();
        tutorialStarter.StartTutorialScript();
        yield return new WaitForSeconds(3.5f);
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(1);
        while (!sceneLoadOp.isDone)
        {
            sceneLoadOp.allowSceneActivation = tutorialStarter.allowSceneChange;
            startScreenController.UpdateLoadProgress(sceneLoadOp.progress);
            yield return null;
        }
    }

    public void OpenSettingsMenu()
    {
        if (settingsMenuController == null)
        {
            TemplateContainer container = settingsMenuPrefab.Instantiate();
            settingsMenuController = new SettingsMenuController(container.Q("Overlay"));
            RootVisualElement.Q("Overlay").Add(settingsMenuController.RootVisualElement);            
            settingsMenuController.OnSettingsMenuClose += OpenStartScreen;
        }
        startScreenController.SetActive(false);
        settingsMenuController.OpenSettings();
    }

    private void OpenStartScreen()
    {
        settingsMenuController.OnSettingsMenuClose-=OpenStartScreen;
        startScreenController.SetActive(true);
    }
}
