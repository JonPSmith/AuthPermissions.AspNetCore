// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Domain;

public class ShopSale : IAggregateRoot, IDataKeyFilterReadOnly
{
    private ShopSale() { } //needed by EF Core

    private ShopSale(int numSoldReturned, string? returnReason, ShopStock foundStock)
    {
        if (numSoldReturned == 0) throw new ArgumentException("cannot be zero", nameof(numSoldReturned));
        if (numSoldReturned < 0 && returnReason == null)
            throw new ArgumentException("cannot be null if its a return", nameof(returnReason));

        NumSoldReturned = numSoldReturned;
        ReturnReason = returnReason;
        StockItem = foundStock;
        DataKey = foundStock.DataKey;
    }

    public int ShopSaleId { get; private set; }

    /// <summary>
    /// positive number for sale, negative number for return
    /// </summary>
    public int NumSoldReturned { get; private set; }

    /// <summary>
    /// Will be null if sale
    /// </summary>
    public string? ReturnReason { get; private set; }

    /// <summary>
    /// This contains the datakey from the RetailOutlet
    /// </summary>
    public string DataKey { get; private set; } = default!;

    //------------------------------------------
    //relationships

    public int ShopStockId { get; private set; }

    [ForeignKey(nameof(ShopStockId))]
    public ShopStock StockItem { get; private set; } = default!;

    //----------------------------------------------
    //create methods

    /// <summary>
    /// This creates a Sale entry, and also update the ShopStock number in stock
    /// </summary>
    /// <param name="numBought"></param>
    /// <param name="foundStock"></param>
    /// <param name="stockName">Used only for error message</param>
    /// <returns></returns>
    public static ShopSale CreateSellAndUpdateStock(int numBought, ShopStock foundStock)
    {
        foundStock.SellSomeStock(numBought);

        return new ShopSale(numBought, null, foundStock);
    }
}