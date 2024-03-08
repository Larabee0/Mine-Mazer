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
    private Label interactText;
    private Label objectiveText;
    private VisualElement interactRoot;
    private VisualElement objectiveRoot;
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
        objectiveRoot = uiController.rootVisualElement.Q("ObjectiveTextRoot");
        interactRoot = uiController.rootVisualElement.Q("InteractionText");
        texture = uiController.rootVisualElement.Q("InteractImage");
        interactText = uiController.rootVisualElement.Q<Label>("InteractText");
        objectiveText = uiController.rootVisualElement.Q<Label>("ObjectiveText");
        
        HideInteraction(true);
        ClearObjective();
    }

    public void ShowInteraction(string message, Texture2D texture, Color tint)
    {
        StopAllCoroutines();
        interactText.text = message;
        this.texture.style.backgroundImage = texture;
        this.texture.style.unityBackgroundImageTintColor = tint;

        interactRoot.style.opacity = 1;
        interactRoot.style.display = DisplayStyle.Flex;
        open = true;
    }

    public void SetObjective(string objective)
    {
        objectiveText.text = objective;
        objectiveRoot.style.display = DisplayStyle.Flex;
    }

    public void ClearObjective()
    {
        objectiveText.text = "";
        objectiveRoot.style.display = DisplayStyle.None;
    }

    public void HideInteraction(bool now = false)
    {
        StopAllCoroutines();
        if (now)
        {
            interactRoot.style.display = DisplayStyle.None;
            open = false;
            return;
        }
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
