using Fungus;
using MazeGame.Input;
using MazeGame.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        EudieHandOff();
    }

    public void PlayCaveAmbiance()
    {
        allowSceneChange = true;
        caveAmbience.Play();
    }

    public void Tutorial_Camera()
    {

        flowChartDelayedExecute = StartCoroutine(DelayedFlowChartExecute("Tutorial Camera", tutorialDelayTime));
    }

    public void Tutorial_Movement()
    {
        flowChartDelayedExecute = StartCoroutine(DelayedFlowChartExecute("Tutorial Movement", tutorialDelayTime));
    }

    public void EudieHandOff()
    {
        PlayerUIController.Instance.ShowCrosshair = true;
        FindObjectOfType<Eudie_Tutorial>().ShowEudieWaypoint(skipToPickUpEudie);
    }

    private IEnumerator DelayedFlowChartExecute(string command, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        tutorialFlowChart.ExecuteBlock(command);
        flowChartDelayedExecute = null;
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
        WorldWayPointsController.Instance.StartWWPC();
        PlayerUIController.Instance.FadeIn(screenFadeTime);
    }

    public void FadeOut()
    {
        PlayerUIController.Instance.FadeOut(screenFadeTime*0.5f);
    }
}
