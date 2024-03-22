using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionSpawnBaseRule : MonoBehaviour
{
    [SerializeField] protected bool spawnable;
    [SerializeField] protected int spawnLimit = -1;
    protected int instances = 0;
    public virtual int SpawnDebt => 1;

    public int InstancesCount
    {
        get => instances;
        set => instances = value;
    }

    protected PlayerExplorationStatistics ExplorationStatistics=>generator.ExplorationStatistics;
    
    [HideInInspector] public SpatialParadoxGenerator generator;
    [HideInInspector] public int owner;

    public bool Spawnable => spawnable;

    public virtual bool UpdateSpawnStatus()
    {
        if (ExplorationStatistics == null)
        {
            generator.GetPlayerExplorationStatistics();
        }

        spawnable = spawnLimit < 0 || InstancesCount < spawnLimit;
        return spawnable;
    }
    
    public virtual void OnSpawned()
    {

    }

    public virtual void ResetRule()
    {
        spawnable = false;
        instances = 0;
    }
}
