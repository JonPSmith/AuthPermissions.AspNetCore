// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example4.ShopCode.EfCoreClasses;
using GenericServices;

namespace Example4.ShopCode.Shop
{
    public class ListStockDto : ILinkToEntity<ShopStock>
    {
        public int ShopStockId { get; set; }
        public string StockName { get; set; }
        public decimal RetailPrice { get; set; }
        public int NumInStock { get; set; }

        public string ShopShortName { get; set; }
    }
}