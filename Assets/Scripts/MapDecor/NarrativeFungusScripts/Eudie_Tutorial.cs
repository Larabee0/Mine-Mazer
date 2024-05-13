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
    [SerializeField] private MapResource lumen;
    [SerializeField] private Transform gate;
    [SerializeField] private ButtonInteractable gateButton;
    [SerializeField] private AudioSource gateSound;
    [SerializeField] private Vector3 raisedPosition;
    [SerializeField] private float raiseSpeed;
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
        gateButton.OnSuccessfulActivation += OnGateBeginOpening;
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

            Inventory.Instance.AddItem(tradeOptions[0].givenItem.ItemStats.type, 1, Instantiate( tradeOptions[0].givenItem));
            PlayerUIController.Instance.SetMiniMapVisible(true);
        }
        else
        {
            InteractMessage.Instance.SetObjective("Speak to the Lumenite");
            eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Lumenite", eudieWaypoint.position, Color.white,5);
        }
    }

    public void RecieveStarterItems()
    {
        for (int i = 0; i < starterItems.Length; i++)
        {
            Inventory.Instance.AddItem(starterItems[i].ItemStats.type, 1, Instantiate(starterItems[i]));
        }

        Inventory.Instance.TryMoveItemToHand(Item.Pickaxe);
    }

    public void PickupLumen()
    {
        
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Lumen Crystal", lumen.transform.position, Color.white,3);
        lumen.OnItemPickedUp += RemoveLumenWaypoint;
        eudieState = EudieContext.LumenGather;
        InteractMessage.Instance.SetObjective("Mine & Give Lumen to Eudie");
    }

    private void RemoveLumenWaypoint()
    {
        lumen.OnItemPickedUp -= RemoveLumenWaypoint;
        WorldWayPointsController.Instance.RemoveWaypoint(eudieWWP);
    }

    public void GivenLumen()
    {
        InteractMessage.Instance.SetObjective("Open the Gate with the Gate Activator and Lumen Torch");
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Gate Activator",gateButton.transform.position,Color.white,2);
        eudieState = EudieContext.MineWall;
        gateButton.GetComponent<ButtonInteractable>().SetRainbowOpacity(0.6f);
        gateButton.GetComponent<ButtonInteractable>().SetOutlineFader(true);
        gateButton.GetComponent<ButtonInteractable>().fader = true;
        gateButton.GetComponent<ButtonInteractable>().interactable=true;
    }

    private void OnGateBeginOpening()
    {
        gateButton.OnSuccessfulActivation -= OnGateBeginOpening;
        if(eudieWWP != null) { WorldWayPointsController.Instance.RemoveWaypoint(eudieWWP); }
        gateSound.Play();
        StartCoroutine(OpenGate());
    }

    private IEnumerator OpenGate()
    {
        while (gate.transform.localPosition != raisedPosition)
        {
            gate.transform.localPosition = Vector3.MoveTowards(gate.transform.localPosition, raisedPosition, Time.deltaTime * raiseSpeed);
            yield return null;
        }
        gate.gameObject.SetActive(false);
        InteractMessage.Instance.ClearObjective();
        Dialogue.ExecuteBlock("Player Breaks Wall"); // next tutorial blocl
        PlayerUIController.Instance.SetMiniMapVisible(true);
    }

    public void PickUpEudie()
    {
        eudieState = EudieContext.PickUpEudie;
        eudieItem.pickUpEudie = true;
        eudieWWP = WorldWayPointsController.Instance.AddwayPoint("Eudie", eudieWaypoint.position, Color.white,4);
        InteractMessage.Instance.SetObjective("Pick up Eudie");
        var comp = GetComponent<Follow_Player>();
        comp.OnHitPlayer += OnHitPlayer;
        comp.ZeroStoppingDistance();
    }

    private void OnHitPlayer()
    {
        var comp = GetComponent<Follow_Player>();
        comp.OnHitPlayer -= OnHitPlayer;
        TransformEudieToItem();
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
        GetComponent<NavMeshAgent>().enabled = false;

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
