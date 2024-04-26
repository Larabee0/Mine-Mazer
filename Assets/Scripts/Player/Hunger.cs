using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hunger : MonoBehaviour
{
    private static Hunger instance;
    public static Hunger Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected Hunger instance not found. Order of operations issue? Or Hunger is disabled/missing.");
            }
            return instance;
        }
        private set
        {
            if (value != null && instance == null)
            {
                instance = value;
            }
        }
    }

    [SerializeField] private float hungerLevel = 1;
    [SerializeField] private float hungerSpeed = 1;
    public float CurrentHungerLevel => hungerLevel;
    public Action OnStarvedToDeath;

    private void Awake()
    {
        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        PlayerUIController.Instance.SetHungerBarProgress(hungerLevel);
    }

    public void StartHunger()
    {
        PlayerUIController.Instance.SetHungerVisible(true);
        StartCoroutine(RunHunger());
    }

    private IEnumerator RunHunger()
    {
        while(hungerLevel > 0)
        {
            hungerLevel -= Time.deltaTime * hungerSpeed;
            PlayerUIController.Instance.SetHungerBarProgress(CurrentHungerLevel);
            yield return null;
        }
        OnStarvedToDeath?.Invoke();
    }

    public void SetToFull()
    {
        hungerLevel = 1;
        PlayerUIController.Instance.SetHungerBarProgress(CurrentHungerLevel);
    }
}
