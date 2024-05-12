using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
public class ProceduralDecorator : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField]private MapResource[] resources;
    [SerializeField] private float coverage = 1f;
    [Header("Generated Points")]
    [SerializeField] private bool showProceduralPoints = true;
    public List<ProDecPoint> proceduralPoints;
    public float orientationRayLength = 0.2f;
    public int selectPoint;
    [SerializeField] private ProDecPoint selectedPoint;
    [Header("Common settings")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(90, 0, 0);
    [SerializeField] private float maxRayDst = 100;
    [SerializeField] private LayerMask layerMasks;
    [SerializeField] private bool showHitNormals = true;
    public bool invertHandles = true;
    public float handleDrawRadius = 100;
    public float handleCullRadius = 0.01f;
    [SerializeField] private Item defaultAllowedItems;
    [Header("Point Raycaster")]
    [SerializeField] bool enableBasicRayCaster = true;
    [SerializeField] private Transform raycaster;
    [SerializeField] private float spreadPlus = 180;
    [SerializeField] private float spreadMinus= -180;
    [SerializeField] private int rayCount = 180;
    [SerializeField] private Vector3 boxSize = new(0.2f, 0.2f, 0.2f);

    [Header("Mesh Raycaster")]
    [SerializeField] bool enableMeshRayCaster = true;
    [SerializeField] bool meshFaceCasts = true;
    [SerializeField] bool meshVertexCasts = true;
    public MeshFilter meshRayCaster;


    private List<Vector3> vertices =new() ;
    private List<Vector3> normals = new();
    private List<int> indices = new();

    private void Start()
    {
        List<ProDecPoint> points = new(proceduralPoints);
        while((float)points.Count / (float)proceduralPoints.Count > 1f- coverage)
        {
            int index = Random.Range(0, points.Count);
            ProDecPoint point = points[index];
            points.RemoveAt(index);
            point.UpdateMatrix(meshRayCaster.transform.localToWorldMatrix);
            MapResource trs = Instantiate(resources[Random.Range(0, resources.Length)], point.WorldPos, Quaternion.identity, transform);
            

            if(trs.ItemStats.type == Item.Versicolor)
            {
                // trs.transform.up = point.Forward;
                // trs.transform.forward = point.Up;

                trs.transform.localRotation = Quaternion.LookRotation(point.Up,Vector3.forward);
                continue;
            }
            trs.transform.up = point.Up;
            // trs.transform.RotateAroundLocal(Vector3.up, Random.Range(0, 359f));
            trs.transform.Rotate(Vector3.up, Random.Range(0, 359f) , Space.Self);

        }
    }

    public void Decorate()
    {
        if (enableMeshRayCaster && meshRayCaster != null)
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
                    Vector3 faceNormal = FaceNormal(vertices, normals);
                    Vector3 faceCenter = (vertices[0] + vertices[1] + vertices[2]) / 3;
                    Vector3 pos = meshRayCaster.transform.TransformPoint(faceCenter);
                    Vector3 normal = meshRayCaster.transform.TransformDirection(faceNormal);

                    if (RayCast(pos, normal, out RaycastHit hitInfo))
                    {
                        proceduralPoints.Add(new()
                        {
                            allowedItems = defaultAllowedItems,
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
                            allowedItems = defaultAllowedItems,
                            localPosition = meshRayCaster.transform.InverseTransformPoint(hitInfo.point),
                            localOrientation = Quaternion.LookRotation(meshRayCaster.transform.InverseTransformDirection(hitInfo.normal)).eulerAngles,
                        });
                    }

                });
            }
        }

        if (enableBasicRayCaster && raycaster != null && meshRayCaster != null)
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
                        allowedItems = defaultAllowedItems,
                        localPosition = meshRayCaster.transform.InverseTransformPoint(hitInfo.point),
                        localOrientation = Quaternion.LookRotation(meshRayCaster.transform.InverseTransformDirection(hitInfo.normal)).eulerAngles,
                    });
                }
            }
        }

        SceneView.RepaintAll();
    }

    public void ClearDecror()
    {
        proceduralPoints.Clear();
        SceneView.RepaintAll();
    }

    private Vector3 FaceNormal(float3x3 pPositions, float3x3 pNormals )
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
        if(enableMeshRayCaster&&meshRayCaster != null)
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
                    Vector3 faceNormal = FaceNormal(vertices, normals);
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
                    //else
                    //{
                    //    Gizmos.color = Color.yellow;
                    //    Gizmos.DrawRay(raycaster.position, normal * maxRayDst);
                    //}
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

                    //else
                    //{
                    //    Gizmos.color = Color.yellow;
                    //    Gizmos.DrawRay(raycaster.position, normal * maxRayDst);
                    //}
                });
            }
        }


        if (enableBasicRayCaster&&raycaster != null)
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

    public void UpdateIntersections()
    {
        for (int i = 0; i < proceduralPoints.Count; i++)
        {
            bool intersect = false;
            var point = proceduralPoints[i];
            point.UpdateMatrix(meshRayCaster.transform.localToWorldMatrix);
            for (int j = i; j < proceduralPoints.Count; j++)
            {
                if (i == j) continue;
                var point2 = proceduralPoints[j];
                point2.UpdateMatrix(meshRayCaster.transform.localToWorldMatrix);
                intersect = !CustomPhysics.CheckBox(point, point2, false);
                if (!intersect) break;
            }
            point.intersection = intersect;
            proceduralPoints[i] = point;

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
        
        if (!point.intersection)
        {
            Gizmos.color = Color.red;
            if (selectPoint == i)
            {
                Gizmos.color = Color.green;
            }
        }
        else
        {
            Gizmos.color = Color.gray;
            if (selectPoint == i)
            {
                Gizmos.color = Color.cyan;
            }
        }
        Gizmos.DrawCube(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(point.WorldPos, point.Forward * orientationRayLength);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(point.WorldPos, point.Up * orientationRayLength);
    }

    private SubMeshDescriptor GatherMeshData()
    {
        var subMesh = meshRayCaster.sharedMesh.GetSubMesh(0);

        if (vertices.Count != meshRayCaster.sharedMesh.vertexCount)
        {
            meshRayCaster.sharedMesh.GetVertices(vertices);
        }
        if (normals.Count != meshRayCaster.sharedMesh.vertexCount)
        {
            meshRayCaster.sharedMesh.GetNormals(normals);
        }
        if (indices.Count != subMesh.indexCount)
        {
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
        selectedPoint = proceduralPoints[selectPoint];
        SceneView.RepaintAll();
        
    }

    public void FocusSceneCamera()
    {
        if (meshRayCaster == null || proceduralPoints.Count == 0 ) return;
        Vector3 pos = meshRayCaster.transform.TransformPoint(proceduralPoints[selectPoint].localPosition);
        SceneView.lastActiveSceneView.Frame(new Bounds(pos, Vector3.one), false);
    }

    public void IncrementSelection(int increment)
    {
        if (proceduralPoints.Count == 0) return;
        int next = selectPoint + increment;
        next = next >= proceduralPoints.Count ?  0: next;
        next = next < 0 ? proceduralPoints.Count -1 : next;
        selectPoint = next;
        selectedPoint = proceduralPoints[selectPoint];
        FocusSceneCamera();
    }

    public void BulkAllowFilter()
    {
        for (int i = 0; i < proceduralPoints.Count; i++)
        {
            ProDecPoint p = proceduralPoints[i];
            p.allowedItems = defaultAllowedItems;
            proceduralPoints[i] = p;
        }
    }

    public void RotateAll()
    {
        Undo.RegisterCompleteObjectUndo(this, "Rotate All");
        for (int i = 0; i < proceduralPoints.Count; i++)
        {
            ProDecPoint p = proceduralPoints[i];
            p.localOrientation += rotationOffset;

            proceduralPoints[i] = p;
        }
        SceneView.RepaintAll();
    }
}
#endif


[System.Serializable]
public struct ProDecPoint
{
    public bool intersection;
    public Item allowedItems;
    public Vector3 localPosition;
    public Vector3 localOrientation;
    public Vector3 WorldPos => internalMatrix.Translation();
    public Vector3 Forward => (Quaternion)internalMatrix.Rotation() * Vector3.up;
    public Vector3 Up => (Quaternion)internalMatrix.Rotation() * Vector3.forward;

    private float4x4 internalMatrix;
    public float4x4 LTWMatrix =>internalMatrix;
    private Quaternion internalLocalRotation;
    public Quaternion LocalRotation => internalLocalRotation;

    public void UpdateMatrix(float4x4 matrix)
    {
        internalLocalRotation = Quaternion.Euler(localOrientation);
        internalMatrix = math.mul(matrix, float4x4.TRS(localPosition, internalLocalRotation, new float3(1)));

    }

    public void UpdateMatrix(ProDecPointBurst m)
    {
        internalLocalRotation = m.internalLocalRotation;
        internalMatrix = m.internalMatrix;
    }
}

public struct ProDecPointBurst
{
    public float3 localOrientation;
    public float3 localPosition;    
    public float4x4 internalMatrix;
    public quaternion internalLocalRotation;


    public static implicit operator ProDecPointBurst(ProDecPoint p)
    {
        return new ProDecPointBurst()
        {
            localOrientation = p.localOrientation,
            localPosition = p.localPosition
        };
    }
}