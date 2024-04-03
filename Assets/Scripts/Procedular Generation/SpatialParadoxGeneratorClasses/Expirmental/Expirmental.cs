using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class SpatialParadoxGenerator
{
    [Header("Expirmental Debug")]
    public bool DrawVirtualPhysicsWorldColliders = true;
    public bool runPostProcessLast = false;
    public bool breakEditorAfterInitialGen = false;
    public bool disableMultiThreading = false;
    public bool sameFrameComplete = false;
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
        if (disableMultiThreading)
        {
            return new UpdatePhysicsWorldTransforms
            {
                VirtualPhysicsWorld = VirtualPhysicsWorld
            }.Schedule(length, jobHandle);
        }
        else
        {
            int batches = Mathf.Max(2, length / SystemInfo.processorCount);
            return new UpdatePhysicsWorldTransforms
            {
                VirtualPhysicsWorld = VirtualPhysicsWorld
            }.ScheduleParallel(length, batches, jobHandle);
        }
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

    public MapTreeElement EnqueueSection(MapTreeElement primaryElement, TunnelSection prefabSecondary, Connector primaryConnector, Connector secondaryConnector)
    {
        int permanentID = primaryElement.GetHashCode();

        while (virtualPhysicsWorldIds.Contains(permanentID))
        {
            permanentID = randomNG.NextInt(0, int.MaxValue);
        }

        SectionQueueItem newQueue = new(primaryElement, prefabSecondary, primaryConnector, secondaryConnector, permanentID);

        virtualPhysicsWorldIds.Add(permanentID);
        HandleNewSectionInstance(prefabSecondary);

        MapTreeElement element = new()
        {
            queuedSection = newQueue,
            UID = permanentID
        };
        //LinkSections(primaryElement, element, primaryConnector.internalIndex, secondaryConnector.internalIndex);
        preProcessingQueue.Add(element);
        return element;
    }

    private IEnumerator PreProcessQueue()
    {
        NativeArray<BurstConnectorPair> matrixRequirments = new(preProcessingQueue.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<BurstConnector> primaryConnectors = new(preProcessingQueue.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<float4x4> matrixResults = new(preProcessingQueue.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < preProcessingQueue.Count; i++)
        {
            matrixRequirments[i] = preProcessingQueue[i].queuedSection.GetConnectorPair();
            processingQueue.Add(preProcessingQueue[i].queuedSection.physicsWorldId, preProcessingQueue[i]);
        }
        preProcessingQueue.Clear();

        yield return ProcessQueue(new FinalConnectorMulJob() { primaryConnectors = primaryConnectors, connectorPairs = matrixRequirments, calculatedMatricies = matrixResults });
    }

    private IEnumerator ProcessQueue(FinalConnectorMulJob matrixJob)
    {
        int length = matrixJob.connectorPairs.Length;
        JobHandle handle;
        if (disableMultiThreading)
        {
            handle = matrixJob.Schedule(length, new JobHandle());
        }
        else
        {
            int batches = Mathf.Max(2, length / SystemInfo.processorCount);
            handle = matrixJob.ScheduleParallel(length, batches, new JobHandle());
        }

        if (sameFrameComplete)
        {
            handle.Complete();
        }
        else
        {
            while (!handle.IsCompleted)
            {
                yield return null;
            }
            handle.Complete();
        }

        yield return null;
        for (int i = 0; i < length; i++)
        {
            var item = processingQueue[matrixJob.connectorPairs[i].id].queuedSection;
            item.secondaryMatrix = matrixJob.calculatedMatricies[i];
            item.primaryConnector = matrixJob.primaryConnectors[i];
            postProcessingQueue.Add(processingQueue[matrixJob.connectorPairs[i].id]);
            processingQueue.Remove(item.physicsWorldId);
            AddSection(item.pickedPrefab, item.secondaryMatrix, item.physicsWorldId);
        }
        matrixJob.connectorPairs.Dispose();
        matrixJob.calculatedMatricies.Dispose();
        matrixJob.primaryConnectors.Dispose();
    }

    private IEnumerator PostProcessQueue()
    {
        double interationTime = Time.realtimeSinceStartupAsDouble;
        while (postProcessingQueue.Count > 0)
        {
            var item = postProcessingQueue.Dequeue();
            QueuedTunnInstantiate(item);

            double timeCheck = (Time.realtimeSinceStartupAsDouble - interationTime) * 1000;
            if (timeCheck > maxTimeInstantiatingPerFrame)
            {
                //Debug.LogFormat("Spent {0}ms Instantiating, waiting for next frame", timeCheck);
                yield return null;
                interationTime = Time.realtimeSinceStartupAsDouble;
            }
            yield return null;
        }
    }

    private IEnumerator PostProcessQueueInfinite()
    {
        while (true)
        {
            yield return PostProcessQueue();
            yield return null;
        }
    }

    private void QueuedTunnInstantiate(MapTreeElement item)
    {
        TunnelSection sectionInstance = Instantiate(item.queuedSection.pickedPrefab,
            item.queuedSection.secondaryMatrix.Translation(),
            item.queuedSection.secondaryMatrix.Rotation(),
            transform);
        sectionInstance.gameObject.SetActive(true);
        InstantiateBreakableWalls(item.queuedSection.pickedPrefab, sectionInstance, item.queuedSection.primaryConnector);
        item.SetInstance(sectionInstance);
        totalDecorations += sectionInstance.Decorate(decorCoverage, decorations);
    }

    public void AddSection(TunnelSection sectionInstance, float4x4 matrix)
    {
        int id = sectionInstance.GetInstanceID();
        int index = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual() { boundSection = id });
        if (index >= 0)
        {
            UpdateSectionTransform(id, matrix);
        }
        VirtualPhysicsWorld.Add(new TunnelSectionVirtual() { boundSection = id });
        InitiliseTSV(ref VirtualPhysicsWorld.ElementAt(VirtualPhysicsWorld.Length - 1), sectionInstance, matrix);
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

    public void DestroySectionPhysicsWorld(int id)
    {
        int index = VirtualPhysicsWorld.IndexOf(new TunnelSectionVirtual() { boundSection = id });
        if (index >= 0)
        {
            VirtualPhysicsWorld[index].Dispose();
            VirtualPhysicsWorld.RemoveAt(index);
        }
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

    public IEnumerator ParallelRandomiseIntersection(MapTreeElement primary,
        List<Connector> primaryConnectors,
        int priIndex, List<int> internalNextSections, ParallelRandInter iteratorData)
    {
        List<Connector> secondaryConnectors = new();
        List<int2> managedTests = new();
        for (int i = 0; i < internalNextSections.Count; i++)
        {
            int id = internalNextSections[i];
            List<Connector> secondaryConnectorsInner = FilterConnectorsByOriginalOnly(id);
            secondaryConnectors.AddRange(secondaryConnectorsInner);
            secondaryConnectorsInner.ForEach(connector => managedTests.Add(new(id, connector.internalIndex)));
        }

        NativeArray<int2> tests = new(managedTests.ToArray(), Allocator.Persistent);
        NativeArray<bool> results = new(managedTests.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        int length = ScheduleBoxTests(iteratorData, tests, results);
        if (sameFrameComplete)
        {
            iteratorData.handle.Complete();
        }
        else
        {
            while (!iteratorData.handle.IsCompleted)
            {
                yield return null;
            }
            iteratorData.handle.Complete();
        }
        List<int2> validSecondaryConnectors = GetValidSecondaryConnectors(tests, results, length);

        CheckValidConnectors(primary, primaryConnectors, priIndex, iteratorData, secondaryConnectors, validSecondaryConnectors);

        iteratorData.iterations++;
    }

    private void CheckValidConnectors(MapTreeElement primary, List<Connector> primaryConnectors, int priIndex, ParallelRandInter iteratorData, List<Connector> secondaryConnectors, List<int2> validSecondaryConnectors)
    {
        if (validSecondaryConnectors.Count > 0)
        {
            int2 connectorIndex = validSecondaryConnectors[randomNG.NextInt(0, validSecondaryConnectors.Count)];
            iteratorData.secondaryPreference = secondaryConnectors[connectorIndex.y];
            iteratorData.targetSection = instanceIdToSection[connectorIndex.x];
            ConnectorMultiply(primary.LocalToWorld, ref iteratorData.primaryPreference, ref iteratorData.secondaryPreference);
            iteratorData.success = true;
        }
        else
        {
            iteratorData.targetSection = null;
            primaryConnectors.RemoveAt(priIndex);
            iteratorData.success = false;
        }
    }

    private static List<int2> GetValidSecondaryConnectors(NativeArray<int2> tests, NativeArray<bool> results, int length)
    {
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
        return validSecondaryConnectors;
    }

    private int ScheduleBoxTests(ParallelRandInter iteratorData, NativeArray<int2> tests, NativeArray<bool> results)
    {
        int length = tests.Length;
        iteratorData.handle = UpdatePhyscisWorld(iteratorData.handle);
        if (disableMultiThreading)
        {
            iteratorData.handle = new BoxCheckJob
            {
                incomingMatrices = sectionBoxTransforms,
                incomingBoxBounds = incomingBoxBounds,
                VirtualPhysicsWorld = VirtualPhysicsWorld,
                sectionIds = tests,
                outGoingChecks = results
            }.Schedule(length, iteratorData.handle);
        }
        else
        {
            int batches = Mathf.Max(2, length / SystemInfo.processorCount);
            iteratorData.handle = new BoxCheckJob
            {
                incomingMatrices = sectionBoxTransforms,
                incomingBoxBounds = incomingBoxBounds,
                VirtualPhysicsWorld = VirtualPhysicsWorld,
                sectionIds = tests,
                outGoingChecks = results
            }.ScheduleParallel(length, batches, iteratorData.handle);
        }
        return length;
    }

    private void OnDrawGizmos()
    {
        if (!DrawVirtualPhysicsWorldColliders) { return; }
        if (!VirtualPhysicsWorld.IsCreated) { return; }
        if (VirtualPhysicsWorld.IsEmpty) { return; }
        Gizmos.color = Color.red;
        for (int i = 0; i < VirtualPhysicsWorld.Length; i++)
        {
            TunnelSectionVirtual tunnelSectionVirtual = VirtualPhysicsWorld[i];
            for (int j = 0; j < tunnelSectionVirtual.boxes.Length; j++)
            {
                InstancedBox box = tunnelSectionVirtual.boxes[j];

                Gizmos.matrix = math.mul(tunnelSectionVirtual.sectionTransform, box.boxBounds.LocalMatrix);
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
                if (i == 1)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawWireCube(Vector3.zero, box.boxBounds.size);

            }

            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            for (int i = 0; i < drawCorners.Count; i++)
            {
                if (i == 8)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawSphere(drawCorners[i], 0.5f);
            }
        }
    }
}
