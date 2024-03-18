using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;

[CustomEditor(typeof(ProceduralDecorator))]
public class ProceduralDecoratorEditor : Editor
{
    private void OnSceneGUI()
    {
        ProceduralDecorator decorator = (ProceduralDecorator)target;
        
        if (decorator.proceduralPoints.Count > 0)
        {
            for (int i = 0; i < decorator.proceduralPoints.Count; i++)
            {
                if (decorator.selectPoint == i) continue;
                if (DrawProceduralHandle(i, decorator.GetPoint(i)))
                {
                    decorator.selectPoint = i;
                }
            }
            if (decorator.selectPoint < decorator.proceduralPoints.Count && decorator.selectPoint >= 0)
            {
                if (Event.current != null &&
                        Event.current.isKey &&
                        Event.current.type.Equals(EventType.KeyDown) &&
                        Event.current.keyCode == KeyCode.Delete)
                {

                    Event.current.Use();
                    decorator.RemoveSelected();
                }
                if (DrawProceduralHandle(decorator.selectPoint, decorator.GetPoint(decorator.selectPoint)))
                {
                    decorator.FocusSceneCamera();
                }
            }
        }
    }
    
    private bool DrawProceduralHandle(int i,ProDecPoint point)
    {
        ProceduralDecorator decorator = (ProceduralDecorator)target;
        Handles.matrix = point.LTWMatrix;
        Handles.color = Color.gray;
        if (decorator.selectPoint == i)
        {
            Handles.color = Color.cyan;
        }
        Vector3 camDir = (SceneView.currentDrawingSceneView.camera.transform.position- point.WorldPos).normalized;
        camDir = point.LTWMatrix.inverse.MultiplyVector(camDir);
        
        if (Handles.Button(Vector3.zero, Quaternion.LookRotation(camDir), 0.1f, 0.1f, Handles.RectangleHandleCap))
        {
            return true;
        }
        return false;
    }

    public override void OnInspectorGUI()
    {
        
        ProceduralDecorator decorator = (ProceduralDecorator)target;
        if (GUILayout.Button("Focus Scene Camera"))
        {
            decorator.FocusSceneCamera();
        }

        if (GUILayout.Button("Next Point"))
        {
            decorator.IncrementSelection(1);
        }
        if (GUILayout.Button("Previous Point"))
        {
            decorator.IncrementSelection(-1);
        }
        if (GUILayout.Button("Delete Selected"))
        {
            decorator.RemoveSelected();
        }
        DrawDefaultInspector();
        if (GUILayout.Button("Set All To Default Allowed Items"))
        {
            decorator.BulkAllowFilter();
        }
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
