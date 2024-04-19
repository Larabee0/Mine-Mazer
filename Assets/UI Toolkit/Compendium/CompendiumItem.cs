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
    private int curCount;
    private Label boundLabel;

    public int CompareTo(CompendiumItem other)
    {
        return order.CompareTo(other.order);
    }

    public void Bind(Label label)
    {
        if (targetItem == Item.None) return;
        boundLabel = label;
        if(curCount == 0 && !alwaysDisplay)
        {
            label.style.display = DisplayStyle.None;
        }
    }

    public void OnTargetItemValueChanged(int newValue)
    {
        if(targetItem == Item.None) return;
        if(curCount == 0 && newValue >0)
        {

            boundLabel.style.display = DisplayStyle.Flex;
        }
        boundLabel.text = string.Format("{0} (x{1})", title, newValue);
    }
}
