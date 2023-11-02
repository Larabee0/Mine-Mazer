using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(SectionBaker))]
public class SectionBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SectionBaker baker = (SectionBaker)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Bake"))
        {
            Bake(baker);
        }
    }

    private void Bake(SectionBaker baker)
    {
        if(baker.SectionModel == null) { Debug.LogWarning("Missing section Model, cannot create prefab"); return; }
        List<BoxBounds> boxBounds = new();
        List<CapsuleBounds> capBounds = new();
        for (int i = 0; i < baker.BoundsObjects.Length; i++)
        {
            GameObject go = baker.BoundsObjects[i];
            if (go.TryGetComponent(out Collider collider))
            {
                if (collider is BoxCollider boxCollider)
                {
                    boxBounds.Add(new BoxBounds()
                    {
                        center = boxCollider.center + go.transform.localPosition,
                        size = go.transform.localScale,
                        oreintation = go.transform.localRotation.eulerAngles,
                    });
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    Vector3 scale = go.transform.localScale;
                    float radMul = scale.x > scale.z ? scale.x : scale.z;
                    capBounds.Add(new CapsuleBounds()
                    {
                        center = capsuleCollider.center + go.transform.localPosition,
                        radius = capsuleCollider.radius * radMul,
                        height = capsuleCollider.height * scale.y,
                        oreintation = go.transform.localRotation.eulerAngles
                    });
                }
            }
        }
        GameObject prefab = Instantiate(baker.SectionModel);
        prefab.name = baker.name + " Prefab";
        if (prefab.TryGetComponent(out TunnelSection section))
        {
            Destroy(section);
        }
        section = prefab.AddComponent<TunnelSection>();
        if (baker.ConnectorObjects != null)
        {
            section.connectors = new Connector[baker.ConnectorObjects.Length];

            section.connectorPairs = new(section.connectors.Length);
            for (int i = 0; i < baker.ConnectorObjects.Length; i++)
            {
                section.connectors[i] = new Connector()
                {
                    internalIndex = i,
                    localPosition = baker.ConnectorObjects[i].transform.localPosition,
                    localRotation = baker.ConnectorObjects[i].transform.localRotation
                };
                section.connectorPairs.Add(i,null);
            }
        }


        boxBounds.ForEach(box =>
        {
            GameObject empty = new("BoundingBox");
            empty.transform.SetParent(section.transform, false);
            empty.transform.SetLocalPositionAndRotation(box.center, Quaternion.Euler(box.oreintation));
            BoxCollider collider = empty.AddComponent<BoxCollider>();
            collider.size = box.size;
            empty.layer = (int)Mathf.Log(baker.tunnelSectionLayerMask.value, 2);
        });
        
        capBounds.ForEach(cap =>
        {
            GameObject empty = new("BoundingCap");
            empty.transform.SetParent(section.transform, false);
            empty.transform.SetLocalPositionAndRotation(cap.center, Quaternion.Euler(cap.oreintation));
            CapsuleCollider collider = empty.AddComponent<CapsuleCollider>();
            collider.radius = cap.radius;
            collider.height = cap.height;
            empty.layer = (int)Mathf.Log(baker.tunnelSectionLayerMask.value, 2);
        });

        section.boundingBoxes = boxBounds.ToArray();
        section.boundingCaps = capBounds.ToArray();


        
    }
}
