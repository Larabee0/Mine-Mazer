using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralDecorator : MonoBehaviour
{
    [SerializeField] private ProDecPoint[] proceduralPoints;

    private void OnDrawGizmos()
    {
        if(proceduralPoints != null && proceduralPoints.Length > 0)
        {
            for (int i = 0; i < proceduralPoints.Length; i++)
            {
                proceduralPoints[i].UpdateMatrix(transform.localToWorldMatrix);
                Gizmos.matrix = proceduralPoints[i].LTWMatrix;
                Gizmos.color = Color.gray;
                Gizmos.DrawCube(Vector3.zero,new Vector3(0.2f,0.2f,0.2f));
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.green;
                Gizmos.DrawRay(proceduralPoints[i].WorldPos, proceduralPoints[i].Forward);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(proceduralPoints[i].WorldPos, proceduralPoints[i].Up);
            }
        }
    }
}

[System.Serializable]
public struct ProDecPoint
{
    public Item allowedItems;
    public Vector3 localPosition;
    public Vector3 localOrientation;
    public Vector3 WorldPos => internalMatrix.GetPosition();
    public Vector3 Forward => internalMatrix.rotation * Vector3.forward;
    public Vector3 Up => internalMatrix.rotation * Vector3.up;

    private Matrix4x4 internalMatrix;
    public Matrix4x4 LTWMatrix=>internalMatrix;
    private Quaternion internalLocalRotation;
    public Quaternion LocalRotation => internalLocalRotation;

    public void UpdateMatrix(Matrix4x4 matrix)
    {
        internalLocalRotation = Quaternion.Euler(localOrientation);
        internalMatrix = matrix * Matrix4x4.TRS(localPosition, internalLocalRotation, Vector3.one);
        
    }
}