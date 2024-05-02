using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpatialParadoxGenerator : MonoBehaviour
{
    public void PlaceStatnationBeacon(TunnelSection section, StagnationBeacon beacon)
    {
        if (section.stagnationBeacon == null)
        {
            beacon.transform.parent = section.transform;
            beacon.targetSection = section;
            section.stagnationBeacon = beacon.gameObject;
            section.Keep = true;
            OnMapUpdate?.Invoke();
        }
    }

    public void RemoveStagnationBeacon(StagnationBeacon beacon)
    {
        if (beacon.targetSection != null &&beacon.targetSection.stagnationBeacon == beacon)
        {
            beacon.targetSection.Keep = false;
            beacon.targetSection.stagnationBeacon = null;
            beacon.targetSection = null;
            OnMapUpdate?.Invoke();
        }
    }

    public void GetPlayerExplorationStatistics()
    {
        explorationStatistics = FindObjectOfType<PlayerExplorationStatistics>();
    }


    public void PlayerExitSection(MapTreeElement section)
    {
        lastExit = section;

        if (lastEnter != null)
        {
            UpdateMap();
        }
    }

    public void PlayerEnterSection(MapTreeElement section)
    {
        lastEnter = section;
        if (!section.sectionInstance.explored)
        {
            explorationStatistics.Increment(section.OriginalInstanceId);
        }
        explorationStatistics.JustVisitedCacher(section.OriginalInstanceId, section.sectionInstance.explored);
        if (lastExit != null)
        {
            UpdateMap();
        }
        if (section.sectionInstance.HasLadder)
        {
            OnEnterLadderSection?.Invoke();
        }
        if (section.sectionInstance.IsColony)
        {
            OnEnterColonySection?.Invoke();
        }
    }

}
