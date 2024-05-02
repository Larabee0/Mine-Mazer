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

    [SerializeField] private int defaultAnimator;
    [SerializeField] private Animator[] animators;
    [SerializeField] private AnimationTrigger[] triggers;

    private Dictionary<string, int> animationTriggers = new();

    [SerializeField] private AnimationEventCreator[] animationEvents;

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

        for (int i = 0; i < triggers.Length; i++)
        {
            var trigger = triggers[i];
            if(animationTriggers.TryAdd(trigger.methodName, i))
            {
                if (trigger.triggerOnStart)
                {
                    CallTrigger(i);
                }
            }
            
        }

        for (int i = 0; i < animationEvents.Length; i++)
        {
            animationEvents[i].ToAnimationEvent(this);
        }

        //for (int i = 0; i < animators.Length; i++)
        //{
        //    animators[i].gameObject.SetActive(false);
        //}
        //
        //animators[defaultAnimator].gameObject.SetActive(true);
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
        animators[0].SetTrigger("Equip");
        switch (item)
        {
            case Item.Torch:

            default:
                return;
        }

    }

    #region Animation Triggers
    public void CallTrigger(int index)
    {
        triggers[index].Call(this);
    }

    public void TorchDeploy()
    {
        if(animationTriggers.TryGetValue(nameof(TorchDeploy), out int index))
        {
            CallTrigger(index);
        }
    }

    #endregion

    #region Animation Event callbacks

    public void TorchDeployFinished()
    {
        Debug.Log("Torch deply fininshed");
    }

    #endregion

    [Serializable]
    public struct AnimationTrigger
    {
        public string methodName;
        public string triggerName;
        public int animatorIndex;
        public bool triggerOnStart;

        public void Call(PlayerAnimationController pac)
        {
            pac.animators[animatorIndex].SetTrigger(triggerName);
        }
    }

    [Serializable]
    public struct AnimationEventCreator
    {
        public string eventInvoke;
        public int ctrlIndex;
        public int clipInCtrlIndex;
        public float timeInClip;

        public void ToAnimationEvent(PlayerAnimationController pac)
        {
            if (string.IsNullOrWhiteSpace(eventInvoke) || string.IsNullOrEmpty(eventInvoke)) return;            
            if (pac.animators.Length <= ctrlIndex) return;
            if (pac.animators[ctrlIndex].runtimeAnimatorController.animationClips.Length <= clipInCtrlIndex) return;
            AnimationEvent animationEvent = new()
            {
                functionName = eventInvoke,
                time = timeInClip
            };
            pac.animators[ctrlIndex].runtimeAnimatorController.animationClips[clipInCtrlIndex].AddEvent(animationEvent);
        }
    }
}
