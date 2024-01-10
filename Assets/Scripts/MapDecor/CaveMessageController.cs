using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

public class CaveMessageController : MonoBehaviour
{
    [SerializeField] private string messageFilePath;
    [SerializeField] private SpatialParadoxGenerator mapGenerator;

    private void Awake()
    {
        
        if (mapGenerator == null || !mapGenerator.isActiveAndEnabled)
        {
            Debug.LogError("No Map Generator or Map Generator Disabled");
            enabled = false;
            return;
        }
    }

    void Update()
    {

    }
}

public class MessageContainer
{
    public VisualElement root;
    public Label text;
    public Texture2D backgroundImage;

    public MessageAsset asset;

    public MessageContainer(VisualElement root, MessageAsset asset, Texture2D backgroundImage = null)
    {
        this.root = root;
        this.asset = asset;
        text = root.Q<Label>();
        this.backgroundImage = backgroundImage;
    }
    public MessageContainer(VisualElement root, Texture2D backgroundImage = null)
    {
        this.root = root;
        text = root.Q<Label>();
        this.backgroundImage = backgroundImage;
    }
}

public class MessageAsset
{
    public string assetName = "Empty Asset";
    public int backgroundIndex = -1;
    public Color32 backgroundTint = Color.white;
    public string messageText = "Sample Text.";
}