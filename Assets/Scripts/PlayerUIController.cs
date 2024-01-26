using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    private VisualElement Root => playerUi.rootVisualElement;

    private VisualElement screenFade;
    private VisualElement miniMapContainer;

    private void Awake()
    {
        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
        miniMapContainer = Root.Q("MiniMap");
        screenFade = Root.Q("ScreenFade");
        screenFade.style.display = DisplayStyle.None;
    }
    
    public void SetMiniMapVisible(bool visible)
    {
        miniMapContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
        StartCoroutine(FadeScreenOperation(startAlpha, endAlpha, duration));
    }

    private IEnumerator FadeScreenOperation(float startAlpha, float endAlpha, float duration)
    {
        screenFade.style.display = DisplayStyle.Flex;

        for (float i = 0; i < duration; i += Time.deltaTime)
        {
            yield return null;
            float t = Mathf.InverseLerp(0, duration, i);
            float opacity = Mathf.Lerp(startAlpha, endAlpha, t);
            screenFade.style.opacity = opacity;
        }
        if(endAlpha == 0)
        {
            yield return null;
            screenFade.style.display = DisplayStyle.None;
        }
    }
}
