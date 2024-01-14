using Fungus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTrade : Interact_Example
{
    [Header("Trader")]
    public string tradeUIText;
    public string tradeCloseReturnBlock;
    public string tradeCloseSuccessBlock;
    [Header("Takes")]
    public bool specific;
    public Item specificItem;
    public ItemCategory category;
    public int takeQuantity = 1;
    [Header("Gives")]
    public MapResource givenItem;
    public int giveQuantity = 1;
    

    private void Awake()
    {
    }

    public void AttemptTrade()
    {
        if (TradingUI.Instance)
        {
            TradingUI.Instance.OnTradeClose += TradeClose;
            if(specific)
            {
                TradingUI.Instance.OpenTrading(specificItem, givenItem, takeQuantity, giveQuantity, tradeUIText);
            }
            else
            {
                TradingUI.Instance.OpenTrading(category, givenItem, takeQuantity, giveQuantity, tradeUIText);
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
        if (newValue)
        {
            TradeClose(tradeCloseSuccessBlock);
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