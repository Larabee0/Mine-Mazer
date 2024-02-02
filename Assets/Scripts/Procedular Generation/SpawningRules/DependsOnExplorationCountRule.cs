using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DependsOnExplorationCountRule : SectionSpawnBaseRule
{
    [SerializeField] protected int exploreThreshold = 3;

    public override bool UpdateSpawnStatus()
    {
        spawnable = base.UpdateSpawnStatus() && exploreThreshold < ExplorationStatistics.UniqueSectionsVisited;
        return spawnable;
    }
}
