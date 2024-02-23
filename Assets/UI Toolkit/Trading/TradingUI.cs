using MazeGame.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TradingUI : MonoBehaviour
{
    private static TradingUI instance;
    public static TradingUI Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogWarning("Expected TradingUI instance not found. Order of operations issue? Or TradingUI is disabled/missing.");
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

    [SerializeField] private UIDocument playerUI;
    [SerializeField] private VisualTreeAsset tradingUiPrefab;

    private VisualElement buttonContainer;
    private VisualElement tradingInstanceRoot;
    private Label tradingText;
    private string TradingText { get => tradingText.text; set => tradingText.text = value; }
    private VisualElement playerRoot => playerUI.rootVisualElement;

    private bool specific;
    private ItemCategory targetCategory;
    private Item targetItem;
    private MapResource givenItem;
    private string targetText;
    private int takeQuantity;
    private int giveQuantity;

    private Dictionary<Item, int> specificMultiTradeTargets;

    public BoolPluse OnTradeClose;

    private void Awake()
    {
        if (instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    public void OpenTrading(ItemCategory targetCategory,MapResource give, int takeQuantity, int giveQuantity, string tradingText)
    {
        targetText = tradingText;
        this.targetCategory = targetCategory;
        this.takeQuantity = takeQuantity;
        this.giveQuantity = giveQuantity;
        givenItem = give;
        specific = false;
        InternalOpen();
    }

    public void OpenTrading(Item  target, MapResource give, int takeQuantity, int giveQuantity, string tradingText)
    {
        specific = true;
        targetText = tradingText;
        this.targetItem = target;
        this.takeQuantity = takeQuantity;
        this.giveQuantity = giveQuantity;
        givenItem = give;
        InternalOpen();
    }

    public void OpenTradingMulti(Item[] targets, MapResource give, int[] takeQuantities, int giveQuantity, string tradingText)
    {
        specific = true;
        targetText = tradingText;
        this.giveQuantity = giveQuantity;
        givenItem = give;

        specificMultiTradeTargets = new(targets.Length);
        for (int i = 0; i < targets.Length; i++)
        {
            specificMultiTradeTargets.TryAdd(targets[i], takeQuantities[i]);
        }

        InternalOpen();
    }

    private void InternalOpen()
    {
        InputManager.Instance.SetUIToolkitFocus();
        tradingInstanceRoot = SpawnTradingUI();
        buttonContainer.Clear();
        Repaint();
        playerRoot.Add(tradingInstanceRoot);
    }

    public void CloseTrading(bool success)
    {
        playerRoot.Remove(tradingInstanceRoot);
        tradingInstanceRoot = null;
        tradingText = null;
        buttonContainer = null;
        targetText = "";
        takeQuantity = 0;
        OnTradeClose?.Invoke(success);
    }

    private void Repaint()
    {
        if (tradingInstanceRoot == null)
        {
            return;
        }

        Dictionary<Item, int> tradablePlayerItems = new();

        if (!specific)
        {
            tradablePlayerItems = Inventory.Instance.GetAllItemsMatchingCategory(targetCategory);
        }
        else if (Inventory.Instance.TryGetItem(targetItem, out KeyValuePair<Item, int> pair))
        {
            tradablePlayerItems.Add(pair.Key, pair.Value);

        }

        if (tradablePlayerItems == null || tradablePlayerItems.Count == 0)
        {
            TradingText = string.Format("You lack any items to trade, you need {0}(s)", specific ? targetItem.ToString() : targetCategory.ToString());
            AddCancel();
            return;
        }

        TradingText = targetText;
        List<Item> items = new(tradablePlayerItems.Keys);
        items.ForEach(item =>
        {
            Button cur = new()
            {
                text = string.Format("{0} x{1}", item.ToString(), tradablePlayerItems[item])
            };
            cur.AddToClassList("TradeMainbutton");
            buttonContainer.Add(cur);

            if (tradablePlayerItems[item] < takeQuantity)
            {
                cur.text = TextFormatter.ColourText(cur.text, TextFormatter.Red);
                cur.SetEnabled(false);
            }

            cur.RegisterCallback<ClickEvent>(ev => TradeButtonPress(item));
            cur.RegisterCallback<NavigationSubmitEvent>(ev => TradeButtonPress(item));
        });
        AddCancel();
        buttonContainer[0].Focus();
    }

    private void AddCancel()
    {
        buttonContainer.Add(new Button()
        {
            text = "Cancel",
        });
        buttonContainer[buttonContainer.childCount - 1].AddToClassList("TradeMainbutton");
        buttonContainer[buttonContainer.childCount - 1].RegisterCallback<ClickEvent>(ev => CancelTrade());
        buttonContainer[buttonContainer.childCount - 1].RegisterCallback<NavigationSubmitEvent>(ev => CancelTrade());
    }

    private VisualElement SpawnTradingUI()
    {
        VisualElement element = tradingUiPrefab.Instantiate().Q("TradeRoot");
        tradingText = element.Q<Label>("TradeTextOut");
        buttonContainer = element.Q("ButtonContainer");
        return element;
    }

    private void TradeButtonPress(Item item)
    {
        bool withdrawn = (Inventory.Instance.CanTrade(item, takeQuantity)
            && Inventory.Instance.TryRemoveItem(item, takeQuantity));

        if (withdrawn && givenItem != null)
        {
            Inventory.Instance.AddItem(givenItem.ItemStats.type, giveQuantity, Instantiate(givenItem));

            Inventory.Instance.TryMoveItemToHand(givenItem.ItemStats.type);
        }
        if(withdrawn && specificMultiTradeTargets.Count > 0)
        {
            specificMultiTradeTargets.Remove(item);
            Repaint();
            return;
        }

        CloseTrading(withdrawn);
    }

    private void CancelTrade()
    {
        CloseTrading(false);
    }
}
