using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CapsuleBounds
{
    public Vector3 center;
    public Vector3 oreintation;
    [Min(0)]
    public float radius;
    [Min(0)]
    public float height;
    public float HalfHeight=> height*0.5f;
}

[System.Serializable]
public struct BoxBounds
{
    public Vector3 center;
    public Vector3 oreintation;
    public Vector3 size;
}

[System.Serializable]
public struct Connector
{
    public static Connector Empty = new() { localPosition = Vector3.zero, localRotation = Quaternion.identity };
    public Vector3 localPosition;
    public Quaternion localRotation;

    public Matrix4x4 Matrix => Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
}