// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopSales;

public class SearchShopSaleRequest : IRequest<List<ShopSaleDto>>
{
}

public class SearchShopSaleRequestHandler : IRequestHandler<SearchShopSaleRequest, List<ShopSaleDto>>
{
    private readonly IReadRepositoryBase<ShopSale> _repository;

    public SearchShopSaleRequestHandler(IReadRepositoryBase<ShopSale> repository)
    {
        _repository = repository;
    }

    public async Task<List<ShopSaleDto>> Handle(SearchShopSaleRequest request, CancellationToken cancellationToken)
    {
        ShopSaleBySearchWithIncludesSpec spec = new(request);
        return await _repository.ListAsync(spec, cancellationToken);
    }
}