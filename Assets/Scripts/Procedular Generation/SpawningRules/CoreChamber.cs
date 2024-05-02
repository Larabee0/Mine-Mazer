using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CoreChamber : DependsOnExplorationCountRule
{
    [SerializeField] protected TunnelSection dependsOnVisited;

    protected bool visited = false;
    protected int startIndex = -1;
    public override bool UpdateSpawnStatus()
    {
        spawnable = false;
        if (!visited && ExplorationStatistics.UniqueVistedSections.Contains(dependsOnVisited.orignalInstanceId))
        {
            visited = true;
            startIndex = ExplorationStatistics.UniqueSpawnSectionsCount;
        }
        if (startIndex != -1)
        {
            spawnable =  base.UpdateSpawnStatus() && startIndex + exploreThreshold < ExplorationStatistics.UniqueSpawnSectionsCount;
        }
        return spawnable;
    }
    public override void ResetRule()
    {
        base.ResetRule();
        startIndex = -1;
        visited = false;
    }
}
