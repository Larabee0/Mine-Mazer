using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class PlayerExplorationStatistics : MonoBehaviour
{
    private HashSet<int> uniqueIdVistedSections = new();
    [SerializeField] private int uniqueSpawnSectionsCount = 0;

    [SerializeField] private bool allowSanctumPartSpawn = false;
    private bool sanctumPartLatch = false;

    [SerializeField] private bool playerLearnedFromlarimar = false;
    [SerializeField] private bool playerHasSpokenToMQ = false;
    [SerializeField] private bool playerMQLeaveBad = false;
    [SerializeField] private bool playerMQLeaveGood = false;
    [SerializeField] private bool playerMQSeenSanctumHandover = false;

    public HashSet<int> UniqueVistedSections => uniqueIdVistedSections;

    public int UniqueSpawnSectionsCount => uniqueSpawnSectionsCount;

    public bool AllowSanctumPartSpawn => allowSanctumPartSpawn;

    public bool PlayerLearnedFromlarimar => playerLearnedFromlarimar;
    public bool PlayerHasSpokenToMQ => playerHasSpokenToMQ;
    public bool PlayerMQLeaveBad => playerMQLeaveBad;
    public bool PlayerMQLeaveGood => playerMQLeaveGood;
    public bool PlayerMQSeenSanctumHandover => playerMQSeenSanctumHandover;


    public void Increment(int originalInstanceId)
    {
        uniqueIdVistedSections.Add(originalInstanceId);
        uniqueSpawnSectionsCount++;
    }

    public void SetAllowSanctumPartSpawn(bool allowed, bool permant = false)
    {
        if (!sanctumPartLatch)
        {
            allowSanctumPartSpawn = allowed;
        }
        sanctumPartLatch = permant;
    }


    public void SetLarmiar_MinesMove()
    {
        playerLearnedFromlarimar = true;
    }

    public void SetMQ_SpokenTo()
    {
        playerHasSpokenToMQ = true;
    }

    public void SetMQ_LeaveBad()
    {
        playerMQLeaveBad = true;
        playerMQLeaveGood = false;
    }


    public void SetMQ_LeaveGood()
    {
        playerMQLeaveGood = true;
        playerMQLeaveBad = false;
    }

    public void SetMQ_ISee()
    {
        playerMQSeenSanctumHandover = true;
    }
}
