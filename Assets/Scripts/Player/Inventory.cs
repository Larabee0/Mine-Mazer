using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private static Inventory instance;
    public static Inventory Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected Inventory instance not found. Order of operations issue? Or Inventory is disabled/missing.");
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

    public Dictionary<Item, int> inventory = new();
    public Dictionary<Item, MapResource> assets = new();
    public Item? CurHeldItem => inventoryOrder.Count > 0 ? inventoryOrder[curIndex] : null;
    public MapResource CurHeldAsset => inventoryOrder.Count > 0 ? assets[CurHeldItem.Value] : null;

    [SerializeField] private List<Item> inventoryOrder = new();
    [SerializeField] private int curIndex = -1;
    [SerializeField] private MapResource heldItem;
    [SerializeField] private Transform virtualhands;
    [SerializeField] private MapResource[] defaultItems;
    [SerializeField] private float itemNameTime = 1f;

    private void Awake()
    {
        curIndex = -1;
        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
        if (InputManager.Instance == null)
        {
            return;
        }

        InputManager.Instance.scrollDirection += ScrollInventory;
    }

    private void Start()
    {
        for (int i = 0; i < defaultItems.Length; i++)
        {
            AddItem(defaultItems[i].ItemStats.type, 1, Instantiate(defaultItems[i]));
        }
    }

    public void AddItem(Item itemType, int quantity, MapResource itemInstance)
    {
        if (inventory.ContainsKey(itemType))
        {
            inventory[itemType] += quantity;
            Destroy(itemInstance.gameObject);
        }
        else
        {
            inventory.Add(itemType, quantity);
            assets.Add(itemType, itemInstance);
            itemInstance.SetColliderActive(false);
            itemInstance.SetMapResourceActive(false);
            itemInstance.transform.parent = virtualhands;
            itemInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(itemInstance.heldOrenintationOffset));
            UpdateInventory();
        }
    }

    public bool TryRemoveItem(Item item, int quantity)
    {
        if(inventory.ContainsKey(item))
        {
            inventory[item]-=quantity;
            
            if(inventory[item] <= 0)
            {
                inventory.Remove(item);
                Destroy(assets[item].gameObject);
                assets.Remove(item);
                
            }
            UpdateInventory();
            return true;
        }

        return false;
    }
    public bool TryRemoveItem(Item item, int quantity, out MapResource itemInstance)
    {
        itemInstance = null;
        if (inventory.ContainsKey(item))
        {
            inventory[item] -= quantity;

            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
                itemInstance = assets[item];
                //Destroy(assets[item].gameObject);
                assets.Remove(item);

            }
            UpdateInventory();
            return true;
        }

        return false;
    }

    public bool CanTrade(Item taken, int take = 1)
    {
        if (inventory.TryGetValue(taken, out int quantity))
        {
            if(quantity >= take)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    public Dictionary<Item,int> GetAllItemsMatchingCategory(ItemCategory taken)
    {
        HashSet<Item> validItems = ItemUtility.GetItemsInCategory(taken);

        Dictionary<Item, int> playerItems = new();

        foreach (Item item in validItems)
        {
            if(inventory.TryGetValue(item,out int quantity))
            {
                playerItems.TryAdd(item, quantity);
            }
        }
        
        if(playerItems.Count == 0)
        {
            return null;
        }

        return playerItems;
    }

    public bool TryGetItem(Item item, out KeyValuePair<Item, int> itemQuantity)
    {
        if(inventory.TryGetValue(item, out int value))
        {
            itemQuantity = new(item,value);
            return true;
        }
        itemQuantity = new(item, 0);
        return false;
    }

    private void ScrollInventory(int axis)
    {
        if (inventory.Count > 1)
        {
            curIndex = WrapInventoryIndex(axis);
            MoveItemToHand();
        }
    }

    private int WrapInventoryIndex(int axis)
    {
        if (inventoryOrder.Count > 0)
        {
            curIndex += axis;
            curIndex = curIndex < 0 ? inventoryOrder.Count - 1 : curIndex;
            curIndex = curIndex < inventoryOrder.Count ? curIndex : 0;
            return curIndex;
        }
        return 0;
    }

    private void UpdateInventory()
    {
        int oldIndex = curIndex;
        Item oldCur = Item.LumenCrystal;
        if (curIndex > 0)
        {
            oldCur = inventoryOrder[curIndex];
        }
        
        inventoryOrder.Clear();
        inventoryOrder.AddRange(inventory.Keys);
        if (inventoryOrder.Count == 0)
        {
            return;
        }

        if(oldIndex >= 0)
        {
            curIndex = inventoryOrder.IndexOf(oldCur);
        }
        
        if (curIndex == -1)
        {
            curIndex = 0;
        }
        MoveItemToHand();
    }

    private void MoveItemToHand()
    {
        if (assets.TryGetValue(inventoryOrder[curIndex], out MapResource switchTo))
        {
            if (heldItem == switchTo)
            {
                return;
            }
            if (heldItem != null)
            {
                heldItem.SetMapResourceActive(false);
            }
            switchTo.SetMapResourceActive(true);
            heldItem = switchTo;
            StopAllCoroutines();
            StartCoroutine(NameItem(heldItem.ItemStats.name));
        }
        else
        {
            Debug.LogError("Target item was not contained in the assets dictionary!");
        }
    }

    public void TryMoveItemToHand(Item target)
    {
        int index = inventoryOrder.IndexOf(target);
        if(index >= 0)
        {
            curIndex = index;
            MoveItemToHand();
        }
    }

    private IEnumerator NameItem(string text)
    {
        InteractMessage.Instance.ShowInteraction(text, null, Color.white);
        yield return new WaitForSeconds(itemNameTime);
        InteractMessage.Instance.HideInteraction();
    }
}
