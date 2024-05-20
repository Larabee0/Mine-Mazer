using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class PlayerSettings : MonoBehaviour
{
    public static PlayerSettings instance;
    public static PlayerSettings Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected Player Settings instance not found. Order of operations issue? Or Player Settings is disabled/missing.");
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


    private string userSettingsPath;

    public Action OnSettingsChanged;

    public SettingsForDisk userSettings;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool alwaysReset = false;
    [SerializeField] private bool neverSave = false;
#endif

    private void Awake()
    {
        userSettingsPath = Path.Combine(Application.persistentDataPath, "userSettings.xml");
        if(instance != null )
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        userSettings = new();
    }

    private void Start()
    {
#if UNITY_EDITOR
        if(alwaysReset)
        {
            return;
        }
#endif
        if (File.Exists(userSettingsPath))
        {
            XmlSerializer reader = new(typeof(SettingsForDisk));
            StreamReader file = new(userSettingsPath);
            userSettings = (SettingsForDisk)reader.Deserialize(file);
            file.Close();
            if (userSettings == null)
            {
                Debug.LogWarning("Read User Settings file, but failed to deserialize it!");
                userSettings = new();
            }
        }

    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        if (neverSave)
        {
            return;
        }
#endif
        userSettings ??= new ();
        XmlSerializer writer = new(typeof(SettingsForDisk));
        FileStream file = File.Create(userSettingsPath);
        writer.Serialize(file, userSettings);
        file.Close();
    }
}

[Serializable]
public class SettingsForDisk
{
    public bool cameraHInvert =false;
    public bool cameraVInvert = false;
    public bool moveHInvert = false;
    public bool moveVInvert = false;
    public bool skipIntro = false;
    public bool skipTutorial = false;
    public float cameraSens = 1;
    public float constrastAdj = 0;
    public float postExposureAdj = 1.1f;
}