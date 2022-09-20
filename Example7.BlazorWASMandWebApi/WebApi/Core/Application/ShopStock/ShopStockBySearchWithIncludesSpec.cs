// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopStock;

public class ShopStockBySearchWithIncludesSpec : Specification<Domain.ShopStock, ShopStockDto>
{
    public ShopStockBySearchWithIncludesSpec(SearchShopStockRequest request)
    {
        Query.AsNoTracking()
            .Include(x => x.Shop);
    }
}

