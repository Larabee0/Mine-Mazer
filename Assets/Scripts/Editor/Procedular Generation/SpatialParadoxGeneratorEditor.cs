using Unity.Mathematics;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SpatialParadoxGenerator))]
public class SpatialParadoxGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var script = target as SpatialParadoxGenerator;

        DrawDefaultInspector();
        script.debugging = GUILayout.Toggle(script.debugging, "Transform Debugger");
        if (script.debugging)
        {
            script.transformDebugging = GUILayout.Toggle(script.transformDebugging, "Transform Debug");
        }
        else
        {
            script.transformDebugging = false;
        }
    }
}