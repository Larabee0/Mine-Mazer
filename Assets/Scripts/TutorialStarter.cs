using Fungus;
using MazeGame.Input;
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
    [Header("Debug")]
    public bool skipTutorial = false;

    private Flowchart tutorialFlowChart;

    private Coroutine flowChartDelayedExecute =null;

    private void Awake()
    {
        tutorialFlowChart = GetComponent<Flowchart>();
    }

    private void Start()
    {
        tutorialFlowChart.SetBooleanVariable("Gamepad", InputManager.GamePadPresent);
        if (skipTutorial)
        {
            SkipTutorial();
        }
        else
        {
            Tutorial_Backstory();
        }
    }

    public void PlayCaveAmbiance()
    {
        caveAmbience.Play();
    }

    private void SkipTutorial()
    {
        EudieHandOff();
    }

    private void Tutorial_Backstory()
    {
        InteractMessage.Instance.HideInteraction();
        PlayerUIController.Instance.SetMiniMapVisible(false);
        tutorialFlowChart.ExecuteBlock("Tutorial Start");
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
        FindObjectOfType<Eudie_Tutorial>().ShowEudieWaypoint();
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
        PlayerUIController.Instance.FadeIn(screenFadeTime);
    }

    public void FadeOut()
    {
        PlayerUIController.Instance.FadeOut(0);
    }
}
