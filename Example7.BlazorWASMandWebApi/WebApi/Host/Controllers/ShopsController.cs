using AuthPermissions.AspNetCore;
using Example7.BlazorWASMandWebApi.Application.ShopSales;
using Example7.BlazorWASMandWebApi.Application.ShopStock;
using Example7.BlazorWASMandWebApi.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

public class ShopsController : VersionedApiController
{
    [HttpPost("stock")]
    [HasPermission(Example7Permissions.StockRead)]
    public Task<List<ShopStockDto>> GetStock(SearchShopStockRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("sales")]
    [HasPermission(Example7Permissions.SalesRead)]
    public Task<List<ShopSaleDto>> GetSales(SearchShopSaleRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("till")]
    [HasPermission(Example7Permissions.SalesSell)]
    public Task Till(CreateSaleItemRequest request)
    {
        return Mediator.Send(request);
    }
}

