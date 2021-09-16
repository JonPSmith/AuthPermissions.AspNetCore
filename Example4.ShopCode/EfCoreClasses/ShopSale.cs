// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using AuthPermissions.CommonCode;
using StatusGeneric;

namespace Example4.ShopCode.EfCoreClasses
{
    public class ShopSale : IDataKeyFilterReadOnly
    {
        private ShopSale() { } //needed by EF Core

        private ShopSale(int numSoldReturned, string returnReason, ShopStock foundStock)
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
        public string ReturnReason { get; private set; }

        /// <summary>
        /// This contains the datakey from the RetailOutlet
        /// </summary>
        public string DataKey { get; private set; }

        //------------------------------------------
        //relationships

        public int ShopStockId { get; private set; }

        [ForeignKey(nameof(ShopStockId))]
        public ShopStock StockItem { get; private set; }

        //----------------------------------------------
        //create methods

        /// <summary>
        /// This creates a Sale entry, and also update the ShopStock number in stock
        /// </summary>
        /// <param name="numBought"></param>
        /// <param name="foundStock"></param>
        /// <param name="stockName">Used only for error message</param>
        /// <returns></returns>
        public static IStatusGeneric<ShopSale> CreateSellAndUpdateStock(int numBought, ShopStock foundStock, string stockName)
        {
            if (numBought < 0) throw new ArgumentException("must be positive", nameof(numBought));
            var status = new StatusGenericHandler<ShopSale> {Message = $"Successfully bought a {(foundStock?.StockName ?? stockName)}"};

            if (foundStock == null)
                return status.AddError($"Could not find any stock of: {stockName}.");

            status.CombineStatuses(foundStock.SellSomeStock(numBought));
            if (status.HasErrors)
                return status;

            var sale = new ShopSale(numBought, null, foundStock);
            return status.SetResult(sale);
        }
    }
}