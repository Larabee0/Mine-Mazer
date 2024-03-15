using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ProceduralDecorator : MonoBehaviour
{
    [Header("Generated Points")]
    [SerializeField] private bool showProceduralPoints = true;
    public List<ProDecPoint> proceduralPoints;
    public float orientationRayLength = 0.2f;
    public int selectPoint;
    [SerializeField] private ProDecPoint selectedPoint;
    [Header("Common settings")]
    [SerializeField] private float maxRayDst = 100;
    [SerializeField] private LayerMask layerMasks;
    [SerializeField] private bool showHitNormals = true;
    [Header("Point Raycaster")]
    [SerializeField] bool showBasicRayCaster = true;
    [SerializeField] private Transform raycaster;
    [SerializeField] private float spreadPlus = 180;
    [SerializeField] private float spreadMinus= -180;
    [SerializeField] private int rayCount = 180;
    [SerializeField] private Vector3 boxSize = new(0.2f, 0.2f, 0.2f);

    [Header("Mesh Raycaster")]
    [SerializeField] bool showMeshRayCaster = true;
    [SerializeField] bool meshFaceCasts = true;
    [SerializeField] bool meshVertexCasts = true;
    [SerializeField] private MeshFilter meshRayCaster;


    private List<Vector3> vertices =new() ;
    private List<Vector3> normals = new();
    private List<int> indices = new();

    public void Decorate()
    {
        if (showMeshRayCaster && meshRayCaster != null)
        {
            SubMeshDescriptor subMesh = GatherMeshData();

            if (meshFaceCasts)
            {
                for (int i = 0; i < subMesh.indexCount; i += 3)
                {
                    float3x3 vertices = new()
                    {
                        c0 = this.vertices[i],
                        c1 = this.vertices[i + 1],
                        c2 = this.vertices[i + 2]
                    };
                    float3x3 normals = new()
                    {
                        c0 = this.normals[i],
                        c1 = this.normals[i + 1],
                        c2 = this.normals[i + 2]
                    };
                    Vector3 faceNormal = CalcNormalOfFace(vertices, normals);
                    Vector3 faceCenter = (vertices[0] + vertices[1] + vertices[2]) / 3;
                    Vector3 pos = meshRayCaster.transform.TransformPoint(faceCenter);
                    Vector3 normal = meshRayCaster.transform.TransformDirection(faceNormal);

                    if (RayCast(pos, normal, out RaycastHit hitInfo))
                    {
                        proceduralPoints.Add(new()
                        {
                            localPosition = meshRayCaster.transform.InverseTransformPoint(hitInfo.point),
                            localOrientation = Quaternion.LookRotation(meshRayCaster.transform.InverseTransformDirection(hitInfo.normal)).eulerAngles,
                        });
                    }
                }
            }

            if (meshVertexCasts)
            {
                indices.ForEach(index =>
                {
                    Vector3 pos = meshRayCaster.transform.TransformPoint(vertices[index]);
                    Vector3 normal = meshRayCaster.transform.TransformDirection(normals[index]);

                    if (RayCast(pos, normal, out RaycastHit hitInfo))
                    {
                        proceduralPoints.Add(new()
                        {
                            localPosition = meshRayCaster.transform.InverseTransformPoint(hitInfo.point),
                            localOrientation = Quaternion.LookRotation(meshRayCaster.transform.InverseTransformDirection(hitInfo.normal)).eulerAngles,
                        });
                    }

                });
            }
        }

        if (showBasicRayCaster && raycaster != null && meshRayCaster != null)
        {
            for (int i = 0; i < rayCount; i++)
            {
                float t = Mathf.InverseLerp(0, rayCount - 1, i);
                float angle = Mathf.Lerp(spreadMinus, spreadPlus, t);
                Vector3 rayDir = raycaster.TransformDirection(Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up);
                if (RayCast(raycaster.position, rayDir, out RaycastHit hitInfo))
                {
                    proceduralPoints.Add(new()
                    {
                        localPosition = meshRayCaster.transform.InverseTransformPoint(hitInfo.point),
                        localOrientation = Quaternion.LookRotation(meshRayCaster.transform.InverseTransformDirection(hitInfo.normal)).eulerAngles,
                    });
                }
            }
        }
    }

    public void ClearDecror()
    {
        proceduralPoints.Clear();
    }

    private Vector3 CalcNormalOfFace(float3x3 pPositions, float3x3 pNormals )
    {
        Vector3 p0 = pPositions[1] - pPositions[0];
        Vector3 p1 = pPositions[2] - pPositions[0];
        Vector3 faceNormal = Vector3.Cross(p0, p1);

        Vector3 vertexNormal = (pNormals[0] + pNormals[1] + pNormals[2]) / 3; // or you can average 3 normals.
        float dot = Vector3.Dot(faceNormal, vertexNormal);

        return (dot < 0.0f) ? -faceNormal : faceNormal;
    }

    private int oldSelect;
    private void OnValidate()
    {
        if (proceduralPoints.Count > 0)
        {
            selectPoint = Mathf.Clamp(selectPoint, 0, proceduralPoints.Count - 1);
            if (selectPoint != oldSelect)
            {
                selectedPoint = proceduralPoints[selectPoint];
                oldSelect = selectPoint;
            }
            proceduralPoints[selectPoint] = selectedPoint;
        }
        else
        {
            oldSelect = -1;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(showMeshRayCaster&&meshRayCaster != null)
        {
            Gizmos.matrix = meshRayCaster.transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            SubMeshDescriptor subMesh = GatherMeshData();

            Gizmos.matrix = Matrix4x4.identity;
            if (meshFaceCasts)
            {
                for (int i = 0; i < subMesh.indexCount; i += 3)
                {
                    float3x3 vertices = new()
                    {
                        c0 = this.vertices[i],
                        c1 = this.vertices[i + 1],
                        c2 = this.vertices[i + 2]
                    };
                    float3x3 normals = new()
                    {
                        c0 = this.normals[i],
                        c1 = this.normals[i + 1],
                        c2 = this.normals[i + 2]
                    };
                    Vector3 faceNormal = CalcNormalOfFace(vertices, normals);
                    Vector3 faceCenter = (vertices[0] + vertices[1] + vertices[2]) / 3;
                    Vector3 pos = meshRayCaster.transform.TransformPoint(faceCenter);
                    Vector3 normal = meshRayCaster.transform.TransformDirection(faceNormal);

                    if (RayCast(pos, normal, out RaycastHit hitInfo))
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(pos, hitInfo.point);
                        if (showHitNormals)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(hitInfo.point, hitInfo.normal);
                        }
                    }
                }
            }

            if (meshVertexCasts)
            {
                indices.ForEach(index =>
                {
                    Vector3 pos = meshRayCaster.transform.TransformPoint(vertices[index]);
                    Vector3 normal = meshRayCaster.transform.TransformDirection(normals[index]);



                    if (RayCast(pos, normal, out RaycastHit hitInfo))
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(pos, hitInfo.point);
                        if (showHitNormals)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(hitInfo.point, hitInfo.normal);
                        }
                    }

                });
            }
        }


        if (showBasicRayCaster&&raycaster != null)
        {
            Gizmos.matrix = raycaster.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, boxSize);

            Gizmos.matrix = Matrix4x4.identity;
            for (int i = 0; i < rayCount; i++)
            {
                float t = Mathf.InverseLerp(0, rayCount - 1, i);
                float angle = Mathf.Lerp(spreadMinus, spreadPlus, t);
                Vector3 rayDir = raycaster.TransformDirection(Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up);
                if(RayCast(raycaster.position,rayDir,out RaycastHit hitInfo))
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(raycaster.position, hitInfo.point);
                    if (showHitNormals)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(hitInfo.point, hitInfo.normal);
                    }
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(raycaster.position, rayDir*maxRayDst);
                }
            }
        }

        if (showProceduralPoints && proceduralPoints != null && proceduralPoints.Count > 0 && meshRayCaster != null)
        {
            selectPoint = Mathf.Clamp(selectPoint, 0, proceduralPoints.Count - 1);
            for (int i = 0; i < proceduralPoints.Count; i++)
            {
                var point = proceduralPoints[i];
                if (selectPoint == i)
                {
                    continue;
                }
                DrawPoint(i, point);
            }
            DrawPoint(selectPoint, proceduralPoints[selectPoint]);
        }
    }

    public ProDecPoint GetPoint(int i)
    {
        ProDecPoint point = proceduralPoints[i];
        point.UpdateMatrix(meshRayCaster.transform.localToWorldMatrix);
        return point;
    }

    private void DrawPoint(int i, ProDecPoint point)
    {
        point.UpdateMatrix(meshRayCaster.transform.localToWorldMatrix);
        Gizmos.matrix = point.LTWMatrix;
        Gizmos.color = Color.gray;
        if (selectPoint == i)
        {
            Gizmos.color = Color.cyan;
        }
        Gizmos.DrawCube(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(point.WorldPos, point.Forward * orientationRayLength);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(point.WorldPos, point.Up * orientationRayLength);
    }

    private SubMeshDescriptor GatherMeshData()
    {
        var subMesh = meshRayCaster.sharedMesh.GetSubMesh(0);

        if (vertices.Count != meshRayCaster.sharedMesh.vertexCount)
        {
            vertices.Capacity = meshRayCaster.sharedMesh.vertexCount;
            meshRayCaster.sharedMesh.GetVertices(vertices);
        }
        if (normals.Count != meshRayCaster.sharedMesh.vertexCount)
        {
            normals.Capacity = meshRayCaster.sharedMesh.vertexCount;
            meshRayCaster.sharedMesh.GetNormals(normals);
        }
        if (indices.Count != subMesh.indexCount)
        {
            indices.Capacity = subMesh.indexCount;
            meshRayCaster.sharedMesh.GetIndices(indices, 0);
        }

        return subMesh;
    }

    

    private bool RayCast(Vector3 pos, Vector3 dir, out RaycastHit hitInfo)
    {
        return Physics.Raycast(pos, dir, out hitInfo, maxRayDst, layerMasks.value);
    }

    public void RemoveSelected()
    {
        if (proceduralPoints == null || proceduralPoints.Count == 0) return;
        Undo.RegisterCompleteObjectUndo(this, "Remove Selected");
        proceduralPoints.RemoveAt(selectPoint);
        selectPoint = Mathf.Clamp(selectPoint, 0, proceduralPoints.Count - 1);
        SceneView.RepaintAll();
    }

    public void FocusSceneCamera()
    {
        if (meshRayCaster == null) return;
        Vector3 pos = meshRayCaster.transform.TransformPoint(proceduralPoints[selectPoint].localPosition);
        SceneView.lastActiveSceneView.Frame(new Bounds(pos, Vector3.one), false);
    }

    public void IncrementSelection(int increment)
    {
        int next = selectPoint + increment;
        next = next >= proceduralPoints.Count ?  0: next;
        next = next < 0 ? proceduralPoints.Count -1 : next;
        selectPoint = next;
        FocusSceneCamera();
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