using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : UIToolkitBase
{
    public RadialMenuItem inventoryUI;


    public InventoryController(VisualElement root) : base(root)
    {
        Query();
        Bind();        
    }

    public override void Bind()
    {
        inventoryUI.RegisterCallback<ClickEvent>(ev => CloseInventory());
        inventoryUI.RegisterCallback<NavigationSubmitEvent>(ev => CloseInventory());
    }

    public override void Query()
    {
        inventoryUI = RootQ<RadialMenuItem>("RadialInventory");
    }

    public void InventorySelectAttempt(Item targetItem)
    {
        if (Inventory.Instance&& Inventory.Instance.inventory.ContainsKey(targetItem))
        {
            Inventory.Instance.TryMoveItemToHand(targetItem);
        }
    }

    public void OpenIventory()
    {
        RootVisualElement.style.display = DisplayStyle.Flex;
        Dictionary<Item, int> inv = Inventory.Instance.inventory;
        List<Item> items = new(inv.Keys);
        List<string> inventoryForDisplay = new() { "Compendium" };
        List<Action> inventoryActions = new() { delegate () { OpenCompendium(); } };
        for (int i = 0; i < items.Count; i++)
        {
            inventoryForDisplay.Add(string.Format("{0} (x{1})", items[i].ToString(), inv[items[i]]));
            var item = items[i];
            inventoryActions.Add(delegate () { InventorySelectAttempt(item); });
        }

        inventoryUI.PushInventory(inventoryForDisplay,inventoryActions);
        InputManager.Instance.UnlockPointer();

        PlayerUIController.Instance.StartCoroutine(UpdateUI());

        PlayerUIController.Instance.ShowCrosshair = false;
    }

    private IEnumerator UpdateUI()
    {
        yield return new WaitForEndOfFrame();
        inventoryUI.UpdateLabels();
    }

    public void CloseInventory()
    {
        RootVisualElement.style.display = DisplayStyle.None;
        
        InputManager.Instance.LockPointer();
        PlayerUIController.Instance.ShowCrosshair = true;
    }

    public void OpenCompendium()
    {
        CloseInventory();
        Debug.Log("Open compendium command");
        PlayerUIController.Instance.CompendiumUI.SetCompendiumUIActive(true);
    }
}
