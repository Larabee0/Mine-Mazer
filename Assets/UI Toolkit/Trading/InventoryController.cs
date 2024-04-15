using MazeGame.Input;
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
        inventoryUI.RegisterCallback<ClickEvent>(ev => InventorySelectAttempt());
        inventoryUI.RegisterCallback<NavigationSubmitEvent>(ev => InventorySelectAttempt());
    }

    public override void Query()
    {
        inventoryUI = RootQ<RadialMenuItem>("RadialInventory");
    }

    public void InventorySelectAttempt()
    {
        Debug.Log(inventoryUI.SelectedItem);
        Close();

        Dictionary<Item, int> inv = Inventory.Instance.inventory;
        List<Item> items = new(inv.Keys);
        Inventory.Instance.TryMoveItemToHand(items[inventoryUI.SelectedItem]);
    }

    public void Open()
    {
        RootVisualElement.style.display = DisplayStyle.Flex;
        Dictionary<Item, int> inv = Inventory.Instance.inventory;
        List<Item> items = new(inv.Keys);
        List<string> inventoryForDisplay = new();
        for (int i = 0; i < items.Count; i++)
        {
            inventoryForDisplay.Add(string.Format("{0} (x{1})", items[i].ToString(), inv[items[i]]));
        }

        inventoryUI.PushInventory(inventoryForDisplay);
        InputManager.Instance.UnlockPointer();

        PlayerUIController.Instance.StartCoroutine(UpdateUI());

        PlayerUIController.Instance.ShowCrosshair = false;
    }

    private IEnumerator UpdateUI()
    {
        yield return new WaitForEndOfFrame();
        inventoryUI.UpdateLabels();
    }

    public void Close()
    {
        RootVisualElement.style.display = DisplayStyle.None;
        
        InputManager.Instance.LockPointer();
        PlayerUIController.Instance.ShowCrosshair = true;
    }
}
