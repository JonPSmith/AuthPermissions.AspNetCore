// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopSales;

public class ShopSaleDto
{
    public int ShopSaleId { get; set; }

    public string StockItemStockName { get; set; } = default!;

    public decimal StockItemRetailPrice { get; set; }

    /// <summary>
    /// positive number for sale, negative number for return
    /// </summary>
    public int NumSoldReturned { get; set; }

    /// <summary>
    /// Will be null if sale
    /// </summary>
    public string? ReturnReason { get; set; }

    public string StockItemShopShortName { get; set; } = default!;
}

