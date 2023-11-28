using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SectionImager))]
public class SectionImagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Photograph"))
        {
            ((SectionImager)target).Photograph();
        }
    }
}
