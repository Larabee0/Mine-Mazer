using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DependsOnExplorationCountRule : SectionSpawnBaseRule
{
    [SerializeField] protected int exploreThreshold = 3;
    [SerializeField] protected bool aggressiveSpawning = false;
    protected int spawnableSince = -1;
    private bool nowSpawnable = false;
    public override int SpawnDebt
    {
        get
        {
            
            return aggressiveSpawning ? ExplorationStatistics.UniqueSpawnSectionsCount - spawnableSince : 1;
        }
    }

    public override bool UpdateSpawnStatus()
    {
        spawnable = base.UpdateSpawnStatus() && exploreThreshold < ExplorationStatistics.UniqueSpawnSectionsCount;
        if (!nowSpawnable && spawnable)
        {
            spawnableSince = ExplorationStatistics.UniqueSpawnSectionsCount;
            nowSpawnable = true;
        }
        return spawnable;
    }

    public override void ResetRule()
    {
        base.ResetRule();
        spawnableSince = -1;
        nowSpawnable = false;
    }
}
