// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example7.BlazorWASMandWebApi.Application.ShopStock;

namespace Example7.BlazorWASMandWebApi.Application.ShopSales;

public class CreateSaleItemRequest : IRequest<ShopSaleDto>
{
    /// <summary>
    /// This holds the 
    /// </summary>
    public int ShopStockId { get; set; }

    public int NumBought { get; set; }
}

public class CreateSaleItemRequestHandler : IRequestHandler<CreateSaleItemRequest, ShopSaleDto>
{
    private readonly IRepository<ShopSale> _shopSaleRepository;
    private readonly IRepository<Domain.ShopStock> _shopStockRepository;
    private readonly IValidator<CreateSaleItemRequest> _validator;

    public CreateSaleItemRequestHandler(
        IRepository<ShopSale> shopSaleRepository,
        IRepository<Domain.ShopStock> shopStockRepository,
        IValidator<CreateSaleItemRequest> validator)
    {
        _shopSaleRepository = shopSaleRepository;
        _shopStockRepository = shopStockRepository;
        _validator = validator;
    }
    public async Task<ShopSaleDto> Handle(CreateSaleItemRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var foundStock = await _shopStockRepository.FirstOrDefaultAsync(new ShopStockByIdWithIncludesAsTrackingSpec(request.ShopStockId), cancellationToken);

        var shopSale = ShopSale.CreateSellAndUpdateStock(request.NumBought, foundStock!);

        await _shopSaleRepository.AddAsync(shopSale, cancellationToken);

        return shopSale.Adapt<ShopSaleDto>();
    }
}

