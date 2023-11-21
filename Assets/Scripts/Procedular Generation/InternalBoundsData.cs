using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct BoxTransform
{
    public float3 pos;
    public quaternion rotation;
}

[Serializable]
public struct BoxBounds
{
    public Vector3 center;
    public Vector3 oreintation;
    public Vector3 size;
    public float4x4 Matrix => float4x4.TRS(center, Quaternion.Euler(oreintation), Vector3.one);
}

[Serializable]
public struct Connector : System.IEquatable<Connector>
{
    public static Connector Empty = new() { localPosition = Vector3.zero, localRotation = Quaternion.identity, internalIndex = int.MaxValue };
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 position;
    public Quaternion rotation;

    public Vector3 parentPos;
    public int internalIndex;

    public float4x4 Matrix => float4x4.TRS(localPosition, localRotation, Vector3.one);

    public bool Equals(Connector other)
    {
        return internalIndex == other.internalIndex;
    }

    public void UpdateWorldPos(float4x4 transform)
    {
        parentPos = transform.Translation();
        float4x4 ltw = math.mul(transform, Matrix);
        position = ltw.Translation();
        rotation = ltw.Rotation();
    }
}

[Serializable]
public class ConnectorMask
{
    public TunnelSection[] exclude;

    public HashSet<int> excludeRuntime;

    public void Build()
    {
        if (exclude == null)
        {
            excludeRuntime = new HashSet<int>();
            return;
        }
        

        excludeRuntime = new HashSet<int>(exclude.Length);

        for (int i = 0; i < exclude.Length; i++)
        {
            excludeRuntime.Add(exclude[i].GetInstanceID());
        }
        exclude = null;
    }
}

[Serializable]
public class SectionAndConnector
{
    public TunnelSection sectionInstance;
    public int internalIndex = 0;
    public int instanceID;

    public SectionAndConnector(TunnelSection section, int interalIndex)
    {
        sectionInstance = section;
        internalIndex = interalIndex;
        instanceID = section.GetInstanceID();
    }
}