using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompendiumController : UIToolkitBase
{
    private CompendiumUI compendiumRef;
    private ScrollView outline;
    private List<ListViewHeadingPairs> subHeadings;
    private List<string> categories ;

    private Label titleText;
    private Label summaryText;
    private Label bodyText;
    private VisualElement bigIcon;

    private ItemMote overallGoalsMote;
    private bool beOpened = false;

    public CompendiumController(VisualElement rootVisualElement, CompendiumUI compendium) : base(rootVisualElement)
    {
        compendiumRef = compendium;
        Query();
        Bind();
    }

    public override void Query()
    {
        outline = RootQ<ScrollView>("Outline");
        titleText = RootQ<Label>("TitleText");
        summaryText = RootQ<Label>("SummaryText");
        bodyText = RootQ<Label>("BodyText");
        bigIcon = RootQ("BigIcon");
    }

    public override void Bind()
    {
        InputManager.Instance.advanceDialogueButton.OnButtonReleased += OnAdvancedDialogue;
    }

    private void OnAdvancedDialogue()
    {
        if (Open)
        {
            SetActive(false);
        }
    }

    public void SetActive(bool active)
    {
        RootVisualElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        if (active)
        {
            if (!beOpened)
            {
                OnHoverGoals();
            }
            beOpened = true;
            if (PlayerUIController.Instance.InventoryMenuUI.Open)
            {
                PlayerUIController.Instance.InventoryMenuUI.CloseInventory();
            }
            InputManager.Instance.UnlockPointer();
        }
        else
        {
            InputManager.Instance.LockPointer();
        }
    }


    public void CreateParts()
    {
        List<CompendiumItem> items = compendiumRef.CompendiumItems;
        items.Sort();
        Dictionary<string, List<CompendiumItem>> categoriesSet = new();
        items.ForEach(item =>
        {
            if (categoriesSet.TryAdd(item.category, new() { item }))
            {

            }
            else
            {
                categoriesSet[item.category].Add(item);
            }
        });

        categories = new(categoriesSet.Keys);

        subHeadings = new List<ListViewHeadingPairs>();

        List<List<CompendiumItem>> categoryTitles = new();

        CreateQuestAndItemGoal();

        for (int i = 0; i < categories.Count; i++)
        {
            categoryTitles.Add(new());
            categoriesSet[categories[i]].ForEach(item => categoryTitles[^1].Add(item));
            subHeadings.Add(new(new() { text = categories[i] }, categoryTitles[^1], compendiumRef.ItemMotePrefab));
            outline.Add(subHeadings[^1].container);
        }

        subHeadings.ForEach(item =>
        {
            for (int j = 0; j < item.motes.Count; j++)
            {
                var compItem = item.titles[j];
                item.motes[j].RootVisualElement.RegisterCallback<PointerOverEvent>(ev => OnHover(compItem));
            }
        });
    }

    private void CreateQuestAndItemGoal()
    {
        overallGoalsMote = new ItemMote(compendiumRef.ItemMotePrefab.Instantiate());


        overallGoalsMote.RootVisualElement[0].AddToClassList("title");
        overallGoalsMote.RootVisualElement[0].pickingMode = PickingMode.Position;
        overallGoalsMote.RootVisualElement[0].style.borderTopColor = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderBottomColor = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderLeftColor = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderRightColor = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderTopWidth = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderBottomWidth = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderLeftWidth = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderRightWidth = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderTopLeftRadius = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderTopRightRadius = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderBottomLeftRadius = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement[0].style.borderBottomRightRadius = StyleKeyword.Initial;
        overallGoalsMote.RootVisualElement.pickingMode = PickingMode.Ignore;


        outline.Add(overallGoalsMote.RootVisualElement);
        overallGoalsMote.Title = "Item Collection Goals";
        overallGoalsMote.Description = "";
        overallGoalsMote.Icon = PlayerUIController.Instance.CompendiumIcon;
        overallGoalsMote.RootVisualElement.RegisterCallback<PointerOverEvent>(ev => OnHoverGoals());
    }

    private void OnHoverGoals()
    {
        titleText.text = overallGoalsMote.Title;
        bigIcon.style.backgroundImage = overallGoalsMote.Icon;
        bigIcon.style.display = DisplayStyle.Flex;
        summaryText.text = "List of all the items you can collect and how many you should try to get!\n The goal for an item goes up when you hit it.";

        List<Item> goalableItems = ItemUtility.GetGoalableItems();
        string goalOutline = "";
        for (int i = 0; i < goalableItems.Count; i++)
        {
            int curQuantity = 0;
            if(Inventory.Instance != null && Inventory.Instance.inventory.TryGetValue(goalableItems[i], out int value))
            {
                curQuantity = value;
            }

            string name = ItemUtility.GetItemDisplayName(goalableItems[i]);
            if( string.IsNullOrEmpty(name))
            {
                name = "MISSING ITEM DISPLAY NAME" + goalableItems[i].ToString();
            }

            goalOutline += string.Format("{2} {0} / {1}\n\n", curQuantity, ItemUtility.GetItemQuantityGoal(goalableItems[i]), name);
        }
        bodyText.text = goalOutline;

    }

    private void OnHover(CompendiumItem compendiumItem)
    {
        titleText.text = compendiumItem.title;
        summaryText.text = compendiumItem.summary;
        bodyText.text = compendiumItem.body;
        if(compendiumItem.icon != null)
        {
            bigIcon.style.backgroundImage = compendiumItem.icon;
            bigIcon.style.display = DisplayStyle.Flex;
        }
        else
        {
            bigIcon.style.display = DisplayStyle.None;
        }
    }

}

public class ListViewHeadingPairs
{
    public VisualElement container;
    public Label title;
    public ScrollView scrollView;
    public List<ItemMote> motes = new();
    public List<CompendiumItem> titles=new();

    public ListViewHeadingPairs(Label label,List<CompendiumItem> titles, VisualTreeAsset prefab)
    {
        container = new();

        title = label;
        this.titles = titles;
        label.AddToClassList("category");
        scrollView = new ScrollView();
        container.Add(title);
        container.Add(scrollView);

        titles.ForEach(item =>
        {
            TemplateContainer templateContainer = prefab.Instantiate();
            ItemMote mote = new(templateContainer);
            mote.RootVisualElement[0].AddToClassList("title");
            mote.RootVisualElement[0].pickingMode = PickingMode.Position;
            mote.RootVisualElement[0].style.borderTopColor = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderBottomColor = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderLeftColor = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderRightColor = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderTopWidth = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderBottomWidth = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderLeftWidth = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderRightWidth = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderTopLeftRadius = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderTopRightRadius = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderBottomLeftRadius = StyleKeyword.Initial;
            mote.RootVisualElement[0].style.borderBottomRightRadius = StyleKeyword.Initial;
            mote.RootVisualElement.pickingMode = PickingMode.Ignore;
            mote.Title = item.title;
            mote.Description = "";
            mote.Icon = item.icon;

            scrollView.Add(mote.RootVisualElement);
            motes.Add(mote);
            item.Bind(mote);

        });
    }
}

