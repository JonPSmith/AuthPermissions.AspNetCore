﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Domain;

/// <summary>
/// This contains an item stocked in the shop, and how many they have
/// </summary>
public class ShopStock : IAggregateRoot, IDataKeyFilterReadOnly
{
    private ShopStock() { } //needed by EF Core

    public ShopStock(string stockName, decimal retailPrice, int numInStock, RetailOutlet shop)
    {
        StockName = stockName;
        RetailPrice = retailPrice;
        NumInStock = numInStock;
        Shop = shop;
        DataKey = shop.DataKey;
    }

    public int ShopStockId { get; private set; }
    public string StockName { get; private set; } = default!;
    public decimal RetailPrice { get; private set; }
    public int NumInStock { get; private set; }
    /// <summary>
    /// This contains the datakey from the RetailOutlet
    /// </summary>
    public string DataKey { get; private set; } = default!;

    //------------------------------------------
    //relationships

    public int TenantItemId { get; private set; }

    [ForeignKey(nameof(TenantItemId))]
    public RetailOutlet Shop { get; private set; } = default!;

    public override string ToString()
    {
        return $"{nameof(StockName)}: {StockName}, {nameof(RetailPrice)}: {RetailPrice}, {nameof(NumInStock)}: {NumInStock}";
    }

    //------------------------------------------
    //access methods

    public IStatusGeneric SellSomeStock(int numBought)
    {
        var status = new StatusGenericHandler();
        NumInStock -= numBought;
        if (NumInStock < 0)
        {
            status.AddError("There are not enough items of that product to sell.");
        }

        return status;
    }
}