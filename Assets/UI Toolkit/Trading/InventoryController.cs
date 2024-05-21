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
        //inventoryUI.Bind();
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

    public override void Open()
    {
        inventoryUI.style.display = DisplayStyle.Flex;
        if (Inventory.Instance == null) return;
        Dictionary<Item, int> inv = Inventory.Instance.inventory;
        Dictionary<Item, List<MapResource>> assets = Inventory.Instance.assets;
        List<Item> items = new(inv.Keys);
        List<Texture2D> icons = new() { PlayerUIController.Instance.CompendiumIcon };

        List<string> inventoryForDisplay = new() { "Compendium" };
        List<Action> inventoryActions = new() { delegate () { Close(); OpenCompendium();  } };
        for (int i = 0; i < items.Count; i++)
        {
            inventoryForDisplay.Add(string.Format("{0} (x{1})", assets[items[i]][0].ItemStats.name, inv[items[i]]));
            var item = items[i];
            inventoryActions.Add(delegate () { InventorySelectAttempt(item); Close(); });
            if (Inventory.Instance.icons.ContainsKey(item))
            {
                icons.Add(Inventory.Instance.icons[item]);
            }
            else
            {
                icons.Add(null);
            }
        }

        inventoryUI.PushInventory(inventoryForDisplay,inventoryActions,icons);
        InputManager.Instance.UnlockPointer();
        InputManager.Instance.inventoryAxis.OnAxisAngle += inventoryUI.InputAxisAngle;
        InputManager.Instance.inventoryCycle += inventoryUI.ChangePage;

        // PlayerUIController.Instance.StartCoroutine(UpdateUI());

        PlayerUIController.Instance.ShowCrosshair = false;
        base.Open();
    }

    protected override void FocusOnOpen(GeometryChangedEvent evt)
    {
        base.FocusOnOpen(evt);
        inventoryUI.UpdateLabels();
        inventoryUI.UpdateIventoryItems();
        inventoryUI.SetLabelVisibility(true);
    }

    private IEnumerator UpdateUI()
    {
        yield return new WaitForEndOfFrame();
        inventoryUI.UpdateLabels();
        inventoryUI.UpdateIventoryItems();
        yield return null;
        inventoryUI.SetLabelVisibility(true);
    }

    public override void Close()
    {
        base.Close();
        
        inventoryUI.style.display = DisplayStyle.None;

        InputManager.Instance.inventoryAxis.OnAxisAngle -= inventoryUI.InputAxisAngle;
        InputManager.Instance.inventoryCycle -= inventoryUI.ChangePage;
        InputManager.Instance.LockPointer();
        PlayerUIController.Instance.ShowCrosshair = true;
    }

    public void OpenCompendium()
    {
        Debug.Log("Open compendium command");
        PlayerUIController.Instance.CompendiumUI.SetCompendiumUIActive(true);
    }
}
