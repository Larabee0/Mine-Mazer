using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

[System.Serializable]
public class CompendiumItem : IComparable<CompendiumItem>
{
    public string title;
    public string category;
    public string summary;
    public string body;
    public bool alwaysDisplay;
    public Item targetItem;
    public int order;
    public Texture2D icon;
    private int curCount;
    private ItemMote boundLabel;

    public bool shownMote= false;

    public int CompareTo(CompendiumItem other)
    {
        return order.CompareTo(other.order);
    }

    public void Bind(ItemMote label)
    {
        if (targetItem == Item.None) return;
        boundLabel = label;
        if(curCount == 0 && !alwaysDisplay)
        {
            label.RootVisualElement.style.display = DisplayStyle.None;
        }
    }

    public void OnTargetItemValueChanged(int newValue)
    {
        if(targetItem == Item.None) return;
        if(curCount == 0 && newValue >0)
        {

            boundLabel.RootVisualElement.style.display = DisplayStyle.Flex;
        }
        boundLabel.Description = string.Format("{0} / {1}", newValue, ItemUtility.GetItemQuantityGoal(targetItem));
    }
}
