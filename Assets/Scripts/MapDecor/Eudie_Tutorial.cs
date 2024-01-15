using MazeGame.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eudie_Tutorial : NPCTrade
{
    [Header("Eudie")]
    public Transform eudieWaypoint;

    private WorldWayPoint eudieWWP;

    [SerializeField] private Eudie_Item eudieItem;
    [SerializeField] private MapResource[] starterItems;
    [SerializeField] private BreakableWall[] tutorialAreaWalls;
    private WorldWayPoint[] wallWWP;

    private bool eudieSleep = true;
    private bool lumenGather = false;
    private bool mineWall = false;
    private bool pickUpEudie = false;
    private bool eudieInInventory = false;

    private void Awake()
    {
        if(tutorialAreaWalls.Length == 0)
        {
            Debug.LogError("Eudie has no tutorial break out walls assigned! The game cannot progress with out them", gameObject);
        }

        for (int i = 0; i < tutorialAreaWalls.Length; i++)
        {
            tutorialAreaWalls[i].OnWallBreak += OnWallBroken;
        }

        wallWWP = new WorldWayPoint[tutorialAreaWalls.Length];
    }

    private void OnWallBroken()
    {
        mineWall = false;
        for (int i = 0; i < wallWWP.Length; i++)
        {
            tutorialAreaWalls[i].OnWallBreak -= OnWallBroken;
            WorldWayPointsController.Instance.RemoveWaypoint(wallWWP[i]);
        }
        Dialogue.ExecuteBlock("Player Breaks Wall"); // next tutorial blocl
        PlayerUIController.Instance.SetMiniMapVisible(true);
    }

    public void ShowEudieWaypoint()
    {
        eudieSleep = false;
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Eudie", eudieWaypoint.position, Color.yellow);
    }

    public override void Interact()
    {
        if (pickUpEudie)
        {
            TransformEudieToItem();
        }
        else if (mineWall)
        {
            return;
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
        else if (pickUpEudie || eudieInInventory)
        {
            return null;
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

        for (int i = 0; i < tutorialAreaWalls.Length; i++)
        {
            wallWWP[i] = WorldWayPointsController.Instance.AddwayPoint(tutorialAreaWalls[i].GetToolTipText(), tutorialAreaWalls[i].transform.position, Color.green);
        }
    }

    public void PickUpEudie()
    {
        eudieItem.pickUpEudie = pickUpEudie = true;
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Eudie", eudieWaypoint.position, Color.yellow);
    }

    public void TransformEudieToItem()
    {
        if (eudieWWP != null)
        {
            WorldWayPointsController.Instance.RemoveWaypoint(eudieWWP);
            eudieWWP = null;
        }
        eudieInInventory = true;
        eudieItem.PickUpEudieItem();
    }
}
