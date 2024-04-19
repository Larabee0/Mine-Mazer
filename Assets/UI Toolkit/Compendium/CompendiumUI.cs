using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompendiumUI : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private bool forceShowAll;
    [SerializeField] private List<CompendiumItem> compendiumItems;
    public List<CompendiumItem> CompendiumItems =>compendiumItems;
    private CompendiumController compendiumController;

    private void Start()
    {
        compendiumController = new CompendiumController(document.rootVisualElement.Q("Compendium"),this);
        compendiumController.UpdateParts();
        InputManager.Instance.SetUIToolkitFocus();
        if (Inventory.Instance && !forceShowAll)
        {
            Inventory.Instance.OnItemPickUp += OnItemPickUp;
        }
        else
        {
            compendiumItems.ForEach(compItem =>
            {
                compItem.OnTargetItemValueChanged(1);
            });
        }

        compendiumController.SetActive(false);
    }


    private void OnItemPickUp(Item item, int quantity)
    {
        compendiumItems.ForEach(compItem =>
        {
            if (compItem.targetItem == item)
            {
                compItem.OnTargetItemValueChanged(quantity);
            }
        });
    }

    public void SetCompendiumUIActive(bool active)
    {
        compendiumController.SetActive(active);
    }
}
