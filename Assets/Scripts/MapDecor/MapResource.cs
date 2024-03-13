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
    Eudie,
    StagnationBeacon,
    BrokenHeart,
    Soup,
    FicusWood,
    ClockworkMechanism,
    GlanceiteResonator,
    HeartNode,
    SanctumMachine
}

public enum ItemCategory
{
    Crystal,
    Mushroom,
    Equippment,
    Lumenite,
    Wood,
    Quest
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
                Item.Pickaxe,
                Item.StagnationBeacon,
                Item.Soup
            }
        },

        {
            ItemCategory.Lumenite, new ()
            {
                Item.Eudie
            }
        },

        {
            ItemCategory.Wood, new ()
            {
                Item.FicusWood
            }
        },
        {
            ItemCategory.Quest, new()
            {
                Item.BrokenHeart,
                Item.ClockworkMechanism,
                Item.GlanceiteResonator,
                Item.HeartNode,
                Item.SanctumMachine
            }
        }
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
    [SerializeField] protected bool Placeable;
    [SerializeField] protected bool Interactable = true;
    public Vector3 heldOrenintationOffset;
    public Vector3 heldpositonOffset;
    public Vector3 heldScaleOffset = Vector3.one;
    protected Vector3 originalScale;
    public Vector3 placementPositionOffset = Vector3.zero;
    public Action OnItemPickedUp;
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
    protected virtual void Awake()
    {
        originalScale = transform.localScale;
        SetColliderActive(Interactable);
    }

    public virtual string GetToolTipText()
    {
        if (InputManager.GamePadPresent)
        {
            return string.Format("A to  Pick Up {0}", ToolTipName);
        }
        else
        {
            return string.Format("Click to Pick Up {0}", ToolTipName);
        }
    }

    public virtual void Interact()
    {
        if(Inventory.Instance== null)
        {
            return;
        }
        if (TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
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

    public virtual void SetMapResourceActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public virtual bool PlaceItem()
    {
        if (Placeable)
        {
            Ray r = new(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(r, out RaycastHit hitInfo, NPC_Interact.Instance.InteractRange))
            {
                if (Inventory.Instance.TryRemoveItem(ItemStats.type, 1, out MapResource item))
                {
                    Vector3 playerPos = Inventory.Instance.transform.position;
                    Vector3 toPlayer = (playerPos- hitInfo.point).normalized;
                    toPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
                    item.gameObject.transform.parent = FindObjectOfType<SpatialParadoxGenerator>().CurPlayerSection.sectionInstance.transform;
                    item.gameObject.transform.position = hitInfo.point+ placementPositionOffset;
                    item.gameObject.transform.forward = toPlayer;
                    item.gameObject.transform.localScale = originalScale;
                    item.SetMapResourceActive(true);
                    item.SetColliderActive(true);
                    OnItemPickedUp?.Invoke();
                    //item.gameObject.transform.up = hitInfo.normal;
                    return true;
                }
            }
        }
        return false;
    }
}
