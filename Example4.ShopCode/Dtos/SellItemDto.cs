// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Example4.ShopCode.EfCoreClasses;
using GenericServices;

namespace Example4.ShopCode.Dtos
{
    public class SellItemDto : ILinkToEntity<ShopSale>
    {
        public List<StockSelectDto> DropdownData { get; set; }

        /// <summary>
        /// This holds the PK of the created ShopSale
        /// </summary>
        public int ShopSaleId { get; set; }

        /// <summary>
        /// This holds the 
        /// </summary>
        public int ShopStockId { get; set; }

        public int NumBought { get; set; } = 1;



        public void SetResetDto(List<StockSelectDto> stockList)
        {
            DropdownData = stockList;
        }
    }
}