using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class CaveMessageController : MonoBehaviour
{
    private static CaveMessageController instance;
    public static CaveMessageController Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected Cave Message Controller instance not found. Order of operations issue? Or Cave Message Controller is disabled/missing.");
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

    [SerializeField] private SpatialParadoxGenerator mapGenerator;

    [SerializeField] private UIDocument document;
    [SerializeField] private VisualTreeAsset messagePrefab;
    [SerializeField] private bool debugForceRandomOnStart = false;
    [SerializeField] private bool debugInfiniteRandom = false;
    [SerializeField, Range(0.001f,60)] private float debugLingerTime = 25f;


    private MessageContainer currentMessage;
    [Space]
    public List<Hash128> messageAssetHashes = new();
    public Dictionary<Hash128, MessageAsset> hashToMessage = new();
    public HashSet<Hash128> readableMessages = new();
    [Space]
    public List<Hash128> unreadMessages = new();
    public HashSet<Hash128> readMesssages = new();


    private void Awake()
    {
        if(document == null)
        {
            return;
        }
        document.rootVisualElement.Q("MessagePreview")?.Clear();
        // if (mapGenerator == null || !mapGenerator.isActiveAndEnabled)
        // {
        //     Debug.LogError("No Map Generator or Map Generator Disabled");
        //     enabled = false;
        //     return;
        // }

        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        TextAsset[] allMessages = Resources.LoadAll<TextAsset>("Notes");
        
        for (int i = 0; i < allMessages.Length; i++)
        {
            TextAsset textAsset = allMessages[i];
            using StringReader stringReader = new(textAsset.text);
            XmlSerializer reader = new(typeof(MessageAsset));
            MessageAsset asset = (MessageAsset)reader.Deserialize(stringReader);
            asset.Unpack();
            asset.hash = Hash128.Compute(asset.assetName);

            if (hashToMessage.TryAdd(asset.hash, asset))
            {
                messageAssetHashes.Add(asset.hash);
                for (int j = 0; j < asset.dependsOn.Length; j++)
                {
                    asset.dependsOnHashes.Add(Hash128.Compute(asset.dependsOn[j]));
                }
                asset.dependsOn = null;
            }
            else
            {
                Debug.LogWarningFormat("Duplicate(s) of \'{0}\' exist in the asset database. Skipping!", asset.assetName);
            }
        }
        unreadMessages.AddRange(messageAssetHashes);
        Debug.LogFormat("Loaded {0} Messages", messageAssetHashes.Count);
        UpdateReadableMessages();
        if (InputManager.Instance != null)
        {
            //InputManager.Instance.advanceDialogueButton.OnButtonReleased += CloseMessage;
        }
        if (debugForceRandomOnStart)
        {
            Debug.Log("Testing random message show!");
            DebugRandom();
        }
    }

    private void DebugRandom()
    {
        if (ShowRandomMessage(out _))
        {
            Debug.LogFormat("Closing Test random message in {0} seconds!", debugLingerTime.ToString("0.0"));
            Invoke(nameof(CloseMessage), debugLingerTime);
            if (debugInfiniteRandom)
            {
                Invoke(nameof(DebugRandom), debugLingerTime+0.5f);
            }
        }
    }

    //private void OnDisable()
    //{
    //    if(InputManager.Instance != null)
    //    {
    //        InputManager.Instance.advanceDialogueButton.OnButtonReleased -= CloseMessage;
    //    }
    //}

    public bool ShowRandomMessage(out Hash128 chosenMessage)
    {
        List<Hash128> pickAbleMessages = new(readableMessages);
        if(pickAbleMessages.Count == 0)
        {
            chosenMessage = new Hash128();
            return false ;
        }

        chosenMessage = pickAbleMessages[Random.Range(0, pickAbleMessages.Count)];

        bool success;
        if(hashToMessage.TryGetValue(chosenMessage,out MessageAsset asset))
        {
            asset.shownToPlayer = true;
            ShowMessage(chosenMessage);
            success = true;
        }
        else
        {
            success = false;
            Debug.LogError("Chosen message not present in Database!");
        }
        UpdateReadableMessages();
        return success;
    }
    
    public void TryShowMessageByName(string messageName)
    {
        Hash128 hash = Hash128.Compute(messageName);
        if(hashToMessage.ContainsKey(hash))
        {
            ShowMessage(hash);
        }
    }

    public void ShowMessage(Hash128 message)
    {
        if (document != null && hashToMessage.ContainsKey(message))
        {
            VisualElement messageContainer;
            if (document.rootVisualElement == null && document.visualTreeAsset == null)
            {
                document.visualTreeAsset = messagePrefab;
                messageContainer = document.rootVisualElement.Q("MessagePreview");
            }
            else
            {
                messageContainer = messagePrefab.Instantiate().Q("Container");
                document.rootVisualElement.Add(messageContainer);
            }

            if (document.rootVisualElement != null)
            {
                currentMessage = new(messageContainer, hashToMessage[message]);
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.UnlockPointer();
                    InputManager.Instance.advanceDialogueButton.OnButtonReleased += CloseMessage;
                }
            }
            else
            {
                Debug.LogError("Problem with UI Document, it may not be assigned.");
            }
        }
        else
        {
            Debug.LogError("UI Document not assigned or chosen message not present in Database!");
        }
    }

    public void CloseMessage()
    {
        if(currentMessage != null)
        {
            document.rootVisualElement.Remove(currentMessage.root);
            currentMessage = null;
            if (InputManager.Instance != null)
            {
                InputManager.Instance.LockPointer();
                InputManager.Instance.advanceDialogueButton.OnButtonReleased -= CloseMessage;

            }
        }
    }

    private void UpdateReadableMessages()
    {
        for (int i = unreadMessages.Count - 1; i >= 0; i--)
        {
            Hash128 hash = unreadMessages[i];
            MessageAsset asset = hashToMessage[hash];
            if (!asset.randomlyFound || asset.shownToPlayer || readMesssages.Contains(hash))
            {
                readMesssages.Add(hash);
                unreadMessages.RemoveAt(i);
                readableMessages.Remove(hash);
                continue;
            }
            else if (asset.dependsOnHashes.Count > 0 && !readMesssages.IsSupersetOf(asset.dependsOnHashes))
            {
                readableMessages.Remove(hash);
                continue;
            }
            if (asset.dependsOnHashes.Count == 0
                || (asset.dependsOnHashes.Count > 0 && readMesssages.IsSupersetOf(asset.dependsOnHashes)))
            {
                readableMessages.Add(hash);
            }
        }

        Debug.LogFormat("There are {0} Messages unread", unreadMessages.Count);
        Debug.LogFormat("There are {0} Messages read", readMesssages.Count);
        Debug.LogFormat("There are {0} Messages readable", readableMessages.Count);
    }
}

public class MessageContainer
{
    public VisualElement root;
    public VisualElement messageBody;
    public VisualElement pageIncrementContainer;
    public Label text;
    public Label pageNumber;
    public Button forwardButton;
    public Button backButton;
    public Texture2D backgroundImage;

    public MessageAsset asset;
    public int pageIndex = 0; 

    public Color PageIncremenetColour
    {
        set
        {

            forwardButton.style.unityBackgroundImageTintColor = value;
            backButton.style.unityBackgroundImageTintColor = value;
            pageNumber.style.color = value;
        }
    }

    public MessageContainer(VisualElement root, MessageAsset asset, Texture2D backgroundImage = null)
    {
        this.root = root;
        this.asset = asset;
        messageBody = root.Q("MessagePreview");
        text = messageBody.Q<Label>();
        pageIncrementContainer = root.Q("PagePanel");
        pageNumber = pageIncrementContainer.Q<Label>();
        forwardButton = pageIncrementContainer.Q<Button>("PageForwardArrow");
        backButton = pageIncrementContainer.Q<Button>("PageBackArrow");

        forwardButton.style.unityBackgroundImageTintColor = (Color)asset.pageIncrementColour;
        backButton.style.unityBackgroundImageTintColor = (Color)asset.pageIncrementColour;
        pageNumber.style.color = (Color)asset.pageIncrementColour;


        this.backgroundImage = backgroundImage;
        messageBody.style.backgroundImage = backgroundImage;

        text.text = asset.pages[pageIndex];

        messageBody.style.width = asset.dimentions.x;
        //messageBody.style.height = asset.dimentions.y+20;

        if (asset.backgroundIndex < 0)
        {
            messageBody.style.backgroundColor = (Color)asset.backgroundTint;
        }
        else
        {
            messageBody.style.unityBackgroundImageTintColor = (Color)asset.backgroundTint;
        }
        if (asset.pages.Length <= 1)
        {
            pageIncrementContainer.style.display = DisplayStyle.None;
        }

        backButton.SetEnabled(false);
        UpdatePageNumberDisplay(pageIndex, asset.pages.Length);
        forwardButton.RegisterCallback<ClickEvent>(ev => NextPage());
        backButton.RegisterCallback<ClickEvent>(ev => PreviousPage());
        forwardButton.RegisterCallback<NavigationSubmitEvent>(ev => NextPage());
        backButton.RegisterCallback<NavigationSubmitEvent>(ev => PreviousPage());
    }

    public MessageContainer(VisualElement root, Texture2D backgroundImage = null)
    {
        this.root = root;
        messageBody = root;
        text = root.Q<Label>();
        this.backgroundImage = backgroundImage;
        pageIncrementContainer = root.Q("PagePanel");
        pageNumber = pageIncrementContainer.Q<Label>();
        forwardButton = pageIncrementContainer.Q<Button>("PageForwardArrow");
        backButton = pageIncrementContainer.Q<Button>("PageBackArrow");
    }

    public void NextPage()
    {
        if(pageIndex < asset.pages.Length - 1)
        {
            pageIndex++;
            UpdatePageNumberDisplay(pageIndex,asset.pages.Length);

            text.text = asset.pages[pageIndex];
            if (pageIndex == asset.pages.Length - 1)
            {
                forwardButton.SetEnabled(false);
            }
            if (pageIndex > 0)
            {
                backButton.SetEnabled(true);
            }
        }
    }

    public void UpdatePageNumberDisplay(int pageIndex, int totalPages)
    {
        pageNumber.text = string.Format("{0}/{1}", pageIndex + 1, totalPages);
    }

    public void PreviousPage()
    {
        if (pageIndex > 0)
        {
            pageIndex--;
            text.text = asset.pages[pageIndex];
            UpdatePageNumberDisplay(pageIndex, asset.pages.Length);

            if (pageIndex == 0)
            {
                backButton.SetEnabled(false);
            }
            if (pageIndex < asset.pages.Length - 1)
            {
                forwardButton.SetEnabled(true);
            }
        }
    }
}

[Serializable]
public class MessageAsset
{
    public string assetName = "Empty Asset";
    public int backgroundIndex = -1;
    public Color32 backgroundTint = Color.white;
    public Vector2Int dimentions;
    public bool randomlyFound = true;
    public string messageText = "Sample Text.";
    public string[] dependsOn = new string[0];
    public Color32 pageIncrementColour = Color.white;
    [XmlIgnore] public Hash128 hash;
    [XmlIgnore] public HashSet<Hash128> dependsOnHashes = new();
    [XmlIgnore] public bool shownToPlayer = false;
    [XmlIgnore] public string[] pages;

    public void Unpack()
    {
        pages = messageText.Split('¬', StringSplitOptions.RemoveEmptyEntries);
    }
}