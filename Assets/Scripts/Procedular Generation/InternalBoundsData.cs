using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct BoxTransform
{
    public float3 pos;
    public quaternion rot;

    public float4x4 Matrix => float4x4.TRS(pos, rot,new(1));
    public float4x4 RotMatrix => float4x4.TRS(float3.zero, rot,new(1));
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
    public float3 center;
    public float3 oreintation;
    public float3 size;
    public float4x4 LocalMatrix => float4x4.TRS(center, Quaternion.Euler((oreintation)), Vector3.one);
    public float4x4 RotationMatrix => float4x4.TRS(float3.zero, Quaternion.Euler((oreintation)), new(1));
    public float3 LocalCenter => center;
    public float3 LocalOreintation => oreintation;
    public float3 Extents => size * 0.5f;
    public float3 Min => center- Extents; 
    public float3 Max => center+ Extents;
    public float3 MinLocal => -Extents;
    public float3 MaxLocal => +Extents;

    public float4x4 GetCentreMatrix(float4x4 rootMatrix)
    {
        float3 center = rootMatrix.TransformPoint(this.center);
        return float4x4.TRS(center,Quaternion.Euler(oreintation), Vector3.one);
    }
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

public struct BurstConnectorPair
{
    public int id;
    public float4x4 primaryMatrix;
    public BurstConnector primary;
    public BurstConnector secondary;
}

public struct BurstConnector
{
    public float4x4 localMatrix;
    public float3 parentPos;
    public float3 position;
    public quaternion rotation;

    public static implicit operator BurstConnector(Connector connector) => new(connector);
    public static implicit operator Connector(BurstConnector connector) => new()
    { 
        internalIndex = -1,
        localPosition = connector.localMatrix.Translation(),
        parentPos = connector.parentPos,
        localRotation = connector.localMatrix.Rotation(),
        rotation = connector.rotation,
        position = connector.position
    };

    public BurstConnector(Connector connector) 
    {
        localMatrix = float4x4.TRS(connector.localPosition, connector.localRotation, new(1));
        parentPos = new(0);
        position = new(0);
        rotation = quaternion.identity;
    }

    
}

public static class ConnectoExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateWorldPos(this ref BurstConnector connector, float4x4 parentMatrix)
    {
        connector.parentPos = parentMatrix.Translation();
        float4x4 ltw = math.mul(parentMatrix, connector.localMatrix);
        connector.position = ltw.Translation();
        connector.rotation = ltw.Rotation();
    }

    public static void TransformNormals(this ref InstancedBox box, in float4x4 matrix)
    {
        for (int i = 0; i < box.normals.Length; i++)
        {
            box.normals[i] = matrix.TransformDirection(BoxCheckJob.normals[i]);
        }
    }

    public static void GetTransformedCorners(this ref InstancedBox box, in float4x4 matrix)
    {
        for (int i = 0; i < box.corners.Length; i++)
        {
            box.corners[i] = i < 4 ? box.boxBounds.MinLocal : box.boxBounds.MaxLocal;
        }

        float4 minMax = new(box.boxBounds.MaxLocal.x, box.boxBounds.MaxLocal.z, box.boxBounds.MinLocal.x, box.boxBounds.MinLocal.z);

        box.corners.ElementAt(1).x = minMax.x;
        box.corners.ElementAt(2).z = minMax.y;
        box.corners.ElementAt(3).z = minMax.y;
        box.corners.ElementAt(3).x = minMax.x;

        box.corners.ElementAt(5).x = minMax.z;
        box.corners.ElementAt(6).z = minMax.w;
        box.corners.ElementAt(7).z = minMax.w;
        box.corners.ElementAt(7).x = minMax.z;

        for (int i = 0; i < box.corners.Length; i++)
        {
            box.corners[i] = matrix.TransformPoint(box.corners[i]);
        }
    }

    public static void GetTransformedCorners(this ref InstancedBox box, in float4x4 matrix, in float4x4 rotation)
    {
        for (int i = 0; i < box.corners.Length; i++)
        {
            box.corners[i] = i < 4 ? box.boxBounds.MinLocal : box.boxBounds.MaxLocal;
        }

        float4 minMax = new(box.boxBounds.MaxLocal.x, box.boxBounds.MaxLocal.z, box.boxBounds.MinLocal.x, box.boxBounds.MinLocal.z);

        box.corners.ElementAt(1).x = minMax.x;
        box.corners.ElementAt(2).z = minMax.y;
        box.corners.ElementAt(3).z = minMax.y;
        box.corners.ElementAt(3).x = minMax.x;

        box.corners.ElementAt(5).x = minMax.z;
        box.corners.ElementAt(6).z = minMax.w;
        box.corners.ElementAt(7).z = minMax.w;
        box.corners.ElementAt(7).x = minMax.z;

        for (int i = 0; i < box.corners.Length; i++)
        {
            box.corners[i] = matrix.TransformPoint( rotation.TransformPoint(box.corners[i]));
        }
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
    public MapTreeElement element;
    public TunnelSection SectionInstance =>element.sectionInstance;
    public int internalIndex = 0;
    public int InstanceID => element.UID;

    public SectionAndConnector(MapTreeElement element, int interalIndex)
    {
        this.element = element;
        internalIndex = interalIndex;
    }
}