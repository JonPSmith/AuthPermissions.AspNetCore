// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopStock;

public class ShopStockDto
{
    public int ShopStockId { get; set; }
    public string StockName { get; set; } = default!;
    public decimal RetailPrice { get; set; }
    public int NumInStock { get; set; }

    public string ShopShortName { get; set; } = default!;
}

