using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaveMessageBaker))]
public class CaveMessageBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CaveMessageBaker baker = (CaveMessageBaker)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Force Preview"))
        {
            baker.Preview();
        }
        if (GUILayout.Button("Bake"))
        {
            baker.BakeDown();
        }
    }
}
