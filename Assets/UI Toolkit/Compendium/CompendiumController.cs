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


    public void UpdateParts()
    {
        List<CompendiumItem> items = compendiumRef.CompendiumItems;
        items.Sort();
        Dictionary<string,List<CompendiumItem>> categoriesSet = new();
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

        for (int i = 0; i < categories.Count; i++)
        {
            categoryTitles.Add(new());
            categoriesSet[categories[i]].ForEach(item => categoryTitles[^1].Add(item));
            subHeadings.Add(new(new() { text = categories[i] }, categoryTitles[^1]));
            outline.Add(subHeadings[^1].container);
        }

        subHeadings.ForEach(item =>
        {
            for (int j = 0; j < item.labels.Count; j++)
            {
                var compItem = item.titles[j];
                item.labels[j].RegisterCallback<PointerOverEvent>(ev => OnHover(compItem));
            }
        });
    }

    private void OnHover(CompendiumItem compendiumItem)
    {
        titleText.text = compendiumItem.title;
        summaryText.text = compendiumItem.summary;
        bodyText.text = compendiumItem.body;
    }

}

public class ListViewHeadingPairs
{
    public VisualElement container;
    public Label title;
    public ScrollView scrollView;
    public List<Label> labels = new();
    public List<CompendiumItem> titles=new();

    public ListViewHeadingPairs(Label label,List<CompendiumItem> titles)
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
            Label lab= new () { text = item.title };
            lab.AddToClassList("title");
            scrollView.Add(lab);
            labels.Add(lab);
            item.Bind(lab);

        });
    }
}

