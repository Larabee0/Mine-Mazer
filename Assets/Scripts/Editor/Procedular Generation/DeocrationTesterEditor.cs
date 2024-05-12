using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DecorationTester))]
public class DeocrationTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Redecorate"))
        {
            ((DecorationTester)target).Redecorate();
        }
    }
}
