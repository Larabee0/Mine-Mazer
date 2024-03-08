using MazeGame.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    private bool skipToPickUpEudie = false;

    [SerializeField] EudieContext eudieState = EudieContext.EudieSleep;


    public enum EudieContext
    {
        EudieSleep,
        LumenGather,
        MineWall,
        PickUpEudie,
        EudieInInventory,
        AtColony
    }

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


    public override string GetToolTipText()
    {
        if (eudieState == EudieContext.EudieSleep && eudieWWP != null)
        {
            WorldWayPointsController.Instance.RemoveWaypoint(eudieWWP);
            eudieWWP = null;
        }

        return eudieState switch
        {
            EudieContext.MineWall => "Eudie. Current Objective, break out of room",
            EudieContext.PickUpEudie => eudieItem.GetToolTipText(),
            EudieContext.EudieInInventory => null,
            _ => base.GetToolTipText(),
        };
    }

    public override void Interact()
    {
        switch (eudieState)
        {
            case EudieContext.LumenGather:
                UnlockPointer();
                AttemptTrade();
                break;
            case EudieContext.MineWall:
                return;
            case EudieContext.PickUpEudie:
                TransformEudieToItem();
                break;
            case EudieContext.AtColony:
                Dialogue.ExecuteBlock("Come Back Later");
                break;
            default:
                InteractMessage.Instance.ClearObjective();
                Dialogue.ExecuteBlock("Start Eudie");
                break;
        }
    }

    #region barks
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
    #endregion

    public void ClearObjectiveFungus()
    {
        InteractMessage.Instance.ClearObjective();
    }

    public void ShowEudieWaypoint(bool skipToPickUpEudie = false)
    {
        this.skipToPickUpEudie = skipToPickUpEudie;
        if (skipToPickUpEudie)
        {
            RecieveStarterItems();
            PickUpEudie();
            for (int i = 0; i < wallWWP.Length; i++)
            {
                tutorialAreaWalls[i].OnWallBreak -= OnWallBroken;
            }
            PlayerUIController.Instance.SetMiniMapVisible(true);
        }
        else
        {
            InteractMessage.Instance.SetObjective("Speak to the Lumenite");
            eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Lumenite", eudieWaypoint.position, Color.yellow);
        }
    }

    public void RecieveStarterItems()
    {
        for (int i = 0; i < starterItems.Length; i++)
        {
            Inventory.Instance.AddItem(starterItems[i].ItemStats.type, 1, Instantiate(starterItems[i]));
        }

        Inventory.Instance.TryMoveItemToHand(Item.Torch);
    }

    public void PickupLumen()
    {
        lumen.SetActive(true);
        eudieState = EudieContext.LumenGather;
        InteractMessage.Instance.SetObjective("Give Lumen to Eudie");
    }

    public void GivenLumen()
    {
        InteractMessage.Instance.SetObjective("Break out of the current cave");
        eudieState = EudieContext.MineWall;
        Vector3 yOffset = new(0, 1.5f, 0);
        for (int i = 0; i < tutorialAreaWalls.Length; i++)
        {
            wallWWP[i] = WorldWayPointsController.Instance.AddwayPoint(tutorialAreaWalls[i].GetToolTipText(), tutorialAreaWalls[i].transform.position+ yOffset, Color.green);
        }
    }

    private void OnWallBroken()
    {
        InteractMessage.Instance.ClearObjective();

        for (int i = 0; i < wallWWP.Length; i++)
        {
            tutorialAreaWalls[i].OnWallBreak -= OnWallBroken;
            if (wallWWP[i] != null )
            {
                WorldWayPointsController.Instance.RemoveWaypoint(wallWWP[i]);
            }
        }
        Dialogue.ExecuteBlock("Player Breaks Wall"); // next tutorial blocl
        PlayerUIController.Instance.SetMiniMapVisible(true);
    }

    public void PickUpEudie()
    {
        eudieState = EudieContext.PickUpEudie;
        eudieItem.pickUpEudie = true;
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Eudie", eudieWaypoint.position, Color.yellow);
        InteractMessage.Instance.SetObjective("Pick up Eudie");
    }

    public void TransformEudieToItem()
    {
        if (eudieWWP != null)
        {
            WorldWayPointsController.Instance.RemoveWaypoint(eudieWWP);
            eudieWWP = null;
            GetComponent<NavMeshAgent>().enabled = false;
        }
        eudieState = EudieContext.EudieInInventory;
        eudieItem.PickUpEudieItem();

        SpatialParadoxGenerator mapGen = FindObjectOfType<SpatialParadoxGenerator>();
        mapGen.OnEnterLadderSection += LadderBark;
        mapGen.OnEnterColonySection += EnterColonyBark;
    }

    public void PutEudieDown()
    {
        eudieState = EudieContext.AtColony;
        eudieItem.MakePlaceable();
        eudieItem.OnEudiePlaced += EudiePlaced;
        SpatialParadoxGenerator mapGen = FindObjectOfType<SpatialParadoxGenerator>();
        mapGen.OnEnterLadderSection -= LadderBark;
        mapGen.OnEnterColonySection -= EnterColonyBark;
        GetComponent<NavMeshAgent>().enabled = true;

        eudieItem.putDownEudieToolTip = true;
        if (Inventory.Instance.CurHeldItem != Item.Eudie)
        {
            Inventory.Instance.TryMoveItemToHand(Item.Eudie);
        }
        InteractMessage.Instance.ShowInteraction(eudieItem.GetToolTipText(), null, Color.white);
    }

    // hunger triggering
    private void EudiePlaced()
    {
        Invoke(nameof(DelayedHunger), 0.2f);
    }

    private void DelayedHunger()
    {
        Dialogue.ExecuteBlock("Lunch Time");
    }

    public void GetLunch()
    {
        InteractMessage.Instance.SetObjective("Talk to Eudies friends in the colony & trade for food.");
        Hunger.Instance.StartHunger();
    }
}
