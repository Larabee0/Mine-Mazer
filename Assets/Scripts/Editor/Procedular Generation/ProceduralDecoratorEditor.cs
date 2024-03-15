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
        for (int i = 0; i < decorator.proceduralPoints.Count; i++)
        {
            if (decorator.selectPoint == i) continue;
            DrawProceduralHandle(i,decorator.GetPoint(i));
        }
        DrawProceduralHandle(decorator.selectPoint, decorator.GetPoint(decorator.selectPoint));

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        if (controlID ==
            HandleUtility.nearestControl)
        {
            decorator.selectPoint = HandleUtility.nearestControl;
            //decorator.FocusSceneCamera();
        }

    }

    private void DrawProceduralHandle(int i,ProDecPoint point)
    {
        ProceduralDecorator decorator = (ProceduralDecorator)target;
        Handles.matrix = point.LTWMatrix;
        Handles.color = Color.gray;
        if (decorator.selectPoint == i)
        {
            Handles.color = Color.cyan;
        }
        Handles.CubeHandleCap(i, Vector3.zero, Quaternion.identity, 0.2f, EventType.Repaint);
        Handles.matrix = Matrix4x4.identity;
        Handles.color = Handles.zAxisColor;
        Handles.DrawLine(point.WorldPos, point.WorldPos+( point.Forward * decorator.orientationRayLength));
        Handles.color = Handles.yAxisColor;
        Handles.DrawLine(point.WorldPos, point.WorldPos + (point.Up * decorator.orientationRayLength));
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
