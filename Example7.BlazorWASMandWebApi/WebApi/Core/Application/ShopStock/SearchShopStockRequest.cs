// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Application.ShopStock
{
    public class SearchShopStockRequest : IRequest<List<ShopStockDto>>
    {
    }

    public class SearchShopStockRequestHandler : IRequestHandler<SearchShopStockRequest, List<ShopStockDto>>
    {
        private readonly IReadRepositoryBase<Domain.ShopStock> _repository;

        public SearchShopStockRequestHandler(IReadRepositoryBase<Domain.ShopStock> repository)
        {
            _repository = repository;
        }

        public async Task<List<ShopStockDto>> Handle(SearchShopStockRequest request, CancellationToken cancellationToken)
        {
            ShopStockBySearchWithIncludesSpec spec = new(request);
            return await _repository.ListAsync(spec, cancellationToken);
        }
    }
}

