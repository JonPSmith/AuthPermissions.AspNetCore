// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.AspNetCore;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example4.ShopCode.Dtos;
using Example4.ShopCode.EfCoreClasses;
using GenericServices;
using GenericServices.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class ShopController : Controller
    {
        [HttpGet]
        [HasPermission(Example4Permissions.SalesSell)]
        public IActionResult Till([FromServices] ICrudServices service)
        {
            var dto = new SellItemDto();
            dto.SetResetDto(service.ReadManyNoTracked<StockSelectDto>().ToList());
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example4Permissions.SalesSell)]
        public IActionResult Till([FromServices] ICrudServices service, SellItemDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.SetResetDto(service.ReadManyNoTracked<StockSelectDto>().ToList());
                return View(dto);
            }

            var stockToBuy = service.ReadSingle<ShopStock>(dto.ShopStockId);
            var status = ShopSale.CreateSellAndUpdateStock(dto.NumBought, stockToBuy, null);
            if (status.IsValid)
            {
                service.Context.Add(status.Result);
                service.Context.SaveChanges();
                return RedirectToAction("BuySuccess", new { message = status.Message, status.Result.ShopSaleId });
            }

            //Error state
            service.CopyErrorsToModelState(ModelState, dto);
            dto.SetResetDto(service.ReadManyNoTracked<StockSelectDto>().ToList());
            return View(dto);
        }

        public IActionResult BuySuccess([FromServices] ICrudServices service, string message, int shopSaleId)
        {
            var saleInfo = service.ReadSingle<ListSalesDto>(shopSaleId);
            return View(new Tuple<ListSalesDto, string>(saleInfo, message));
        }

        [HasPermission(Example4Permissions.StockRead)]
        public IActionResult Stock([FromServices] ICrudServices service)
        {
            var allStock = service.ReadManyNoTracked<ListStockDto>().ToList();
            var allTheSameShop = allStock.Any() && allStock.All(x => x.ShopShortName == allStock.First().ShopShortName);
            return View(new Tuple<List<ListStockDto>, bool>(allStock, allTheSameShop));
        }

        [HasPermission(Example4Permissions.SalesRead)]
        public IActionResult Sales([FromServices] ICrudServices service)
        {
            var allSales = service.ReadManyNoTracked<ListSalesDto>().ToList();
            var allTheSameShop = allSales.Any() && allSales.All(x => x.StockItemShopShortName == allSales.First().StockItemShopShortName);
            return View(new Tuple<List<ListSalesDto>, bool>(allSales, allTheSameShop));
        }
    }
}