using System;
using System.Collections.Generic;
using System.Linq;

public static class ItemUtility
{
    private static Dictionary<Item, string> itemDisplayNames = new()
    {
        {
            Item.Pickaxe,
            "Sturdy Miner's Pickaxe"
        },
        {
            Item.Antarticite,
            "Antarticite"
        },
        {
            Item.Versicolor,
            "Versicolor"
        },
        {
            Item.FicusWood,
            "Ficus Wood"
        },
        {
            Item.GoldenTrumpetMycelium,
            "Golden Trumpet Mycelium"
        },
        {
            Item.HeartNode,
            "Heart Node"
        },
        {
            Item.SanctumMachine,
            "Sanctum Machine"
        },
        {
            Item.Soup,
            "Mushroom Soup"
        },
        {
            Item.Eudie,
            "Eudie"
        },
        {
            Item.BrokenHeart,
            "Broken Heart"
        },
        {
            Item.VelvetBud,
            "Velvet Bud"
        },
        {
            Item.ClockworkMechanism,
            "Clockwork Mechanism"
        },
        {
            Item.Cinnabite,
            "Cinnabite"
        },
        {
            Item.GlanceiteResonator,
            "Glanceite Resonator"
        },
        {
            Item.StagnationBeacon,
            "Stagnation Beacon"
        },
        {
            Item.Torch,
            "Lumen Torch"
        },
        {
            Item.LumenCrystal,
            "Lumen Crystal"
        }

    };

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


    private static readonly Dictionary<Item, int> initialItemGoals = new()
    {
        {
            Item.Versicolor,
            25
        },
        {
            Item.VelvetBud,
            25
        },
        {
            Item.GoldenTrumpetMycelium,
            25
        },
        {
            Item.FicusWood,
            10
        },
        {
            Item.LumenCrystal,
            10
        },
        {
            Item.Antarticite,
            5
        },
        {
            Item.Cinnabite,
            10
        },
        {
            Item.Soup,
            25
        },
        {
            Item.Torch,
            5
        }

    };

    private static Dictionary<Item, int> pickUpGoals = new();

    public static Action<Item, int, int> OnItemGoalHit;

    public static string GetItemDisplayName(Item item)
    {
        if(itemDisplayNames.ContainsKey(item)) return itemDisplayNames[item]; return null;
    }

    public static int GetItemQuantityGoal(Item item)
    {
        if (pickUpGoals.ContainsKey(item))
        {
            return pickUpGoals[item];
        }
        else if (IsGoalable(item))
        {
            pickUpGoals.Add(item, GetInitialGoal(item));
            return pickUpGoals[item];
        }
        return -1;
    }

    public static void UpdateQuantityGoal(Item item)
    {
        if (IsGoalable(item)&&Inventory.Instance)
        {
            if (Inventory.Instance.inventory.TryGetValue(item, out int curQuantity))
            {
                int curGoal = GetItemQuantityGoal(item);
                if (curQuantity >= curGoal)
                {
                    pickUpGoals[item] *= 2;
                    OnItemGoalHit?.Invoke(item, curGoal, curGoal * 2);
                }
            }
        }
    }

    public static bool IsGoalable(Item item)
    {
        return initialItemGoals.ContainsKey(item);
    }

    public static List<Item> GetGoalableItems()
    {
        return new(initialItemGoals.Keys);
    }

    public static int GetInitialGoal(Item item)
    {
        if (initialItemGoals.ContainsKey(item))
        {
            return initialItemGoals[item];
        }
        return -1;
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
