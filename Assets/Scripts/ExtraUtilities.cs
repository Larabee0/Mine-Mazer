using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public static class ExtraUtilities
{
    public static Quaternion BetweenDirections(Vector3 source, Vector3 target)
    {
        Quaternion result;
        var norms = MathF.Sqrt(source.sqrMagnitude * target.sqrMagnitude);
        var real = norms + Vector3.Dot(source, target);
        if (real < Mathf.Epsilon * norms)
        {
            // If source and target are exactly opposite, rotate 180 degrees around an arbitrary orthogonal axis.
            // Axis normalisation can happen later, when we normalise the quaternion.
            result = MathF.Abs(source.x) > MathF.Abs(source.z)
                ? new Quaternion(-source.y, source.x, 0.0f, 0.0f)
                : new Quaternion(0.0f, -source.z, source.y, 0.0f);
        }
        else
        {
            // Otherwise, build quaternion the standard way.
            var axis = Vector3.Cross(source, target);
            result = new Quaternion(axis.x,axis.y,axis.z, real);
        }
        result.Normalize();
        return result;
    }

    public static Vector3 RotatePosition(this Quaternion rotation,Vector3 position)
    {
        var pureQuaternion = new Quaternion(position.x, position.y, position.z, 0);
        pureQuaternion = Conjugate(rotation) * pureQuaternion * rotation;
        return new Vector3(pureQuaternion.x,pureQuaternion.y,pureQuaternion.z);
    }
    public static Quaternion Conjugate(Quaternion value)
    {
        return new Quaternion(-value.x, -value.y, -value.z, value.w);
    }

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
