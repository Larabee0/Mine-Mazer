using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private static PlayerAnimationController instance;
    public static PlayerAnimationController Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected PlayerAnimationController instance not found. Order of operations issue? Or PlayerAnimationController is disabled/missing.");
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

    [SerializeField] private Animator animator;
    [SerializeField] private AnimatorController animationController;
    [SerializeField] private string torchDeplyStateName;

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
        AnimationEvent animationEvent = new()
        {
            functionName = nameof(OnAnimationFinished),
            time = 2f
        };
        animationController.animationClips[0].AddEvent(animationEvent);

    }

    private void OnEnable()
    {
        Inventory.Instance.OnHeldItemChanged += OnItemChanged;
    }

    private void OnDisable()
    {
        Inventory.Instance.OnHeldItemChanged -= OnItemChanged;
    }

    private void OnItemChanged(Item item)
    {
        switch (item)
        {
            case Item.Torch:

            default:
                return;
        }
    }

    public void TorchDeply()
    {
        animator.SetTrigger("Deploy");
    }

    public void OnAnimationFinished()
    {
        Debug.Log("Torche deply fininshed");
    }
}
