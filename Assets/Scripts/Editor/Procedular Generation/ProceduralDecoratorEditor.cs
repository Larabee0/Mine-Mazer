using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralDecorator))]
public class ProceduralDecoratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ProceduralDecorator decorator = (ProceduralDecorator)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Decorate"))
        {
            decorator.Decorate();
        }
        if(GUILayout.Button("Clear Decor"))
        {
            decorator.ClearDecror();
        }
    }
}
