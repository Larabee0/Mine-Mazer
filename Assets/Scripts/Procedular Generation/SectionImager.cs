#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SectionImager : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private string fileName = "MiniMapAsset";
    [SerializeField] private string path = "Assets";
    [SerializeField] private string folderName = "ProceduralGenerationMiniMapTextures";
    public void Photograph()
    {
        if(transform.parent != null)
        {
            fileName = transform.parent.name;
        }

        RenderTexture.active = targetCamera.targetTexture;

        RenderTexture.active = targetCamera.activeTexture;

        Texture2D tex = new(targetCamera.activeTexture.width, targetCamera.activeTexture.height, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(0,0, targetCamera.activeTexture.width, targetCamera.activeTexture.height),0,0);
        tex.Apply();

        string targetPath = path+"/"+folderName;
        if (!Directory.Exists(targetPath))
        {
            AssetDatabase.CreateFolder(path,folderName);
        }

        string localPath = path + "/"+ folderName + "/" + fileName + ".png";
        File.WriteAllBytes(localPath, tex.EncodeToPNG());

        AssetDatabase.Refresh();
    }

}
#endif