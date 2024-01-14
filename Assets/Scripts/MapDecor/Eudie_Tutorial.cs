using MazeGame.Navigation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eudie_Tutorial : NPCTrade
{
    [Header("Eudie")]
    public Transform eudieWaypoint;

    private WorldWayPoint eudieWWP;

    [SerializeField] private MapResource[] starterItems;

    private bool eudieSleep = true;
    private bool lumenGather = false;
    private bool mineWall = false;
    public void ShowEudieWaypoint()
    {
        eudieSleep = false;
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Eudie", eudieWaypoint.position, Color.yellow);
    }

    public override void Interact()
    {
        if (mineWall)
        {

        }
        else if(lumenGather)
        {
            UnlockPointer();
            AttemptTrade();
        }
        else
        {
            Dialogue.ExecuteBlock("Start Eudie");
        }
    }

    public override string GetToolTipText()
    {
        if(!eudieSleep && eudieWWP != null)
        {
            WorldWayPointsController.Instance.RemoveWaypoint(eudieWWP);
            eudieWWP =null;
        }

        if (mineWall)
        {
            return "Eudie. Current Objective, break out of room";
        }
        else
        {
            return base.GetToolTipText();
        }
        
    }

    public void PickupLumen()
    {
        lumenGather = true;
    }

    public void RecieveStarterItems()
    {
        for (int i = 0; i < starterItems.Length; i++)
        {
            Inventory.Instance.AddItem(starterItems[i].ItemStats.type, 1, Instantiate(starterItems[i]));
        }
    }

    public void GivenLumen()
    {
        lumenGather = false;
        mineWall = true;
    }
}
