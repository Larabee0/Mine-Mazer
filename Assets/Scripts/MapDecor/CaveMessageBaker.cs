#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CaveMessageBaker : MonoBehaviour
{
    public MessageSection[] messageParts;
    public int backgroundImageIndex;
    public Color backgroundTint = Color.white;
    public int width = 700;
    public Color incrementColour = Color.black;
    
    public string[] dependsOn;
    public bool randomlyFound = true;
    [Header("Debug Display")]
    public int pageIndex = 0;
    [Header("Save Directory Stuff")]
    public string assetName = "Example Message";
    public string prefabsDirectory = "";
    public string folderName = "";
    [Header("Components and debug")]
    public UIDocument document;
    public VisualTreeAsset messagePrefab;
    public bool autoUpdate = false;
    private void OnValidate()
    {
        if (autoUpdate)
        {
            Preview();
        }
    }
    public void Preview()
    {
        if (document != null)
        {
            if(document.rootVisualElement == null || document.visualTreeAsset == null)
            {
                document.visualTreeAsset = messagePrefab;
            }
            if (document.rootVisualElement != null && document.visualTreeAsset != null)
            {
                VisualElement messageContainer = document.rootVisualElement.Q("MessagePreview");
                MessageContainer container = new(messageContainer)
                {
                    PageIncremenetColour = incrementColour
                };
                PreviewPage(container);

                container.root.style.width = width;
                //container.root.style.height = dimentions.y;
                if (backgroundImageIndex < 0)
                {
                    container.root.style.backgroundColor = backgroundTint;
                }
                else
                {
                    container.root.style.unityBackgroundImageTintColor = backgroundTint;
                }
                return;
            }

        }

        Debug.LogError("Error with UI Document or VTA. Check assignments in inspector.");
    }

    private void PreviewPage(MessageContainer container)
    {
        string packedPages = PackageText();
        string[] unpackedPages = packedPages.Split('¬', StringSplitOptions.RemoveEmptyEntries);

        pageIndex = Mathf.Clamp(pageIndex, 0, unpackedPages.Length - 1);

        container.text.text = unpackedPages[pageIndex];
        if (unpackedPages.Length > 1)
        {
            container.pageIncrementContainer.style.display = DisplayStyle.Flex;
            container.UpdatePageNumberDisplay(pageIndex, unpackedPages.Length);
        }
        else
        {
            container.pageIncrementContainer.style.display = DisplayStyle.None;
        }
    }

    public void BakeDown()
    {
        MessageAsset asset = new()
        {
            assetName = assetName,
            backgroundIndex = backgroundImageIndex,
            backgroundTint = backgroundTint,
            dimentions = new Vector2Int(width,0),
            messageText = PackageText(),
            dependsOn = dependsOn,
            randomlyFound = randomlyFound,
            pageIncrementColour = incrementColour
        };

        EditableMessage editableMessage = new()
        {
            assetName = assetName,
            backgroundImageIndex = backgroundImageIndex,
            backgroundTint = backgroundTint,
            dimentions = new Vector2Int(width,0),
            sections = new MessageSection[messageParts.Length],
            dependsOn = dependsOn,
        };

        messageParts.CopyTo(editableMessage.sections,0);
        CreatePrefab(asset, editableMessage);    
    }

    private void CreatePrefab(MessageAsset asset,EditableMessage editableMessage)
    {
        string targetPath = prefabsDirectory + "/" + folderName;
        if (!Directory.Exists(prefabsDirectory))
        {
            string[] folders = prefabsDirectory.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < folders.Length; i++)
            {
                AssetDatabase.CreateFolder(folders[i - 1], folders[i]);
            }
        }
        if (!Directory.Exists(targetPath))
        {
            AssetDatabase.CreateFolder(prefabsDirectory, folderName);
        }
        Debug.Log(Application.dataPath);
        string mainAssetPath = targetPath + "/" + asset.assetName + ".xml";
        mainAssetPath = AssetDatabase.GenerateUniqueAssetPath(mainAssetPath);
        if (mainAssetPath == null)
        {
            Debug.LogError("local path invalid");
            return;
        }

        targetPath = Application.dataPath + "/" + folderName;
        mainAssetPath = targetPath + "/" + asset.assetName + ".xml";
        string editableAssetPath = targetPath + "/" +  editableMessage.assetName + "_EDITABLE" + ".ecm";
        XmlSerializer writer = new(typeof(MessageAsset));
        FileStream file = File.Create(mainAssetPath);
        writer.Serialize(file, asset);
        file.Close();
        
        writer = new(typeof(EditableMessage));
        file = File.Create(editableAssetPath);
        writer.Serialize(file, editableMessage);
        file.Close();
        AssetDatabase.Refresh();
    }

    public void Load()
    {
        string targetPath = prefabsDirectory + "/" + folderName;
        if (!Directory.Exists(prefabsDirectory))
        {
            Debug.LogError("Specified Directory does not exist.");
            return;
        }
        if (!Directory.Exists(targetPath))
        {
            Debug.LogError("Specified Directory does not exist.");
            return;
        }
        targetPath = Application.dataPath + "/" + folderName;
        string editableAssetPath = targetPath + "/" + assetName + "_EDITABLE" + ".ecm";


        XmlSerializer reader = new(typeof(EditableMessage));
        StreamReader file = new(editableAssetPath);
        EditableMessage editableMessage = (EditableMessage)reader.Deserialize(file);
        file.Close();

        backgroundImageIndex = editableMessage.backgroundImageIndex;
        backgroundTint = editableMessage.backgroundTint;
        width = editableMessage.dimentions.x;
        messageParts = new MessageSection[editableMessage.sections.Length];
        dependsOn = editableMessage.dependsOn;

        for (int i = 0; i < messageParts.Length; i++)
        {
            messageParts[i] = new(editableMessage.sections[i]);
        }
        Preview();
    }

    private string PackageText()
    {
        string pak = string.Empty;

        Dictionary<int, List<MessageSection>> pageSort = new();
        Dictionary<int, string> pageBake = new();
        for (int i = 0; i < messageParts.Length; i++)
        {
            if (!pageSort.TryAdd(messageParts[i].pageNum, new() { messageParts[i] }))
            {
                pageSort[messageParts[i].pageNum].Add(messageParts[i]);
            }
        }

        foreach(var pair in pageSort)
        {
            string pagePak = string.Empty;
            pair.Value.ForEach(message => pagePak = string.Format("{0}{1}", pagePak, PackageSection(message)));
            if(string.IsNullOrEmpty(pagePak) )
            {
                pagePak = "Empty String";
            }
            pagePak += "<br>";
            pageBake.Add(pair.Key, pagePak);
        }

        foreach(var pair in pageBake)
        {
            pak = string.Format("{0}¬{1}", pak, pair.Value);
        }
        return pak;

        for (int i = 0; i < messageParts.Length; i++)
        {
            pak = string.Format("{0}{1}", pak, PackageSection(messageParts[i]));
        }
        if (string.IsNullOrEmpty(pak))
        {
            pak = "Empty string.";
        }
        return pak + "<br>";
    }

    private string PackageSection(MessageSection messageSection)
    {
        string pak;
        if (string.IsNullOrEmpty(messageSection.text) || string.IsNullOrWhiteSpace(messageSection.text))
        {
            pak = "";
        }
        else
        {
            pak = TextFormatter.ColourText(messageSection.text, messageSection.colour);

            pak = TextFormatter.AlignText(pak, messageSection.alignment);
            if (messageSection.bold)
            {
                pak = TextFormatter.BoldText(pak);
            }
            if (messageSection.italics)
            {
                pak = TextFormatter.ItalicText(pak);
            }
        }
        string newLines = string.Empty;
        for (int i = 0; i < messageSection.newLines; i++)
        {
            newLines += "<br>";
        }
        newLines=TextFormatter.AlignText(newLines, messageSection.alignment);
        int size = messageSection.size == 0 ? 10 : messageSection.size;
        pak = TextFormatter.SizeText(string.Concat(newLines, pak), size);
        return  pak;
    }
}

public class EditableMessage
{
    public string assetName = "Example Message";
    public int backgroundImageIndex;
    public Color backgroundTint = Color.white;
    public Vector2Int dimentions = new(100, 250);
    public MessageSection[] sections;
    public string[] dependsOn = new string[0];
}

[Serializable]
public class MessageSection
{
    public string text = "Sample Text.";
    public int size = 10;
    public bool italics = false;
    public bool bold = false;
    public int newLines = 0;
    public TextAlignment alignment = TextAlignment.Left;
    public Color colour = Color.white;
    public int pageNum = 1;
    public bool lumenLightRequired = false;

    public MessageSection()
    {
        text = "Sample Text.";
        size = 10;
        italics = false;
        bold = false;
        newLines = 0;
        alignment = TextAlignment.Left;
        colour = Color.white;
        pageNum = 1;
        lumenLightRequired = false;
}

    public MessageSection(MessageSection section)
    {
        text = section.text;
        size = section.size;
        italics = section.italics;
        bold = section.bold;
        newLines = section.newLines;
        alignment = section.alignment;
        colour = section.colour;
        pageNum = section.pageNum;
        lumenLightRequired = section.lumenLightRequired;
    }
}
#endif