// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example4.ShopCode.EfCoreClasses;
using GenericServices;

namespace Example4.ShopCode.Shop
{
    public class ListSalesDto : ILinkToEntity<ShopSale>
    {
        public int ShopSaleId { get; set; }

        public string StockItemStockName { get; set; }

        public decimal StockItemRetailPrice { get; set; }

        /// <summary>
        /// positive number for sale, negative number for return
        /// </summary>
        public int NumSoldReturned { get; set; }

        /// <summary>
        /// Will be null if sale
        /// </summary>
        public string ReturnReason { get; set; }

        public string StockItemShopShortName { get; set; }
    }
}