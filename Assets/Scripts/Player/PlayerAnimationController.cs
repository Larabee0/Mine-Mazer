using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

public enum HandsState
{
    HandsIdleEmpty,
    HandsItemIdle,
    HandsItemPutAway,
    HandsItemPullOut,
    ToEmpty,
    climbing
}


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
    private HandsState handsState = HandsState.HandsIdleEmpty;
    private MapResource tempResource;

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
        handsState = HandsState.HandsIdleEmpty;
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
        Inventory.Instance.OnHeldItemAboutToChange += OnHeldItemAboutToChange;
    }

    private void OnDisable()
    {
        Inventory.Instance.OnHeldItemChanged -= OnItemChanged;
        Inventory.Instance.OnHeldItemAboutToChange -= OnHeldItemAboutToChange;
    }


    public void FakeEmptyHand()
    {
        OnHeldItemAboutToChange();
        handsState = HandsState.ToEmpty;
        animators[0].SetBool("Empty", true);
    }

    public void LadderClimb(float dir)
    {
        animators[0].SetFloat("LadderDir", dir);
        animators[0].SetTrigger("Ladder");
        handsState = HandsState.climbing;
    }

    public void LadderEnd()
    {
        animators[0].SetBool("Empty", false);
        animators[0].SetTrigger("LadderEnd");
        Inventory.Instance.TryMoveItemToHand(Inventory.Instance.CurHeldItem.GetValueOrDefault());
    }

    private void OnHeldItemAboutToChange()
    {
        animators[0].SetTrigger("Equip");
        if(tempResource != null)
        {
            tempResource.SetMapResourceActive(false);
        }
        tempResource = Inventory.Instance.CurHeldAsset;
        switch (handsState)
        {
            case HandsState.HandsIdleEmpty:
                handsState = HandsState.HandsItemPullOut;
                break;
            case HandsState.HandsItemIdle:
                handsState = HandsState.HandsItemPutAway;
                break;
        }
    }

    private void OnItemChanged(Item item)
    {

        switch (handsState)
        {
            case HandsState.HandsIdleEmpty:
                //Inventory.Instance.CurHeldAsset.SetMapResourceActive(false);
                break;
            case HandsState.HandsItemIdle:
                break;
            case HandsState.HandsItemPutAway:
                break;
            case HandsState.HandsItemPullOut:
                break;
        }


        animators[0].SetBool("UseA", Inventory.Instance.CurHeldAsset.useIdleA); 

        switch (item)
        {
            case Item.Torch:
                break;
            case Item.Pickaxe:
                break;
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

    public void EquipMid()
    {

        Debug.Log("EquipMid");
        switch (handsState)
        {
            case HandsState.HandsItemPutAway:
                if(tempResource != null)
                {
                    tempResource.SetMapResourceActive(false);
                }
                
                Inventory.Instance.CurHeldAsset.SetMapResourceActive(true);
                handsState = HandsState.HandsItemPullOut;
                break;
            case HandsState.HandsItemPullOut:
                Inventory.Instance.CurHeldAsset.SetMapResourceActive(true);
                break;
            case HandsState.climbing:
                Inventory.Instance.CurHeldAsset.SetMapResourceActive(true);
                break;
            case HandsState.ToEmpty:
                if (tempResource != null)
                {
                    tempResource.SetMapResourceActive(false);
                }
                break;

        }

    }

    public void EquipEnd()
    {

        switch (handsState)
        {
            case HandsState.HandsItemPutAway:
                handsState = HandsState.HandsItemPullOut;
                break;
            case HandsState.HandsItemPullOut:
                handsState = HandsState.HandsItemIdle;
                break;
            case HandsState.ToEmpty:
                handsState = HandsState.HandsItemIdle;
                break;
            case HandsState.climbing:
                handsState = HandsState.HandsItemIdle;
                break;
        }
        tempResource = null;
        Debug.Log("EquipEnd");
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
        public string clipName;
        public float timeInClip;

        public void ToAnimationEvent(PlayerAnimationController pac)
        {
            if (string.IsNullOrWhiteSpace(eventInvoke) || string.IsNullOrEmpty(eventInvoke)) return;            
            if (pac.animators.Length <= ctrlIndex) return;
            
            string tempName = clipName;


            var clip = pac.animators[ctrlIndex].runtimeAnimatorController.animationClips.First(clip => clip.name == tempName);
            if(clip != null)
            {
                AnimationEvent animationEvent = new()
                {
                    functionName = eventInvoke,
                    time = timeInClip
                };
                clip.AddEvent(animationEvent);
            }
        }
    }
}
