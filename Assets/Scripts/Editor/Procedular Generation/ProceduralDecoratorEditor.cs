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
        
        if (decorator.proceduralPoints.Count > 0 && decorator.meshRayCaster != null)
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
                        (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.R))
                {

                    Event.current.Use();
                    decorator.RemoveSelected();
                    Repaint();
                }
                if (Event.current != null &&
                        Event.current.isKey &&
                        Event.current.type.Equals(EventType.KeyUp) &&
                        Event.current.keyCode == KeyCode.L)
                {

                    Event.current.Use();
                    decorator.IncrementSelection(1);
                    Repaint();
                }
                if (Event.current != null &&
                        Event.current.isKey &&
                        Event.current.type.Equals(EventType.KeyUp) &&
                        Event.current.keyCode == KeyCode.J)
                {

                    Event.current.Use();
                    decorator.IncrementSelection(-1);
                    Repaint();
                }

                if (Event.current != null &&
                        Event.current.isKey &&
                        Event.current.type.Equals(EventType.KeyDown) &&
                        Event.current.keyCode == KeyCode.F)
                {

                    Event.current.Use();
                    decorator.FocusSceneCamera();
                }
                if (DrawProceduralHandle(decorator.selectPoint, decorator.GetPoint(decorator.selectPoint)))
                {
                    decorator.FocusSceneCamera();
                }
            }
        }
    }
    public bool Is01(float a)
    {
        return a > 0 && a < 1;
    }
    private bool DrawProceduralHandle(int i,ProDecPoint point)
    {
        ProceduralDecorator decorator = (ProceduralDecorator)target;
        Vector3 viewPort = SceneView.currentDrawingSceneView.camera.WorldToViewportPoint(point.WorldPos);

        if(!Is01(viewPort.x) || !Is01(viewPort.y))
        {
            return false;
        }
        if(viewPort.z < 0)
        {
            return false;
        }

        Vector3 camPos = SceneView.currentDrawingSceneView.camera.transform.position;
        Vector3 camDir = decorator.invertHandles ? (point.WorldPos - camPos).normalized : (camPos - point.WorldPos).normalized;
        Handles.matrix = Matrix4x4.TRS( point.WorldPos,Quaternion.LookRotation(camDir),Vector3.one);
        //Handles.matrix = point.LTWMatrix;
        Handles.color = Color.gray;
        if (decorator.selectPoint == i)
        {
            Handles.color = Color.cyan;
        }
        float dst = Mathf.Abs((point.WorldPos - camPos).magnitude);
        if (dst > decorator.handleDrawRadius || dst < decorator.handleCullRadius)
        {
            return false;
        }
        
        
        camDir = Unity.Mathematics.math.inverse(point.LTWMatrix).TransformDirection(camDir);
        // Quaternion.LookRotation(camDir)
        if (Handles.Button(Vector3.zero, Quaternion.identity, 0.1f, 0.1f, Handles.RectangleHandleCap))
        {
            return true;
        }
        return false;
    }

    public override void OnInspectorGUI()
    {
        
        ProceduralDecorator decorator = (ProceduralDecorator)target;

        if (GUILayout.Button("Update Intersections"))
        {
            decorator.UpdateIntersections();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Focus Scene Camera"))
        {
            decorator.FocusSceneCamera();
            Repaint();
        }

        if (GUILayout.Button("Next Point"))
        {
            decorator.IncrementSelection(1);
            Repaint();
        }
        if (GUILayout.Button("Previous Point"))
        {
            decorator.IncrementSelection(-1);
            Repaint();
        }
        if (GUILayout.Button("Delete Selected"))
        {
            decorator.RemoveSelected();
            Repaint();
        }
        DrawDefaultInspector();
        if (GUILayout.Button("Set All To Default Allowed Items"))
        {
            decorator.BulkAllowFilter();
            Repaint();
        }
        if (GUILayout.Button("Rotate all 90"))
        {
            decorator.RotateAll();
            Repaint();
        }
        if (GUILayout.Button("Decorate"))
        {
            decorator.Decorate();

            Repaint();
        }
        if(GUILayout.Button("Clear Decor"))
        {
            decorator.ClearDecror();
            Repaint();
        }
    }
}
