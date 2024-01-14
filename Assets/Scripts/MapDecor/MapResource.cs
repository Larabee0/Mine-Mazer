using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    Pickaxe,
    Eudie
}

public enum ItemCategory
{
    Crystal,
    Mushroom,
    Equippment,
    Lumenite
}

public static class ItemUtility
{
    private static readonly Dictionary<ItemCategory, HashSet<Item>> categoryToItems = new()
    {
        {
            ItemCategory.Crystal, new ()
            {
                Item.LumenCrystal,
                Item.Antarticite,
                Item.Cinnabite
            }
        },

        {
            ItemCategory.Mushroom, new ()
            {
                Item.GoldenTrumpetMycelium,
                Item.Versicolor,
                Item.VelvetBud
            }
        },

        {
            ItemCategory.Equippment, new ()
            {
                Item.Torch,
                Item.Pickaxe
            }
        },

        {
            ItemCategory.Lumenite, new ()
            {
                Item.Eudie
            }
        },
    };
    private static Dictionary<Item, ItemCategory> itemToCategory = null;
    public static Dictionary<ItemCategory, HashSet<Item>> CategoryToItems => categoryToItems;
    public static Dictionary<Item, ItemCategory> ItemToCategory
    {
        get
        {
            if (itemToCategory == null)
                InitItemToCategory();

            return itemToCategory;
        }
    }

    public static ItemCategory GetItemCategory(Item item)
    {
        if (itemToCategory == null)
            InitItemToCategory();

        return itemToCategory[item];
    }

    public static HashSet<Item> GetItemsInCategory(ItemCategory category)
    {
        return new HashSet<Item>(CategoryToItems[category]);
    }

    private static void InitItemToCategory()
    {
        ItemCategory max = Enum.GetValues(typeof(ItemCategory)).Cast<ItemCategory>().Max();
        itemToCategory = new();
        for (ItemCategory i = 0; i <= max; i++)
        {
            if(CategoryToItems.TryGetValue(i, out var category))
            {
                foreach(var item in category)
                {
                    itemToCategory.TryAdd(item, i);
                }
            }
        }

    }
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
