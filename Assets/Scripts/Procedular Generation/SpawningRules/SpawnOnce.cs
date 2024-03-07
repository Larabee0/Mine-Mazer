using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnOnce : SectionSpawnBaseRule
{
    [SerializeField]private bool hasBeenSpawned = false;

    public override bool UpdateSpawnStatus()
    {
        spawnable = !hasBeenSpawned;
        return !hasBeenSpawned;
    }
    public override void OnSpawned()
    {
        hasBeenSpawned=true;
    }
    public override void ResetRule()
    {
        base.ResetRule();
        hasBeenSpawned = false;
    }
}
