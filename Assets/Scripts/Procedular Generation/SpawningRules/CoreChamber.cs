using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreChamber : DependsOnExplorationCountRule
{
    [SerializeField] protected TunnelSection dependsOnVisited;

    protected bool visited = false;
    protected int startIndex = -1;
    public override bool UpdateSpawnStatus()
    {
        if (!visited&&ExplorationStatistics.UniqueVistedSections.Contains(dependsOnVisited.orignalInstanceId))
        {
            visited = true;
            startIndex = ExplorationStatistics.UniqueSpawnSectionsCount;
        }
        if(startIndex != -1)
        {
           return base.UpdateSpawnStatus() && startIndex + exploreThreshold < ExplorationStatistics.UniqueSpawnSectionsCount;
        }
        return false;
    }
}
