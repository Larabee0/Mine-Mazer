using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpatialParadoxGenerator : MonoBehaviour
{

    /// <summary>
    /// Places or removes a stagnation beacon in the current player section <see cref="curPlayerSection"/>
    /// This toggles a flag in the <see cref="TunnelSection"/> instance called "keep".
    /// </summary>
    /// <param name="context"></param>
    private void PlaceStagnationBeacon()
    {
        NPC_Interact player = explorationStatistics.GetComponent<NPC_Interact>();
        if (player.HitInteractable)
        {
            return;
        }
        if (curPlayerSection.Keep && !curPlayerSection.StrongKeep)
        {
            curPlayerSection.Keep = false;
            Destroy(curPlayerSection.stagnationBeacon);
        }
        else if (!curPlayerSection.StrongKeep)
        {
            curPlayerSection.Keep = true;
            Transform playerTransform = player.transform;
            curPlayerSection.stagnationBeacon = Instantiate(stagnationBeacon, playerTransform.position - new Vector3(0, 0.6f, 0f), playerTransform.rotation, curPlayerSection.transform);
        }
        OnMapUpdate?.Invoke();
    }

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


    public void PlayerExitSection(TunnelSection section)
    {
        lastExit = section;

        if (lastEnter != null)
        {
            UpdateMap();
        }
    }

    public void PlayerEnterSection(TunnelSection section)
    {
        lastEnter = section;
        if (!section.explored)
        {
            explorationStatistics.Increment(section.orignalInstanceId);
            if (section.SanctumPartSpawnPoint != null)
            {
                SpawnSanctumPartRandom(section.SanctumPartSpawnPoint);
            }
        }
        if (lastExit != null)
        {
            UpdateMap();
        }
        if (section.HasLadder)
        {
            OnEnterLadderSection?.Invoke();
        }
        if (section.IsColony)
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
        if(Random.value <= sanctumPartSpawnChance)
        {
            MapResource item = Instantiate(resources[Random.Range(0, resources.Count)], parent);
                item.transform.localPosition = item.placementPositionOffset;
            item.OnItemPickedUp += delegate () { sanctumPartCooldown = -sanctumPartCooldown; };
        }
    }
}
