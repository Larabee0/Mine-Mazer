using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Motes : UIToolkitBase
{
    private const float MoteDisplayTime = 1.5f;
    private const int MoteOffScreenPixels = 900;

    private PlayerUIController coroutineSource;
    private Coroutine moteRunCoroutine;
    private Queue<ItemGetMote> items = new();
    private List<TemplateContainer> displayingMotes = new();

    public Motes(VisualElement rootVisualElement, PlayerUIController coroutineSource) : base(rootVisualElement)
    {
        this.coroutineSource = coroutineSource;
        ItemUtility.OnItemGoalHit += OnHitGoal;
    }


    public override void Query()
    {

    }

    public override void Bind()
    {
        
    }

    public void BindInventory()
    {
        Inventory.Instance.OnItemPickUp += OnItemPickUp;
    }


    public void UnbindInventory()
    {
        Inventory.Instance.OnItemPickUp -= OnItemPickUp;
    }

    private void OnItemPickUp(Item item, int arg2)
    {
        if(ItemUtility.IsGoalable(item))
        {
            ItemUtility.UpdateQuantityGoal(item);
            items.Enqueue(new() { item = item, itemCount = arg2, target = ItemUtility.GetItemQuantityGoal(item) });
            RunMoteCoroutine();
        }
    }


    private void OnHitGoal(Item item, int arg2, int arg3)
    {
        items.Enqueue(new()
        {
            item = item,
            itemCount = Inventory.Instance.inventory[item],
            hitGoal = true,
            target = arg2,
            nextTarget = arg3
        });
        RunMoteCoroutine();
    }

    private void RunMoteCoroutine()
    {
        if(moteRunCoroutine == null)
        {
            moteRunCoroutine = coroutineSource.StartCoroutine(MoteCoroutine());
        }
    }

    private IEnumerator MoteCoroutine()
    {
        yield return null;

        while (displayingMotes.Count > 0 || items.Count > 0)
        {
            BulkAddMotes();

            for (int i = 0; i < displayingMotes.Count; i++)
            {
                displayingMotes[i].style.top = -(i*3);
                yield return null;
            }

            yield return new WaitForSeconds(MoteDisplayTime);
            var disposal = displayingMotes.Dequeue();
            disposal.style.top = 1*3;
            for (int i = 0; i < MoteOffScreenPixels; i++)
            {
                disposal.style.left = -i;
                yield return null;
            }
            RootVisualElement.Remove(disposal);
        }

        moteRunCoroutine = null;
    }

    private void BulkAddMotes()
    {
        List<ItemGetMote> goals = new();
        ItemGetMote moteForDisplay;
        while (items.Count > 10)
        {
            moteForDisplay = items.Dequeue();
            if (moteForDisplay.hitGoal)
            {
                goals.Add(moteForDisplay);
            }
        }
        while(items.Count > 0)
        {
            goals.Add(items.Dequeue());
        }
        
        goals.ForEach(ele => displayingMotes.Add(AddMote(ele)));

    }

    private TemplateContainer AddMote(ItemGetMote mote)
    {
        var container = coroutineSource.ItemMote.Instantiate();
        container.style.position = Position.Absolute;
        RootVisualElement.Add(container);

        container.Q<Label>("MoteTitle").text = string.Format("Picked up {0} ({1}/{2})", mote.item, mote.itemCount, mote.target);
        Label desc = container.Q<Label>("MoteDescription");
        
        if (mote.newItem) {

            desc.text = string.Format("New compendium page unlocked: {0}!",mote.item); 
        }
        else if (mote.hitGoal)
        {
            desc.text = string.Format("Hit collection goal {0}! New goal {1}", mote.target, mote.nextTarget);
        }
        else
        {
            desc.text = "";
        }


        return container;
    }
}


public struct ItemGetMote
{
    public int itemCount;
    public int target;
    public int nextTarget;
    public bool hitGoal;
    public bool newItem;
    public Item item;
}