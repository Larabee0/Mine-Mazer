using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class ExtraUtilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Translation(this in float4x4 m) => new(m.c3.x, m.c3.y, m.c3.z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion Rotation(this in float4x4 m) => new(math.orthonormalize(new float3x3(m)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Up(in this float4x4 m) => new(m.c1.x, m.c1.y, m.c1.z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Down(in this float4x4 m) => -Up(m);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Right(in this float4x4 m) => new(m.c0.x, m.c0.y, m.c0.z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Left(in this float4x4 m) => -Right(m);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 RotatePosition(this Quaternion rotation,Vector3 position)
    {
        var pureQuaternion = new Quaternion(position.x, position.y, position.z, 0);
        pureQuaternion = Conjugate(rotation) * pureQuaternion * rotation;
        return new Vector3(pureQuaternion.x,pureQuaternion.y,pureQuaternion.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Conjugate(Quaternion value)
    {
        return new Quaternion(-value.x, -value.y, -value.z, value.w);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ToEuler(this quaternion quaternion)
    {
        float4 q = quaternion.value;
        float3 euler;
        float sinr_cosp = 2f * (q.w * q.x + q.y * q.z);
        float cosr_cosp = 1f - 2f * (q.x * q.x + q.y * q.y);
        euler.x = math.atan2(sinr_cosp, cosr_cosp);
        float sinp = 2 * (q.w * q.y - q.z * q.x);
        if (math.abs(sinp) >= 1f)
        {
            euler.y = math.PI / 2 * math.sign(sinp);
        }
        else
        {
            euler.y = math.asin(sinp);
        }

        float siny_cosp = 2f * (q.w * q.z + q.x * q.y);
        float cosy_cosp = 1f - 2f * (q.y * q.y + q.z * q.z);
        euler.z = math.atan2(siny_cosp, cosy_cosp);
        return euler;
    }

#if UNITY_EDITOR
    public static void DrawWireCapsule(float _radius, float _height)
    {
        Vector3 center = Vector3.zero;
        var pointOffset = (_height - (_radius * 2)) / 2;

        //draw sideways
        Handles.DrawWireArc(center + (Vector3.up * pointOffset), Vector3.left, Vector3.back, -180, _radius);
        Handles.DrawLine(center + new Vector3(0, pointOffset, -_radius), center + new Vector3(0, -pointOffset, -_radius));
        Handles.DrawLine(center + new Vector3(0, pointOffset, _radius), center + new Vector3(0, -pointOffset, _radius));
        Handles.DrawWireArc(center + (Vector3.down * pointOffset), Vector3.left, Vector3.back, 180, _radius);
        //draw frontways
        Handles.DrawWireArc(center + (Vector3.up * pointOffset), Vector3.back, Vector3.left, 180, _radius);
        Handles.DrawLine(center + new Vector3(-_radius, pointOffset, 0), center + new Vector3(-_radius, -pointOffset, 0));
        Handles.DrawLine(center + new Vector3(_radius, pointOffset, 0), center + new Vector3(_radius, -pointOffset, 0));
        Handles.DrawWireArc(center + (Vector3.down * pointOffset), Vector3.back, Vector3.left, -180, _radius);
        //draw center
        Handles.DrawWireDisc(center + Vector3.up * pointOffset, Vector3.up, _radius);
        Handles.DrawWireDisc(center + Vector3.down * pointOffset, Vector3.up, _radius);
    }
#endif
}
