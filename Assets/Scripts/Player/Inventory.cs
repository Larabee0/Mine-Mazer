using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Dictionary<Item, List<MapResource>> assets = new();
    public Item? CurHeldItem => inventoryOrder.Count > 0 ? inventoryOrder[curIndex] : null;
    public MapResource CurHeldAsset => inventoryOrder.Count > 0 ? assets[CurHeldItem.Value][0] : null;

    [SerializeField] private List<Item> inventoryOrder = new();
    [SerializeField] private int curIndex = -1;
    [SerializeField] private MapResource heldItem;
    [SerializeField] private Transform virtualhands;
    [SerializeField] private MapResource[] defaultItems;
    [SerializeField] private float itemNameTime = 1f;

    public Action<Item> OnHeldItemChanged;
    public Action OnHeldItemAboutToChange;

    public Action<Item, int> OnItemPickUp;
    public Action OnItemPickUpSfx;
    public Action OnItemRemoveSfx;

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
        PlayerUIController.Instance.CompendiumUI.enabled = true;
    }

    private void Start()
    {
        for (int i = 0; i < defaultItems.Length; i++)
        {
            AddItem(defaultItems[i].ItemStats.type, 1, Instantiate(defaultItems[i]));
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.scrollDirection += ScrollInventory;
            InputManager.Instance.inventoryButton.OnButtonReleased += OpenInventory;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.scrollDirection -= ScrollInventory;
            InputManager.Instance.inventoryButton.OnButtonReleased -= OpenInventory;
        }
    }

    private void OpenInventory()
    {
        if(inventory.Count > 0)
        {
            PlayerUIController.Instance.SetInventoryActive(true);
        }
    }

    public void AddItem(Item itemType, int quantity, MapResource itemInstance, bool sfx = true)
    {
        itemInstance.SetColliderActive(false);
        itemInstance.SetMapResourceActive(false);
        if (inventory.ContainsKey(itemType))
        {
            inventory[itemType] += quantity;
            assets[itemType].Add(itemInstance);
        }
        else
        {
            inventory.Add(itemType, quantity);
            assets.Add(itemType, new() { itemInstance });
            UpdateInventory();
        }
        
        itemInstance.transform.parent = virtualhands;
        itemInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(itemInstance.heldOrenintationOffset));
        itemInstance.transform.localScale = itemInstance.heldScaleOffset;
        OnItemPickUp?.Invoke(itemType, inventory[itemType]);
        if (sfx)
        {
            OnItemPickUpSfx?.Invoke();
        }
    }

    public bool TryRemoveItem(Item item, int quantity)
    {
        if(inventory.ContainsKey(item))
        {
            inventory[item]-=quantity;

            Destroy(assets[item][^1].gameObject);
            assets[item].RemoveAt(assets[item].Count -1);
            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
                assets.Remove(item);
            }
            UpdateInventory();
            return true;
        }

        return false;
    }
    public bool TryRemoveItem(Item item, int quantity, out MapResource itemInstance, bool sfx = true)
    {
        itemInstance = null;
        if (inventory.ContainsKey(item))
        {
            inventory[item] -= quantity;

            itemInstance = assets[item][^1];

            OnHeldItemAboutToChange?.Invoke();
            assets[item].RemoveAt(assets[item].Count - 1);
            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
                //Destroy(assets[item].gameObject);
                assets.Remove(item);

            }
            if (assets.TryGetValue(item, out var value) && value.Count == 0)
            {
                assets.Remove(item);
            }
            UpdateInventory();
            if (sfx)
            {
                OnItemRemoveSfx?.Invoke();
            }
            
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
            OnHeldItemAboutToChange?.Invoke();
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
        if (curIndex >= 0)
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
            curIndex = inventoryOrder.IndexOf(inventoryOrder.FirstOrDefault(item => item == oldCur));
        }
        
        if (curIndex == -1)
        {
            curIndex = 0;
        }
        MoveItemToHand();
    }

    private void MoveItemToHand()
    {
        //OnHeldItemAboutToChange?.Invoke();
        if (assets.TryGetValue(inventoryOrder[curIndex], out List<MapResource> switchTo))
        {
            if (heldItem == switchTo[0])
            {
                return;
            }
            if (heldItem != null)
            {
                //heldItem.SetMapResourceActive(false);
            }
            //switchTo[0].SetMapResourceActive(true);
            heldItem = switchTo[0];
            StopAllCoroutines();
            StartCoroutine(ShowItemNameTooltip(string.Format("{0} (x{1})",heldItem.ItemStats.name, inventory[inventoryOrder[curIndex]])));
        }
        else
        {
            Debug.LogError("Target item was not contained in the assets dictionary!");
        }
        //heldItem.SetMapResourceActive(true);
        OnHeldItemChanged?.Invoke(CurHeldItem.Value);
    }

    public void TryMoveItemToHand(Item target)
    {
        int index = inventoryOrder.IndexOf(target);
        if(index >= 0)
        {
            OnHeldItemAboutToChange?.Invoke();
            curIndex = index;
            MoveItemToHand();
        }
    }

    private IEnumerator ShowItemNameTooltip(string text)
    {
        InteractMessage.Instance.ShowInteraction(text, null, Color.white);
        yield return new WaitForSeconds(itemNameTime);
        InteractMessage.Instance.HideInteraction();
    }
}
