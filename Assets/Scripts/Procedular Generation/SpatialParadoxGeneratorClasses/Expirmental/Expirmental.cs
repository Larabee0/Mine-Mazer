using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public partial class SpatialParadoxGenerator
{
    [Header("Expirmental Debug")]
    public bool DrawVirtualPhysicsWorldColliders = true;
    public bool parallelIntersectTests = true;
    public bool BigParallelIntersectTests = true;
    public bool incrementalBuilder = true;
    public float maxTimeInstantiatingPerFrame = 8;

    List<InstancedBox> fromIntersecitonTests = new();

    List<Vector3> drawCorners = new();

    private bool showIntersectionTests = false;

    public void UpdateVirtualPhysicsWorld()
    {
        UpdatePhyscisWorld(new()).Complete();
    }

    public JobHandle UpdatePhyscisWorld(JobHandle jobHandle)
    {
        int length = VirtualPhysicsWorld.Length;
        int batches = Mathf.Max(2, length / SystemInfo.processorCount);
        return new UpdatePhysicsWorldTransforms
        {
            VirtualPhysicsWorld = VirtualPhysicsWorld
        }.ScheduleParallel(length, batches, jobHandle);
    }

    public void UpdateSectionTransform(int id, float4x4 matrix)
    {
        int index = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual() { boundSection = id });
        if (index >= 0)
        {
            VirtualPhysicsWorld.ElementAt(index).Changed = true;
            VirtualPhysicsWorld.ElementAt(index).sectionTransform = matrix;
        }
    }

    public SectionQueueItem EnqueueSection(TunnelSection primaryInstance, TunnelSection prefabSecondary,Connector primaryConnector,Connector secondaryConnector)
    {
        int temporaryID = primaryInstance.GetInstanceID();

        while (virtualPhysicsWorldIds.Contains(temporaryID))
        {
            temporaryID = Random.Range(0, int.MaxValue);
        }

        SectionQueueItem newQueue = new(primaryInstance, prefabSecondary, primaryConnector, secondaryConnector, temporaryID);
        preProcessingQueue.Add(newQueue);
        virtualPhysicsWorldIds.Add(temporaryID);
        return newQueue;
    }

    private IEnumerator PreProcessQueue()
    {
        NativeArray<BurstConnectorPair> matrixRequirments = new(preProcessingQueue.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<BurstConnector> primaryConnectors = new(preProcessingQueue.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<float4x4> matrixResults = new(preProcessingQueue.Count, Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < preProcessingQueue.Count; i++)
        {
            matrixRequirments[i] = preProcessingQueue[i].GetConnectorPair();
            processingQueue.Add(preProcessingQueue[i].physicsWorldTempId, preProcessingQueue[i]);
        }
        preProcessingQueue.Clear();

        yield return ProcessQueue(new FinalConnectorMulJob() { primaryConnectors = primaryConnectors, connectorPairs = matrixRequirments, calculatedMatricies = matrixResults });
    }

    private IEnumerator ProcessQueue(FinalConnectorMulJob matrixJob)
    {
        int length = matrixJob.connectorPairs.Length;
        int batches = Mathf.Max(2, length / SystemInfo.processorCount);
        JobHandle handle = matrixJob.ScheduleParallel(length, batches, new JobHandle());
        while (!handle.IsCompleted)
        {
            yield return null;
        }
        handle.Complete();
        yield return null;
        for (int i = 0; i < length; i++)
        {
            var item = processingQueue[matrixJob.connectorPairs[i].id];
            item.secondaryMatrix = matrixJob.calculatedMatricies[i];
            item.primaryConnector = matrixJob.primaryConnectors[i];
            processingQueue.Remove(item.physicsWorldTempId);
            postProcessingQueue.Add(item);
            AddSection(item.secondaryPickedPrefab,item.secondaryMatrix,item.physicsWorldTempId);
        }
        matrixJob.connectorPairs.Dispose();
        matrixJob.calculatedMatricies.Dispose();
        matrixJob.primaryConnectors.Dispose();
        //UpdateVirtualPhysicsWorld();
        yield return PostProcessQueue();
    }

    private IEnumerator PostProcessQueue()
    {
        yield return null;
        double interationTime = Time.realtimeSinceStartupAsDouble;
        while (postProcessingQueue.Count > 0)
        {
            var item = postProcessingQueue.Pop();
            QueuedTunnInstantiate(item);

            double timeCheck = (Time.realtimeSinceStartupAsDouble - interationTime) * 1000;
            if (timeCheck > maxTimeInstantiatingPerFrame)
            {
                //Debug.LogFormat("Spent {0}ms Instantiating, waiting for next frame", timeCheck);
                yield return null;
                interationTime = Time.realtimeSinceStartupAsDouble;
            }
        }
    }

    private void QueuedTunnInstantiate(SectionQueueItem item)
    {
        TunnelSection sectionInstance = Instantiate(item.secondaryPickedPrefab, item.secondaryMatrix.Translation(), item.secondaryMatrix.Rotation(), transform);
        
        virtualPhysicsWorldIds.Remove(item.physicsWorldTempId);
        int physicsWorldIndex = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual { boundSection = item.physicsWorldTempId });
        if (physicsWorldIndex >= 0)
        {
            int instanceId = VirtualPhysicsWorld.ElementAt(physicsWorldIndex).boundSection = sectionInstance.GetInstanceID();
            virtualPhysicsWorldIds.Add(instanceId);
        }
        else
        {
            AddSection(sectionInstance, item.secondaryMatrix);
        }

        InstantiateBreakableWalls(item.secondaryPickedPrefab, sectionInstance, item.primaryConnector);
        item.FinishConnection(sectionInstance);
    }

    public void AddSection(TunnelSection sectionInstance, float4x4 matrix)
    {
        int id = sectionInstance.GetInstanceID();
        int index = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual() { boundSection = id });
        if(index >= 0)
        {
            UpdateSectionTransform(id, matrix);
        }
        VirtualPhysicsWorld.Add(new TunnelSectionVirtual() { boundSection = id });
        InitiliseTSV(ref VirtualPhysicsWorld.ElementAt(VirtualPhysicsWorld.Length - 1), sectionInstance, matrix);

        //UpdateVirtualPhysicsWorld();
    }

    public void AddSection(TunnelSection sectionInstance, float4x4 matrix, int id)
    {
        int index = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual() { boundSection = id });
        if (index >= 0)
        {
            UpdateSectionTransform(id, matrix);
        }
        VirtualPhysicsWorld.Add(new TunnelSectionVirtual() { boundSection = id });
        InitiliseTSV(ref VirtualPhysicsWorld.ElementAt(VirtualPhysicsWorld.Length - 1), sectionInstance, matrix);
    }

    private void InitiliseTSV(ref TunnelSectionVirtual newTSV, TunnelSection section, in float4x4 matrix)
    {
        newTSV.Changed = true;
        newTSV.sectionTransform = matrix;
        newTSV.boxes = new UnsafeList<InstancedBox>(section.BoundingBoxes.Length, Allocator.Persistent);
        newTSV.boxes.Resize(section.BoundingBoxes.Length);
        for (int i = 0; i < section.BoundingBoxes.Length; i++)
        {
            newTSV.boxes[i] = new(section.BoundingBoxes[i]);
        }
    }

    public void DestroySection(int id)
    {
        int index = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual() { boundSection = id });
        if (index >= 0)
        {
            VirtualPhysicsWorld[index].Dispose();
            VirtualPhysicsWorld.RemoveAt(index);
        }
    }

    public bool ParallelRandomiseIntersection(TunnelSection primary,
        ref Connector primaryPreference,
        ref Connector secondaryPreference,
        List<Connector> primaryConnectors,
        ref int iterations,
        ref TunnelSection targetSection,
        int priIndex, List<int> internalNextSections,
        JobHandle handle)
    {
        List<Connector> secondaryConnectors = new();
        List<int2> managedTests = new();
        for (int i = 0; i < internalNextSections.Count; i++)
        {
            int id = internalNextSections[i];
            List<Connector> secondaryConnectorsInner = FilterConnectors(instanceIdToSection[id]);
            secondaryConnectors.AddRange(secondaryConnectorsInner);
            secondaryConnectorsInner.ForEach(connector => managedTests.Add(new(id, connector.internalIndex)));
        }

        NativeArray<int2> tests = new(managedTests.ToArray(), Allocator.TempJob);
        NativeArray<bool> results = new(managedTests.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        int length = tests.Length;
        int batches = Mathf.Max(2,length / SystemInfo.processorCount);
        new BoxCheckJob
        {
            incomingMatrices = sectionBoxTransforms,
            incomingBoxBounds = incomingBoxBounds,
            VirtualPhysicsWorld = VirtualPhysicsWorld,
            sectionIds = tests,
            outGoingChecks = results
        }.ScheduleParallel(length, batches, handle).Complete();
        
        //Debug.Log(tests.Length);

        List<int2> validSecondaryConnectors = new(tests.Length);
        for (int i = 0; i < length; i++)
        {
            if (results[i])
            {
                validSecondaryConnectors.Add(new(tests[i].x, i));
            }
        }
        tests.Dispose();
        results.Dispose();

        iterations++;
        if (validSecondaryConnectors.Count > 0)
        {

            int2 connectorIndex = validSecondaryConnectors[UnityEngine.Random.Range(0, validSecondaryConnectors.Count)];
            secondaryPreference = secondaryConnectors[connectorIndex.y];
            targetSection = instanceIdToSection[connectorIndex.x];
            ConnectorMultiply(primary, ref primaryPreference, ref secondaryPreference);
            return true;
        }
        targetSection = null;
        primaryConnectors.RemoveAt(priIndex);
        return false;
    }

    public class ParallelRandInter
    {
        public Connector primaryPreference;
        public Connector secondaryPreference;
        public int iterations;
        public TunnelSection targetSection;
        public bool success;
        public JobHandle handle;
    }

    public IEnumerator ParallelRandomiseIntersection(TunnelSection primary,
        List<Connector> primaryConnectors,
        int priIndex, List<int> internalNextSections, ParallelRandInter iteratorData)
    {
        List<Connector> secondaryConnectors = new();
        List<int2> managedTests = new();
        for (int i = 0; i < internalNextSections.Count; i++)
        {
            int id = internalNextSections[i];
            List<Connector> secondaryConnectorsInner = FilterConnectors(instanceIdToSection[id]);
            secondaryConnectors.AddRange(secondaryConnectorsInner);
            secondaryConnectorsInner.ForEach(connector => managedTests.Add(new(id, connector.internalIndex)));
        }

        NativeArray<int2> tests = new(managedTests.ToArray(), Allocator.Persistent);
        NativeArray<bool> results = new(managedTests.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        int length = tests.Length;
        int batches = Mathf.Max(2, length / SystemInfo.processorCount);
        iteratorData.handle = UpdatePhyscisWorld(iteratorData.handle);
        iteratorData.handle = new BoxCheckJob
        {
            incomingMatrices = sectionBoxTransforms,
            incomingBoxBounds = incomingBoxBounds,
            VirtualPhysicsWorld = VirtualPhysicsWorld,
            sectionIds = tests,
            outGoingChecks = results
        }.ScheduleParallel(length, batches, iteratorData.handle);
        while (!iteratorData.handle.IsCompleted)
        {
            yield return null;
        }
        iteratorData.handle.Complete();
        //Debug.Log(tests.Length);

        List<int2> validSecondaryConnectors = new(tests.Length);
        for (int i = 0; i < length; i++)
        {
            if (results[i])
            {
                validSecondaryConnectors.Add(new(tests[i].x, i));
            }
        }
        tests.Dispose();
        results.Dispose();

        iteratorData.iterations++;
        if (validSecondaryConnectors.Count > 0)
        {

            int2 connectorIndex = validSecondaryConnectors[UnityEngine.Random.Range(0, validSecondaryConnectors.Count)];
            iteratorData.secondaryPreference = secondaryConnectors[connectorIndex.y];
            iteratorData.targetSection = instanceIdToSection[connectorIndex.x];
            ConnectorMultiply(primary, ref iteratorData.primaryPreference, ref iteratorData.secondaryPreference);
            iteratorData.success = true;

        }
        else
        {
            iteratorData.targetSection = null;
            primaryConnectors.RemoveAt(priIndex);
            iteratorData.success = false;
        }
    }

    public bool ParallelIntersectTest(TunnelSection primary, TunnelSection target, ref Connector primaryConnector, out Connector secondaryConnector)
    {
        secondaryConnector = Connector.Empty;
        List<Connector> secondaryConnectors = FilterConnectors(target);

        NativeArray<int2> tests = new(secondaryConnectors.Count, Allocator.TempJob);
        NativeArray<bool> results = new(secondaryConnectors.Count, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < secondaryConnectors.Count; i++)
        {
            tests[i] = new(target.orignalInstanceId, secondaryConnectors[i].internalIndex);
        }

        new BoxCheckJob
        {
            incomingMatrices = sectionBoxTransforms,
            incomingBoxBounds = incomingBoxBounds,
            VirtualPhysicsWorld = VirtualPhysicsWorld,
            sectionIds = tests,
            outGoingChecks = results
        }.ScheduleParallel(tests.Length, 64, new()).Complete();
        

        List<int> validSecondaryConnectors = new(secondaryConnectors.Count);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i])
            {
                validSecondaryConnectors.Add(i);
            }
        }
        tests.Dispose();
        results.Dispose();

        if(validSecondaryConnectors.Count == 0)
        {
            return false;
        }

        int connectorIndex = validSecondaryConnectors[UnityEngine.Random.Range(0, validSecondaryConnectors.Count)];
        secondaryConnector = secondaryConnectors[connectorIndex];

        ConnectorMultiply(primary, ref primaryConnector, ref secondaryConnector);
        return true;
    }

    public bool ManualCheckBoxBurst(int id, int connectorIndex)
    {
        NativeArray<int2> tests = new(new int2[] { new(id, connectorIndex) }, Allocator.TempJob);
        NativeArray<bool> results = new(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        new BoxCheckJob
        {
            incomingMatrices = sectionBoxTransforms,
            incomingBoxBounds = incomingBoxBounds,
            VirtualPhysicsWorld = VirtualPhysicsWorld,
            sectionIds = tests,
            outGoingChecks = results
        }.ScheduleParallel(1, 64, new()).Complete();
        bool result = results[0];
        results.Dispose();
        tests.Dispose();

        return result;
    }

    public bool ManualCheckBox(int id, int connectorIndex)
    {
        UpdateVirtualPhysicsWorld();
        UnsafeList<InstancedBox> sectionBoxes = incomingBoxBounds[id];
        UnsafeList<BoxTransform> sectionBoxTransforms = this.sectionBoxTransforms[id][connectorIndex];

        for (int i = 0; i < sectionBoxes.Length; i++)
        {
            sectionBoxes.ElementAt(i).GetTransformedCorners(sectionBoxTransforms[i].Matrix);
            sectionBoxes.ElementAt(i).TransformNormals(sectionBoxTransforms[i].Matrix);            
        }

        for (int i = 0; i < sectionBoxes.Length; i++)
        {
            if (CheckBox(sectionBoxes[i]))
            {
                fromIntersecitonTests.Add(new InstancedBox() { boxBounds = sectionBoxes[i].boxBounds, matrix = sectionBoxTransforms[i].Matrix });
                return false;
            }
        }

        return true;
    }

    public bool CheckBox(InstancedBox box)
    {
        for (int i = 0; i < VirtualPhysicsWorld.Length; i++)
        {
            TunnelSectionVirtual sectionInstance = VirtualPhysicsWorld[i];

            if (CheckBox(sectionInstance, box))
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckBox(TunnelSectionVirtual sectionInstance, InstancedBox box)
    {
        UnsafeList<InstancedBox> instancedBoxes = sectionInstance.boxes;
        for (int i = 0; i < instancedBoxes.Length; i++)
        {
            if (CheckBox(instancedBoxes[i].normals, instancedBoxes[i].corners, box.normals, box.corners))
            {
                for (int j = 0; j < 8; j++)
                {
                    drawCorners.Add(instancedBoxes[i].corners[j]);
                }
                for (int j = 0; j < 8; j++)
                {
                    drawCorners.Add(box.corners[j]);
                }

                fromIntersecitonTests.Add(new InstancedBox() { boxBounds = instancedBoxes[i].boxBounds, matrix = math.mul(sectionInstance.sectionTransform, instancedBoxes[i].boxBounds.LocalMatrix) });
                return true;
            }
        }
        return false;
    }

    private bool CheckBox(UnsafeList<float3> aNormals, UnsafeList<float3> aCorners, UnsafeList<float3> bNormals, UnsafeList<float3> bCorners)
    {
        for (int i = 0; i < aNormals.Length; i++)
        {
            SATTest(aNormals[i], aCorners, out float shape1Min, out float shape1Max);
            SATTest(aNormals[i], bCorners, out float shape2Min, out float shape2Max);
            if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
            {
                return false;
            }
        }

        for (int i = 0; i < bNormals.Length; i++)
        {
            SATTest(bNormals[i], aCorners, out float shape1Min, out float shape1Max);
            SATTest(bNormals[i], bCorners, out float shape2Min, out float shape2Max);
            if (!Overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
            {
                return false;
            }
        }

        return true;
    }

    private void SATTest(float3 axis, UnsafeList<float3> ptSet, out float minAlong, out float maxAlong)
    {
        minAlong = float.MaxValue;
        maxAlong = float.MinValue;
        for (int i = 0; i < ptSet.Length; i++)
        {
            float dotVal = math.dot(ptSet[i], axis);
            minAlong = (dotVal < minAlong) ? dotVal : minAlong;
            maxAlong = (dotVal > maxAlong) ? dotVal : maxAlong;
        }
    }

    private bool Overlaps(float min1, float max1, float min2, float max2)
    {
        return IsBetweenOrdered(min2, min1, max1) || IsBetweenOrdered(min1, min2, max2);
    }

    private bool IsBetweenOrdered(float val, float lowerBound, float upperBound)
    {
        return lowerBound <= val && val <= upperBound;
    }

    private void OnDrawGizmos()
    {
        if (!DrawVirtualPhysicsWorldColliders) { return; }
        if(!VirtualPhysicsWorld.IsCreated) { return; }
        if(VirtualPhysicsWorld.IsEmpty) { return; }
        Gizmos.color = Color.red;
        for (int i = 0; i < VirtualPhysicsWorld.Length; i++)
        {
            TunnelSectionVirtual tunnelSectionVirtual = VirtualPhysicsWorld[i];
            for (int j = 0; j <  tunnelSectionVirtual.boxes.Length; j++)
            {
                InstancedBox box = tunnelSectionVirtual.boxes[j];

                Gizmos.matrix = math.mul(tunnelSectionVirtual.sectionTransform,box.boxBounds.LocalMatrix);
                Gizmos.DrawWireCube(Vector3.zero, box.boxBounds.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
            for (int j = 0; j < tunnelSectionVirtual.boxes.Length; j++)
            {
                for (int ij = 0; ij < 8; ij++)
                {
                    float3 pos = tunnelSectionVirtual.boxes[j].corners[ij];
                    Gizmos.DrawSphere(pos, 0.1f);
                }
            }    
        }
        if (showIntersectionTests)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < fromIntersecitonTests.Count; i++)
            {
                InstancedBox box = fromIntersecitonTests[i];
                Gizmos.matrix = box.matrix;
                if(i == 1)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawWireCube(Vector3.zero, box.boxBounds.size);
                
            }

            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            for (int i = 0; i < drawCorners.Count; i++)
            {
                if(i == 8)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawSphere(drawCorners[i], 0.5f);
            }
        }
    }
}
