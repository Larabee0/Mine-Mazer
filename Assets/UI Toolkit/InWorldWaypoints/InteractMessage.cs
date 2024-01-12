using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InteractMessage : MonoBehaviour
{
    private static InteractMessage instance;
    public static InteractMessage Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Interact Message instance not found. Order of operations issue? Or Interact Message is disabled/missing.");
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

    [SerializeField] private UIDocument uiController;
    private VisualElement texture;
    private Label text;
    private VisualElement interactRoot;
    private bool open = false;

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

        interactRoot = uiController.rootVisualElement.Q("InteractionText");
        texture = uiController.rootVisualElement.Q("InteractImage");
        text = uiController.rootVisualElement.Q<Label>("InteractText");
        
        HideInteraction();
    }

    public void ShowInteraction(string message, Texture2D texture, Color tint)
    {
        StopAllCoroutines();
        text.text = message;
        this.texture.style.backgroundImage = texture;
        this.texture.style.unityBackgroundImageTintColor = tint;

        interactRoot.style.opacity = 1;
        interactRoot.style.display = DisplayStyle.Flex;
        open = true;
    }

    public void HideInteraction()
    {
        StopAllCoroutines();
        interactRoot.style.opacity = 1;
        StartCoroutine(HideInteractionFade());
    }

    private IEnumerator HideInteractionFade()
    {
        for (float i = 1f; i > 0; i -= Time.deltaTime)
        {
            interactRoot.style.opacity = i;
            yield return null;
        }
        interactRoot.style.display = DisplayStyle.None;
        open = false;
    }
}
