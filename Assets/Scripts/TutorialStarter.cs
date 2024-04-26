using Fungus;
using MazeGame.Input;
using MazeGame.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialStarter : MonoBehaviour
{
    [SerializeField] private float screenFadeTime = 3f;
    [SerializeField] private float tutorialDelayTime = 2f;
    [SerializeField] private Transform EudieWayPoint;
    [SerializeField] private AudioSource caveAmbience;
    public bool allowSceneChange = false;
    [Header("Debug")]
    public bool skipTutorial = false;
    public bool skipToPickUpEudie = false;

    private Flowchart tutorialFlowChart;

    private Coroutine flowChartDelayedExecute =null;

    private void Awake()
    {
        tutorialFlowChart = GetComponent<Flowchart>();
        TutorialStarter[] tutors = FindObjectsOfType<TutorialStarter>();
        for (int i = 0; i < tutors.Length; i++)
        {
            if (tutors[i].allowSceneChange)
            {
                Destroy(tutors[i].gameObject);
            }
        }
    }

    public void StartTutorialScript()
    { 
        tutorialFlowChart.SetBooleanVariable("Gamepad", InputManager.GamePadPresent);
        if (skipTutorial)
        {
            allowSceneChange = true;
            LockPointer();
            FadeOut();
            Invoke(nameof(SkipTutorial), 10f);
            // SkipTutorial();
        }
        else
        {
            Tutorial_Backstory();
        }
    }

    private void Tutorial_Backstory()
    {
        InteractMessage.Instance.HideInteraction();
        PlayerUIController.Instance.SetMiniMapVisible(false);
        tutorialFlowChart.ExecuteBlock("Tutorial Start");
    }

    private void SkipTutorial()
    {
        FadeIn();

        PlayerUIController.Instance.ShowCrosshair = true;
        WorldWayPointsController.Instance.StartWWPC();
        EudieHandOff();
    }

    public void PlayCaveAmbiance()
    {
        allowSceneChange = true;
        caveAmbience.Play();
    }

    public void Tutorial_Camera()
    {
        PlayerUIController.Instance.ShowCrosshair = true;
        Invoke(nameof(TutCamExecute), tutorialDelayTime);
        // flowChartDelayedExecute = StartCoroutine(DelayedFlowChartExecute("Tutorial Camera", tutorialDelayTime));
    }

    private void TutCamExecute()
    {
        string cameraTutorialText = InputManager.GamePadPresent ? "Use Right Stick to look around" : "Use mouse to look around";
        InteractMessage.Instance.ShowInteraction(cameraTutorialText, 0, Color.white);
        InteractMessage.Instance.AllowAutoInteract(false);
        Invoke(nameof(HideInteract), tutorialDelayTime);
        Tutorial_Movement();
    }

    public void Tutorial_Movement()
    {
        Invoke(nameof(TutMovExecute), tutorialDelayTime*2.5f);
        //flowChartDelayedExecute = StartCoroutine(DelayedFlowChartExecute("Tutorial Movement", tutorialDelayTime));
    }

    private void TutMovExecute()
    {
        string movementTutorialText = InputManager.GamePadPresent ? "Use Left Stick to move" : "Use WASD to move";

        InteractMessage.Instance.AllowAutoInteract(true);
        InteractMessage.Instance.ShowInteraction(movementTutorialText, 0, Color.white);
        InteractMessage.Instance.AllowAutoInteract(false);
        Invoke(nameof(HideInteract), tutorialDelayTime);
        Invoke(nameof(EudieHandOff), tutorialDelayTime);
    }

    private void HideInteract()
    {
        InteractMessage.Instance.HideInteraction();
    }

    public void EudieHandOff()
    {
        InteractMessage.Instance.AllowAutoInteract(true);
        FindObjectOfType<Eudie_Tutorial>().ShowEudieWaypoint(skipToPickUpEudie);
        Hunger.Instance.OnStarvedToDeath += StarvedToDeath;
        PlayerUIController.Instance.BindUnBindMotes(true);
    }

    private IEnumerator DelayedFlowChartExecute(string command, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        tutorialFlowChart.ExecuteBlock(command);
        flowChartDelayedExecute = null;
    }

    public void StarvedToDeath()
    {
        Hunger.Instance.OnStarvedToDeath -= StarvedToDeath;
        PlayerUIController.Instance.SetHungerVisible(false);
        PlayerUIController.Instance.ShowCrosshair = false;
        PlayerUIController.Instance.SetMiniMapVisible(false);
        InteractMessage.Instance.ClearObjective();
        InteractMessage.Instance.HideInteraction(true);
        WorldWayPointsController.Instance.ClearWaypoints();

        tutorialFlowChart.ExecuteBlock("StarvedToDeath");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void UnlockPointer()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.UnlockPointer();
        }
    }

    public void LockPointer()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.LockPointer();
        }
    }

    public void FadeIn()
    {
        PlayerUIController.Instance.FadeIn(screenFadeTime);
    }

    public void FadeOut()
    {
        PlayerUIController.Instance.FadeOut(screenFadeTime*0.5f);
    }
}
