using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(SectionBaker))]
public class SectionBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SectionBaker baker = (SectionBaker)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Bake"))
        {
            Bake(baker);
        }
        if(GUILayout.Button("Swap Parent Child Position"))
        {
            (baker.transform.GetChild(0).transform.localPosition, baker.transform.localPosition) = (baker.transform.localPosition, baker.transform.GetChild(0).transform.localPosition);
        }
    }

    private void Bake(SectionBaker baker)
    {
        if(baker.SectionModel == null) { baker.SectionModel = baker.transform.GetChild(0).gameObject;Debug.LogWarning("Section model unassigned getting first child. Check and try bake again."); return; }
        List<BoxBounds> boxBounds = new();
        for (int i = 0; i < baker.BoundsObjects.Length; i++)
        {
            GameObject go = baker.BoundsObjects[i];
            if (go.TryGetComponent(out Collider collider))
            {
                if (collider is BoxCollider boxCollider)
                {
                    boxBounds.Add(new BoxBounds()
                    {
                        center = boxCollider.center + go.transform.localPosition,
                        size = go.transform.localScale,
                        oreintation = go.transform.localRotation.eulerAngles,
                    });
                }
            }
        }
        GameObject prefab = Instantiate(baker.SectionModel);
        prefab.name = baker.name + " Prefab";
        if (prefab.TryGetComponent(out TunnelSection section))
        {
            Destroy(section);
        }
        section = prefab.AddComponent<TunnelSection>();
        if (baker.ConnectorObjects != null)
        {
            section.connectors = new Connector[baker.ConnectorObjects.Length];

            section.connectorPairs = new(section.connectors.Length);
            for (int i = 0; i < baker.ConnectorObjects.Length; i++)
            {
                section.connectors[i] = new Connector()
                {
                    internalIndex = i,
                    localPosition = baker.ConnectorObjects[i].transform.localPosition,
                    localRotation = baker.ConnectorObjects[i].transform.localRotation
                };
                section.connectorPairs.Add(i,null);
            }
        }


        boxBounds.ForEach(box =>
        {
            GameObject empty = new("BoundingBox");
            empty.transform.SetParent(section.transform, false);
            empty.transform.SetLocalPositionAndRotation(box.center, Quaternion.Euler(box.oreintation));
            BoxCollider collider = empty.AddComponent<BoxCollider>();
            collider.size = box.size;
            empty.layer = (int)Mathf.Log(baker.tunnelSectionLayerMask.value, 2);
        });
        
        section.boundingBoxes = boxBounds.ToArray();

        for (int i = 0; i < baker.ConnectorTriggers.Length; i++)
        {
            Instantiate(baker.ConnectorTriggers[i], section.transform);
        }

        if(baker.SaveToPrefabs)
        {
            CreatePrefab(section.gameObject, baker);
            DestroyImmediate(section.gameObject);
        }
    }

    private static void CreatePrefab(GameObject asset, SectionBaker baker)
    {
        string targetPath = baker.prefabsDirectory + "/" + baker.folderName;
        if (!Directory.Exists(baker.prefabsDirectory))
        {
            string[] folders = baker.prefabsDirectory.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < folders.Length; i++)
            {
                AssetDatabase.CreateFolder(folders[i - 1], folders[i]);
            }
        }
        if (!Directory.Exists(targetPath))
        {
            AssetDatabase.CreateFolder(baker.prefabsDirectory, baker.folderName);
        }

        string localPath = targetPath + "/" + asset.name + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        if(localPath == null)
        {
            Debug.LogError("local path invalid");
            return;
        }
        PrefabUtility.SaveAsPrefabAsset(asset, localPath, out bool success);
        Debug.LogFormat("Asset Create successful? {0}", success);
    }
}
