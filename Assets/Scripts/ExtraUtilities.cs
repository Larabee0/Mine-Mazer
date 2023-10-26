using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ExtraUtilities
{
    public static void DrawWireCapsule(float _radius, float _height)
    {
        Vector3 center = Vector3.zero;
        var pointOffset = (_height - (_radius * 2)) / 2;
        
        //draw sideways
        Handles.DrawWireArc(center+(Vector3.up * pointOffset), Vector3.left, Vector3.back, -180, _radius);
        Handles.DrawLine(center + new Vector3(0, pointOffset, -_radius),center +  new Vector3(0, -pointOffset, -_radius));
        Handles.DrawLine(center + new Vector3(0, pointOffset, _radius), center + new Vector3(0, -pointOffset, _radius));
        Handles.DrawWireArc(center + (Vector3.down * pointOffset), Vector3.left, Vector3.back, 180, _radius);
        //draw frontways
        Handles.DrawWireArc(center + (Vector3.up * pointOffset), Vector3.back, Vector3.left, 180, _radius);
        Handles.DrawLine(center + new Vector3(-_radius, pointOffset, 0),center +  new Vector3(-_radius, -pointOffset, 0));
        Handles.DrawLine(center + new Vector3(_radius, pointOffset, 0), center + new Vector3(_radius, -pointOffset, 0));
        Handles.DrawWireArc(center + (Vector3.down * pointOffset), Vector3.back, Vector3.left, -180, _radius);
        //draw center
        Handles.DrawWireDisc(center + Vector3.up * pointOffset, Vector3.up, _radius);
        Handles.DrawWireDisc(center + Vector3.down * pointOffset, Vector3.up, _radius);
    }
}
