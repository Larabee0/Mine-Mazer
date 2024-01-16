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
    [SerializeField] private GameObject lumen;
    private WorldWayPoint[] wallWWP;
    [Header("Debug")]
    public bool SkipToPickUpEudie = false;

    private bool eudieSleep = true;
    private bool lumenGather = false;
    private bool mineWall = false;
    private bool pickUpEudie = false;
    private bool eudieInInventory = false;
    private bool atColony = false;

    // barks allowed
    private bool ladderBark = true;

    private void Awake()
    {
        lumen.SetActive(false);
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

    protected override void Start()
    {
        SpatialParadoxGenerator mapGen = FindObjectOfType<SpatialParadoxGenerator>();
        mapGen.OnEnterLadderSection += LadderBark;
        mapGen.OnEnterColonySection += EnterColonyBark;
    }


    private void EnterColonyBark()
    {
        Invoke(nameof(EnterColonyDelay), 2.5f);
    }

    private void EnterColonyDelay()
    {
        FindObjectOfType<SpatialParadoxGenerator>().OnEnterColonySection -= EnterColonyBark;
        Dialogue.ExecuteBlock("Bark In Colony");

        InteractMessage.Instance.SetObjective("Drop off Eudie in the center.");
    }

    private void LadderBark()
    {
        if(ladderBark)
        {
            Dialogue.ExecuteBlock("Bark Ladder");
            Inventory.Instance.StartCoroutine(LadderBarkCoolDown());
        }
    }

    private IEnumerator LadderBarkCoolDown()
    {
        ladderBark = false;
        yield return new WaitForSeconds(60);
        ladderBark = true;
    }

    public void ClearObjectiveFungus()
    {
        InteractMessage.Instance.ClearObjective();
    }

    private void OnWallBroken()
    {
        InteractMessage.Instance.ClearObjective();
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
        if (SkipToPickUpEudie)
        {
            eudieSleep = false;
            RecieveStarterItems();
            PickUpEudie();
            for (int i = 0; i < wallWWP.Length; i++)
            {
                tutorialAreaWalls[i].OnWallBreak -= OnWallBroken;
            }
        }
        else
        {
            InteractMessage.Instance.SetObjective("Speak to the Lumenite");
            eudieSleep = false;
            eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Lumenite", eudieWaypoint.position, Color.yellow);
        }
    }

    public override void Interact()
    {
        if (atColony)
        {
            Dialogue.ExecuteBlock("Come Back Later");
        }
        else if (pickUpEudie)
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
            InteractMessage.Instance.ClearObjective();
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
        else if(pickUpEudie)
        {
            return eudieItem.GetToolTipText();
        }
        else if (eudieInInventory)
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
        lumen.SetActive(true);
        lumenGather = true;
        InteractMessage.Instance.SetObjective("Give Lumen to Eudie");
    }

    public void RecieveStarterItems()
    {
        for (int i = 0; i < starterItems.Length; i++)
        {
            Inventory.Instance.AddItem(starterItems[i].ItemStats.type, 1, Instantiate(starterItems[i]));
        }

        Inventory.Instance.TryMoveItemToHand(Item.Torch);
    }

    public void GivenLumen()
    {
        InteractMessage.Instance.SetObjective("Break out of the current cave");
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
        InteractMessage.Instance.SetObjective("Pick up Eudie");
    }

    public void PutEudieDown()
    {
        pickUpEudie= mineWall = eudieInInventory = false;
        atColony = true;
        eudieItem.MakePlaceable();
        eudieItem.OnEudiePlaced += EudiePlaced;

        eudieItem.putDownEudieToolTip = true;
        if (Inventory.Instance.CurHeldItem != Item.Eudie)
        {
            Inventory.Instance.TryMoveItemToHand(Item.Eudie);
        }
        InteractMessage.Instance.ShowInteraction(eudieItem.GetToolTipText(), null, Color.white);
    }

    private void EudiePlaced()
    {
        Invoke(nameof(DelayedHunger), 0.2f);
    }

    private void DelayedHunger()
    {
        Dialogue.ExecuteBlock("Lunch Time");
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

    public void GetLunch()
    {
        InteractMessage.Instance.SetObjective("Talk to Eudies friends in the colony & trade for food.");
    }
}
