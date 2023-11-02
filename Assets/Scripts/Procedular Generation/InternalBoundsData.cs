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
    public Matrix4x4 Matrix => Matrix4x4.TRS(center, Quaternion.Euler(oreintation), Vector3.one);
}

[System.Serializable]
public struct Connector
{
    public static Connector Empty = new() { localPosition = Vector3.zero, localRotation = Quaternion.identity };
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 position;
    public Quaternion rotation;

    public Vector3 parentPos;
    public int internalIndex;

    public Matrix4x4 Matrix => Matrix4x4.TRS(localPosition, localRotation, Vector3.one);

    public Vector3 Forward => rotation * Vector3.forward;
    public Vector3 Back => rotation * Vector3.back;


    public void UpdateWorldPos(Matrix4x4 transform)
    {
        parentPos = transform.GetPosition();
        Matrix4x4 ltw = transform * Matrix;
        position = ltw.GetPosition();
        rotation = ltw.rotation;
    }
}