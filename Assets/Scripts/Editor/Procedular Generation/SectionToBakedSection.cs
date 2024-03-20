using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TunnelSection))]
public class SectionToBakedSection : Editor
{
    public override void OnInspectorGUI()
    {
        TunnelSection sectionData = (TunnelSection)target;

        if(GUILayout.Button("Create Baked Data"))
        {
            BakedTunnelSection bakedData = new(sectionData);

            
            
            if (sectionData.gameObject.TryGetComponent(out TunnelSectionData sectionDat))
            {
                sectionDat.bakedData = bakedData;
            }
            else
            {
                
                sectionDat = sectionData.gameObject.AddComponent<TunnelSectionData>();
                
                sectionDat.bakedData = bakedData;
            }
            EditorUtility.SetDirty(sectionData.gameObject);
        }
        DrawDefaultInspector();
    }
}
