using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialParadoxGenerator 
{
    private static void ConnectorMultiply(float4x4 primaryMatrix, ref Connector primaryConnector, ref Connector secondaryConnector)
    {
        NativeReference<Connector> pri = new(primaryConnector, Allocator.TempJob);
        NativeReference<Connector> sec = new(secondaryConnector, Allocator.TempJob);
        JobHandle handle1 = new ConnectorMulJob { connector = pri, sectionLTW = primaryMatrix, }.Schedule(new JobHandle());
        JobHandle handle2 = new ConnectorMulJob { connector = sec, sectionLTW = float4x4.identity }.Schedule(new JobHandle());
        JobHandle.CombineDependencies(handle1, handle2).Complete();
        primaryConnector = pri.Value;
        secondaryConnector = sec.Value;
        pri.Dispose();
        sec.Dispose();
    }
}
