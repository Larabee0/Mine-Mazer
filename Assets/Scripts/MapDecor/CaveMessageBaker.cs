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
    public Vector2Int dimentions = new (100,250);
    public string assetName = "Example Message";
    public string[] dependsOn;
    public string prefabsDirectory = "";
    public string folderName = "";
    
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
                MessageContainer container = new(messageContainer);

                container.text.text = PackageText();

                container.root.style.width = dimentions.x;
                container.root.style.height = dimentions.y;
                if(backgroundImageIndex < 0)
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

    public void BakeDown()
    {
        MessageAsset asset = new()
        {
            assetName = assetName,
            backgroundIndex = backgroundImageIndex,
            backgroundTint = backgroundTint,
            dimentions = dimentions,
            messageText = PackageText(),
            dependsOn = dependsOn,
        };

        EditableMessage editableMessage = new()
        {
            assetName = assetName,
            backgroundImageIndex = backgroundImageIndex,
            backgroundTint = backgroundTint,
            dimentions = dimentions,
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

    private string PackageText()
    {
        string pak = string.Empty;
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

    public MessageSection()
    {
        text = "Sample Text.";
        size = 10;
        italics = false;
        bold = false;
        newLines = 0;
        alignment = TextAlignment.Left;
        colour = Color.white;
    }
}
#endif