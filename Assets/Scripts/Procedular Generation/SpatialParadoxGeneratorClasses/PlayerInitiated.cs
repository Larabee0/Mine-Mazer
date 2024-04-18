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
            if (section.sectionInstance.SanctumPartSpawnPoint != null)
            {
                SpawnSanctumPartRandom(section.sectionInstance.SanctumPartSpawnPoint);
            }
        }
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

    public void SpawnSanctumPartRandom(Transform parent)
    {
        List<MapResource> resources = Inventory.Instance.GetMissingSanctumParts();
        if (resources.Count == 0||!ExplorationStatistics.AllowSanctumPartSpawn || sanctumPartCooldown < 0)
        {
            sanctumPartCooldown++;
            return;
        }
        if(randomNG.NextFloat() <= sanctumPartSpawnChance)
        {
            MapResource item = Instantiate(resources[randomNG.NextInt(0, resources.Count)], parent);
                item.transform.localPosition = item.placementPositionOffset;
            item.OnItemPickedUp += delegate () { sanctumPartCooldown = -sanctumPartCooldown; };
        }
    }
}
