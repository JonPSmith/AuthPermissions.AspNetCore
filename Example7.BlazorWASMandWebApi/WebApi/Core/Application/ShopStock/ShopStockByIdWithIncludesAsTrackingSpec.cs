// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopStock
{
    public class ShopStockByIdWithIncludesAsTrackingSpec : Specification<Domain.ShopStock>
    {
        public ShopStockByIdWithIncludesAsTrackingSpec(int Id)
        {
            Query
                .Include(p => p.Shop)
                .Where(p => p.ShopStockId == Id);
        }
    }
}

