using Fungus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TradeOption
{
    public string tradeUIText;
    public string tradeCloseSuccessBlock;
    [Header("Takes")]
    public bool specific;
    public Item specificItem;
    public ItemCategory category;
    public int takeQuantity = 1;
    [Header("Gives")]
    public MapResource givenItem;
    public int giveQuantity = 1;
}

public class NPCTrade : Interact_Example
{
    [Header("Trader")]
    public string tradeCloseReturnBlock;

    [SerializeField] protected string tradeIndexVariableName;
    [SerializeField] protected TradeOption[] tradeOptions;

    protected TradeOption curOption;

    protected override void Start()
    {
        base.Start();
        if(tradeOptions.Length > 0 )
        {
            curOption = tradeOptions[0];
        }
        else
        {
            Debug.LogError("No trade options filled out.", gameObject);
        }
    }

    public void AttemptTrade()
    {
        int index = Dialogue.GetIntegerVariable(tradeIndexVariableName);
        if(tradeOptions.Length > 0)
        {
            curOption = tradeOptions[index];
            AttemptTradeOption(curOption);
        }
        else
        {
            Debug.LogError("No trade options filled out.", gameObject);
        }

    }

    public void AttemptTradeOption(TradeOption option)
    {
        if (TradingUI.Instance)
        {
            TradingUI.Instance.OnTradeClose += TradeClose;
            if(option.specific)
            {
                TradingUI.Instance.OpenTrading(option.specificItem, option.givenItem, option.takeQuantity, option.giveQuantity, option.tradeUIText);
            }
            else
            {
                TradingUI.Instance.OpenTrading(option.category, option.givenItem, option.takeQuantity, option.giveQuantity, option.tradeUIText);
            }
        }
        else
        {
            TradeClose(tradeCloseReturnBlock);
        }
    }

    private void TradeClose(bool newValue)
    {
        TradingUI.Instance.OnTradeClose -= TradeClose;
        if (newValue && curOption != null)
        {
            TradeClose(curOption.tradeCloseSuccessBlock);
        }
        else
        {
            TradeClose(tradeCloseReturnBlock);
        }
        
    }

    private void TradeClose(string exitCommand)
    {
        if (string.IsNullOrEmpty(exitCommand) || string.IsNullOrWhiteSpace(exitCommand))
        {
            return;
        }

        Dialogue.ExecuteBlock(exitCommand);
    }
}

public class StringVarCollection : GenericCollection<StringVar>
{

}