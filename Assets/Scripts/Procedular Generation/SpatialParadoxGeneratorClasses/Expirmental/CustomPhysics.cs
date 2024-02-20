using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class CustomPhysics : MonoBehaviour
{
    public BoxCollider a;
                        
    public BoxCollider b;
    public float3 rotOffsetA;
    public float3 rotOffsetB;
    float3[] aCorners;
    float3[] bCorners;

    public bool overlap = false;

    float3[] normals = new float3[]
        {
            math.forward(),
            math.up(),
            math.right(),
            math.back(),
            math.down(),
            math.left(),
        };

    private void Update()
    {
        overlap = CheckBox(a, b);
    }

    private void OnDrawGizmos()
    {
        if (aCorners == null || bCorners == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < aCorners.Length; i++)
        {
            Gizmos.DrawSphere(aCorners[i], 0.1f);
        }
        Gizmos.color = Color.green;
        for (int i = 0; i < bCorners.Length; i++)
        {
            Gizmos.DrawSphere(bCorners[i], 0.1f);
        }
    }

    private bool CheckBox(BoxCollider aBox, BoxCollider bBox)
    {
        BoxBounds a = ToBoxBounds(aBox);
        a.oreintation = rotOffsetA;

        BoxBounds b = ToBoxBounds(bBox);
        b.oreintation = rotOffsetB;

        float4x4 ARootMatrix = float4x4.TRS(aBox.transform.position, aBox.transform.rotation, Vector3.one);
        float4x4 BRootMatrix = float4x4.TRS(bBox.transform.position, bBox.transform.rotation, Vector3.one);
        
        float4x4 aWorldMatrix = math.mul(ARootMatrix, a.LocalMatrix);
        float4x4 bWorldMatrix = math.mul(BRootMatrix, b.LocalMatrix);

        InstancedBox instancedBoxA = new(a);
        InstancedBox instancedBoxB = new(a);

        instancedBoxA.TransformNormals(aWorldMatrix);
        instancedBoxA.GetTransformedCorners(aWorldMatrix);

        instancedBoxB.TransformNormals(bWorldMatrix);
        instancedBoxB.GetTransformedCorners(bWorldMatrix);

        aCorners = new float3[8];
        bCorners = new float3[8];

        for (int i = 0; i < 8; i++)
        {
            aCorners[i] = instancedBoxA.corners[i];
            bCorners[i] = instancedBoxB.corners[i];
        }

        //float3[] Anormals = TransformNormals(aWorldMatrix);
        //float3[] Bnormals = TransformNormals(bWorldMatrix);
        //
        //aCorners = GetTransformedCorners(a, aWorldMatrix);
        //bCorners = GetTransformedCorners(b, bWorldMatrix);


        for (int i = 0; i < instancedBoxA.normals.Length; i++)
        {
            SATTest(instancedBoxA.normals[i], aCorners, out float shape1Min, out float shape1Max);
            SATTest(instancedBoxA.normals[i], bCorners, out float shape2Min, out float shape2Max);
            if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
            {
                instancedBoxA.Dispose();
                instancedBoxB.Dispose();
             return  false;
            }
        }

        for (int i = 0; i < instancedBoxB.normals.Length; i++)
        {
            SATTest(instancedBoxB.normals[i], aCorners, out float shape1Min, out float shape1Max);
            SATTest(instancedBoxB.normals[i], bCorners, out float shape2Min, out float shape2Max);
            if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
            {
                instancedBoxA.Dispose();
                instancedBoxB.Dispose();
                return false;
            }
        }
        instancedBoxA.Dispose();
        instancedBoxB.Dispose();

        return true;
    }

    private bool Overlaps(float min1, float max1, float min2, float max2)
    {
        return IsBetweenOrdered(min2, min1, max1) || IsBetweenOrdered(min1, min2, max2);
    }

    private bool IsBetweenOrdered(float val, float lowerBound, float upperBound)
    {
        return lowerBound <= val && val <= upperBound;
    }

    private void SATTest(float3 axis, float3[] ptSet, out float minAlong, out float maxAlong)
    {
        minAlong = float.MaxValue;
        maxAlong = float.MinValue;
        for (int i = 0; i < ptSet.Length; i++)
        {
            float dotVal = math.dot(ptSet[i], axis);
            if(dotVal < minAlong) minAlong = dotVal;
            if(dotVal > maxAlong) maxAlong = dotVal;
        }
    }

    private float3[] TransformNormals(float4x4 matrix)
    {
        float3[] normals = new float3[this.normals.Length];
        Array.Copy(this.normals, normals, normals.Length);
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = matrix.TransformDirection(normals[i]);
        }
        return normals;
    }

    private float3[] GetTransformedCorners(BoxBounds box, float4x4 matrix)
    {
        float3[] corners = new float3[8];
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = i < 4 ? box.Min : box.Max;
        }

        corners[1].x = corners[4].x;
        corners[2].z = corners[4].z;
        corners[3].z = corners[4].z;
        corners[3].x = corners[4].x;

        corners[5].x = corners[0].x;
        corners[6].z = corners[0].z;
        corners[7].z = corners[0].z;
        corners[7].x = corners[0].x;


        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = matrix.TransformPoint(corners[i] - box.LocalCenter);
        }

        return corners;
    }

    private BoxBounds ToBoxBounds(BoxCollider collider)
    {
        return new BoxBounds()
        {
            center = collider.center,
            size = collider.size,
            oreintation = Vector3.zero
        };
    }

}
