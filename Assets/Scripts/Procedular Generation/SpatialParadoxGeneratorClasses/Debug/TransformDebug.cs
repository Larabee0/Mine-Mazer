#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator
{

    /// <summary>
    /// Visually tries every connector combination between <see cref="prefab1"/> and <see cref="prefab2"/>
    /// This runs forever.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TransformDebugging()
    {
        curPlayerSection = new()
        {
            sectionInstance = InstinateSection(prefab1)
        };
        curPlayerSection.sectionInstance.transform.position = new Vector3(0, 0, 0);
        TunnelSection newSection = InstinateSection(prefab2);

        yield return new WaitForSeconds(transformHoldTime);
        primaryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        secondaryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        while (true)
        {
            for (int i = 0; i < curPlayerSection.Connectors.Length; i++)
            {
                Connector primaryConnector = curPlayerSection.Connectors[i];

                curPlayerSection.Connectors[i] = primaryConnector;
                for (int j = 0; j < newSection.connectors.Length; j++)
                {
                    Connector secondaryConnector = newSection.connectors[j];
                    primaryConnector.UpdateWorldPos(curPlayerSection.sectionInstance.transform.localToWorldMatrix);
                    secondaryConnector.UpdateWorldPos(curPlayerSection.sectionInstance.transform.localToWorldMatrix);
                    newSection.connectors[j] = secondaryConnector;
                    float4x4 matix = CalculateSectionMatrix(primaryConnector, secondaryConnector);
                    newSection.transform.SetPositionAndRotation(matix.Translation(), matix.Rotation());
                    Debug.LogFormat("i = {0} j = {1}", i, j);
                    primaryObj.transform.SetPositionAndRotation(primaryConnector.position, primaryConnector.localRotation);
                    yield return new WaitForSeconds(transformHoldTime);
                    newSection.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
            }
            newSection.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}

#endif