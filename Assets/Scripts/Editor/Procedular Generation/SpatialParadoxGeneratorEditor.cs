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
            bool2 cur = new(script.initialAreaDebugging, script.transformDebugging);
            script.initialAreaDebugging = GUILayout.Toggle(script.initialAreaDebugging, "Inital Area Debug");
            script.transformDebugging = GUILayout.Toggle(script.transformDebugging, "Transform Debug");
            
            if (cur[0] != script.initialAreaDebugging && script.initialAreaDebugging && cur[1])
            {
                script.transformDebugging = false;
            }
            if (cur[1] != script.transformDebugging && script.transformDebugging && cur[0])
            {
                script.initialAreaDebugging = false;
            }
        }
        else
        {
            script.initialAreaDebugging = false;
            script.transformDebugging = false;
        }
    }
}