using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Item
{
    LumenCrystal,
    GoldenTrumpetMycelium,
    VelvetBud,
    Versicolor,
    Antarticite,
    Cinnabite,
    Torch,
    Pickaxe
}

[Serializable]
public class ItemStats
{
    public string name;
    public Item type;
}

public class MapResource : MonoBehaviour, IInteractable
{
    [SerializeField] protected Collider itemCollider;
    [SerializeField] protected ItemStats itemStats;
    public Vector3 heldOrenintationOffset;
    [SerializeField,Tooltip("If left blank, falls back to ItemStats.name")] protected string toolTipNameOverride;

    protected virtual string ToolTipName
    {
        get
        {
            if(string.IsNullOrEmpty( toolTipNameOverride)|| string.IsNullOrWhiteSpace(toolTipNameOverride))
            {
                return itemStats.name;
            }
            return toolTipNameOverride;
        }
    }

    public ItemStats ItemStats => itemStats;

    public virtual string GetToolTipText()
    {
        if (InputManager.GamePadPresent)
        {
            return string.Format("B to  Pick Up {0}", ToolTipName);
        }
        else
        {
            return string.Format("E to Pick Up {0}", ToolTipName);
        }
    }

    public virtual void Interact()
    {
        if(Inventory.Instance== null)
        {
            return;
        }

        Inventory.Instance.AddItem(itemStats.type, 1,this);
    }

    public virtual void SetColliderActive(bool active)
    {
        if(itemCollider != null)
        {
            itemCollider.enabled = active;
        }
    }
}
