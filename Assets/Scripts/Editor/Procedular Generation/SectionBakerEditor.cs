using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
        section.boundingBoxes = boxBounds.ToArray();
        section.boundingCaps = capBounds.ToArray();
        if (baker.SectionStart != null)
        {
            section.startConnector = new Connector()
            {
                localPosition = baker.SectionStart.transform.localPosition,
                localRotation = baker.SectionStart.transform.localRotation
            };
        }
        if (baker.SectionEnd != null)
        {
            section.endConnector = new Connector()
            {
                localPosition = baker.SectionEnd.transform.localPosition,
                localRotation = baker.SectionEnd.transform.localRotation
            };
        }
        
    }
}
