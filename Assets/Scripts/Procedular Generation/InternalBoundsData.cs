using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct BoxTransform
{
    public float3 pos;
    public quaternion rot;
}

public struct SectionDstData
{
    public float sqrDst;
    public int dst;

    public SectionDstData(float lastSqrDst, int dst)
    {
        this.sqrDst = lastSqrDst;
        this.dst = dst;
    }
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
public struct Connector : IEquatable<Connector>
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

public struct BurstConnector
{
    public float4x4 localMatrix;
    public float3 parentPos;
    public float3 position;
    public quaternion rotation;

    public BurstConnector(Connector connector) 
    {
        localMatrix = float4x4.TRS(connector.localPosition, connector.localRotation, new(1));
        parentPos = new(0);
        position = new(0);
        rotation = quaternion.identity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateWorldPos(ref BurstConnector connector, float4x4 parentMatrix)
    {
        connector.parentPos = parentMatrix.Translation();
        float4x4 ltw = math.mul(parentMatrix, connector.localMatrix);
        connector.position = ltw.Translation();
        connector.rotation = ltw.Rotation();
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