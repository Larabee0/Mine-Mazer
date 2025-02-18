using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerUIController : MonoBehaviour
{
    private static PlayerUIController instance;
    public static PlayerUIController Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected Player UI Controller instance not found. Order of operations issue? Or Player UI Controller is disabled/missing.");
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

    [SerializeField] private UIDocument playerUi;
    public VisualTreeAsset ItemMote;
    [SerializeField] private CompendiumUI compendiumUI;
    [SerializeField] private Texture2D compendiumIcon;
    public Texture2D CompendiumIcon=> compendiumIcon;

    private VisualElement Root => playerUi.rootVisualElement;
    public CompendiumUI CompendiumUI => compendiumUI;
    public InventoryController InventoryMenuUI => inventoryMenu;

    public SettingsMenuController SettingsMenu=>settingsMenu;
    private SettingsMenuController settingsMenu;
    private InventoryController inventoryMenu;
    private Motes motes;
    private VisualElement pauseButtonContainer;
    private Button resume;
    private Button settings;
    private Button mainMenu;
    private VisualElement overlay;
    private VisualElement screenFade;
    private VisualElement miniMapContainer;
    private VisualElement crossHair;

    public bool ShowCrosshair
    {
        get => crossHair.style.display == DisplayStyle.Flex;
        set => crossHair.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public bool PauseMenuOpen => pauseButtonContainer.style.display == DisplayStyle.Flex;

    [SerializeField] private VisualTreeAsset settingsMenuPrefab;

    [Header("Hunger bar settings")]
    private float curHunger;
    [SerializeField] private float hungerProgressAnimationSpeed;
    [SerializeField] private float hungerFlashFadeTime;
    [SerializeField] private float lowHungerThreshold;
    public float LowHungerThreshold => lowHungerThreshold;
    [SerializeField] private Color lowHungerColour;
    [SerializeField] private Color normalHungerColour;
    private ProgressBar hungerBar;
    private VisualElement hungerBarProgress;
    private Coroutine hungerBarFlashProcess;
    private Coroutine fadeScreenProcess = null;

    private void Awake()
    {
        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        overlay = Root.Q("Overlay");
        pauseButtonContainer = Root.Q("PauseButtonContainer");
        resume = Root.Q<Button>("ResumeButton");
        settings = Root.Q<Button>("SettingsButton");
        mainMenu = Root.Q<Button>("MainMenuButton");

        inventoryMenu = new(Root.Q("Inventory"));
        motes = new(Root.Q("Mote"), this);

        settings.RegisterCallback<ClickEvent>(ev => OpenSettingsMenu());
        settings.RegisterCallback<NavigationSubmitEvent>(ev => OpenSettingsMenu());
        resume.RegisterCallback<ClickEvent>(ev =>
        {
            SetPauseMenuActive(false);
            InputManager.Instance.LockPointer();
        });
        resume.RegisterCallback<NavigationSubmitEvent>(ev =>
        {
            SetPauseMenuActive(false);
            InputManager.Instance.LockPointer();
        });
        mainMenu.RegisterCallback<ClickEvent>(ev =>
        {

            SetPauseMenuActive(false);
            Application.Quit();
        });
        mainMenu.RegisterCallback<NavigationSubmitEvent>(ev =>
        {
            SetPauseMenuActive(false);
            Application.Quit();
        });
        miniMapContainer = Root.Q("MiniMap");
        screenFade = Root.Q("ScreenFade");
        hungerBar = Root.Q<ProgressBar>("HungerBar");
        crossHair = Root.Q("CrossHair");
        hungerBarProgress = hungerBar[0][0][0];
        screenFade.style.display = DisplayStyle.None;
        StartCoroutine(SetHungerBarProgress());
        SetHungerVisible(false);
    }

    private void OnEnable()
    {
        SetInventoryActive(false);
        if (InputManager.Instance != null)
        {
            InputManager.Instance.pauseButton.OnButtonReleased += TogglePauseMenu;
        }
    }
    
    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.pauseButton.OnButtonReleased -= TogglePauseMenu;
        }
    }

    public void BindUnBindMotes(bool bind)
    {
        if (bind)
        {
            if (Inventory.Instance)
            {
                motes.BindInventory();
            }
        }
        else
        {
            if (Inventory.Instance)
            {
                motes.UnbindInventory();
            }
        }
    }

    private void TogglePauseMenu()
    {
        InputManager.Instance.SetPointerLocked(PauseMenuOpen);
        if (inventoryMenu.IsOpen)
        {
            inventoryMenu.Close();
        }
        SetPauseMenuActive(!PauseMenuOpen);
        
    }

    public void SetMiniMapVisible(bool visible)
    {
        miniMapContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetHungerVisible(bool visible)
    {
        hungerBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }


    public void OpenSettingsMenu()
    {
        if (settingsMenu == null)
        {
            TemplateContainer container = settingsMenuPrefab.Instantiate();
            settingsMenu = new SettingsMenuController(container.Q("Overlay"));
            Root.Q("Overlay").Add(settingsMenu.RootVisualElement);
            settingsMenu.OnSettingsMenuClose += OpenPauseMenu;
        }
        SetPauseMenuActive(false);
        settingsMenu.Open();
    }

    private void OpenPauseMenu()
    {
        SetPauseMenuActive(true);
    }

    private void FocusResume()
    {
        resume.Focus();
    }

    public void SetPauseMenuActive(bool active)
    {
        if (active)
        {
            if (compendiumUI.CompendiumController != null && compendiumUI.CompendiumController.IsOpen)
            {
                compendiumUI.CompendiumController.SetActive(false);
            }
            if (inventoryMenu!=null && inventoryMenu.IsOpen)
            {
                inventoryMenu.Close();
            }

            Hunger.Instance.pause = true;
            Invoke(nameof(FocusResume),0);
        }
        else
        {
            Hunger.Instance.pause = false;
        }
        pauseButtonContainer.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetInventoryActive(bool active)
    {
        if (active)
        {
            if (compendiumUI.CompendiumController.IsOpen)
            {
                compendiumUI.CompendiumController.SetActive(false);
            }
            inventoryMenu.Open();
        }
        else
        {
            inventoryMenu.Close();
        }
    }


    public void FadeIn(float duration = 1)
    {
        FadeScreen(1, 0, duration);
    }

    public void FadeOut(float duration = 1)
    {
        FadeScreen(0, 1, duration);
    }

    public void FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (fadeScreenProcess != null)
        {
            StopCoroutine(fadeScreenProcess);
        }
        fadeScreenProcess = StartCoroutine(FadeScreenOperation(startAlpha, endAlpha, duration));
    }

    private IEnumerator FadeScreenOperation(float startAlpha, float endAlpha, float duration)
    {
        screenFade.style.display = DisplayStyle.Flex;
        

        for (float i = 0; i <= duration; i += Time.deltaTime)
        {
            yield return null;
            float t = Mathf.InverseLerp(0, duration, i);
            float opacity = Mathf.Lerp(startAlpha, endAlpha, t);
            screenFade.style.opacity = opacity;
        }

        screenFade.style.opacity = endAlpha;
        if (endAlpha == 0)
        {
            yield return null;
            screenFade.style.display = DisplayStyle.None;
        }
        fadeScreenProcess = null;
    }

    public void StartHungerFlash()
    {
        hungerBarFlashProcess ??= StartCoroutine(HungerBarFlash());
    }
    
    public void StopHungerFlash()
    {
        if(hungerBarFlashProcess != null)
        {
            StartCoroutine(HungerBarFadeToNormal());
        }
    }

    public void SetHungerBarProgress(float curHunger)
    {
        this.curHunger =  curHunger;
    }

    private IEnumerator SetHungerBarProgress()
    {
        while (true)
        {
            yield return null;
            hungerBar.value = Mathf.MoveTowards(hungerBar.value, curHunger, Time.deltaTime * hungerProgressAnimationSpeed);
            if(hungerBar.value < lowHungerThreshold)
            {
                StartHungerFlash();
            }
            else
            {
                StopHungerFlash();
            }
        }
    }

    private IEnumerator HungerBarFlash()
    {
        Color startColour = normalHungerColour;
        Color endColour = lowHungerColour;
        while (true)
        {
            for (float i = 0; i <= hungerFlashFadeTime; i+=Time.deltaTime)
            {
                yield return null;
                hungerBarProgress.style.backgroundColor = Color.Lerp(startColour, endColour, Mathf.InverseLerp(0, hungerFlashFadeTime, i));
            }
            (startColour, endColour) = (endColour, startColour);
        }
    }

    private IEnumerator HungerBarFadeToNormal()
    {
        StopCoroutine(hungerBarFlashProcess);
        Color currentColour = hungerBarProgress.style.backgroundColor.value;
        Color targetColour = normalHungerColour;
        if (currentColour == targetColour)
        {
            yield break;
        }
        Color t = ExtraUtilities.InverseLerp(lowHungerColour, normalHungerColour, currentColour);

        for (float i = Mathf.Lerp(0, hungerFlashFadeTime, t.r); i <= hungerFlashFadeTime; i += Time.deltaTime)
        {
            yield return null;
            hungerBarProgress.style.backgroundColor = Color.Lerp(lowHungerColour, normalHungerColour, Mathf.InverseLerp(0, hungerFlashFadeTime, i));

        }

        hungerBarFlashProcess = null;
    }
}
