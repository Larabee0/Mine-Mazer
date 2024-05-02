using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Motes : UIToolkitBase
{
    private const float MoteDisplayTime = 1.5f;
    private const int MoteOffScreenSpeed = 400;

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
        var element = coroutineSource.CompendiumUI.CompendiumItems.Find(element => element.targetItem == item && !element.shownMote);
        ItemUtility.UpdateQuantityGoal(item);
        if (ItemUtility.IsGoalable(item) && element  != null)
        {
            element.shownMote = true;
            items.Enqueue(new() { item = item, itemCount = arg2, target = ItemUtility.GetItemQuantityGoal(item), newItem = true });
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
        bool first = true;
        while (displayingMotes.Count > 0 || items.Count > 0)
        {
            BulkAddMotes();

            for (int i = 0; i < displayingMotes.Count; i++)
            {
                displayingMotes[i].style.top = (i*6);
                yield return null;
            }

            if (first)
            {
                first = false;
                yield return new WaitForSeconds(MoteDisplayTime);
            }
            else
            {
                yield return new WaitForSeconds(MoteDisplayTime*0.25f);
            }
            
            var disposal = displayingMotes.Pop();
            disposal.style.top = (int)disposal.style.top.value.value + (1 *6);
            float offset = -1;
            while (IsElementOnScreen(RootVisualElement.parent,disposal))
            {
                disposal.style.left = offset;
                yield return null;
                offset-=Time.deltaTime * MoteOffScreenSpeed;
            }
            RootVisualElement.Remove(disposal);
        }

        moteRunCoroutine = null;
    }

    private void BulkAddMotes()
    {
        Stack<ItemGetMote> goals = new();
        ItemGetMote moteForDisplay;
        while (items.Count > 10)
        {
            moteForDisplay = items.Dequeue();
            if (moteForDisplay.hitGoal)
            {
                goals.Push(moteForDisplay);
            }
        }
        while(items.Count > 0)
        {
            goals.Push(items.Dequeue());
        }
        
        while(goals.Count > 0) {
            ItemGetMote element = goals.Pop();
            displayingMotes.Add(AddMote(element));
        }

        //goals.ForEach(ele => displayingMotes.Add(AddMote(ele)));

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

    public static bool IsElementOnScreen(VisualElement pScreenRoot, VisualElement pElement)
    {
        //Detect if any portion of it IS on screen
        var overlaps = pScreenRoot.worldBound.Overlaps(pElement.worldBound);
        return overlaps;
        //See if any portion of it is NOT on screen
        //return pElement.worldBound.x <= 0 || pElement.worldBound.xMax >= pScreenRoot.worldBound.width ||
        //    pElement.worldBound.yMin <= 0 || pElement.worldBound.yMax >= pScreenRoot.worldBound.height;
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