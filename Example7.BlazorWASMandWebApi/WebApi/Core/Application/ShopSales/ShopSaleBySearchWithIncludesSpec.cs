// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopSales;

public class ShopSaleBySearchWithIncludesSpec : Specification<ShopSale, ShopSaleDto>
{
    public ShopSaleBySearchWithIncludesSpec(SearchShopSaleRequest request)
    {
        Query.AsNoTracking()
            .Include(x => x.StockItem).ThenInclude(x => x.Shop);
    }
}

