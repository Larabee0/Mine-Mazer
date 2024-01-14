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
    }
    
    public void SetMiniMapVisible(bool visible)
    {
        miniMapContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
