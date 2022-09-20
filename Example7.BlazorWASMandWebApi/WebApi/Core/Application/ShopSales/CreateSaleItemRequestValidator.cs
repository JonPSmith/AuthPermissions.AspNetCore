// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example7.BlazorWASMandWebApi.Application.ShopStock;

namespace Example7.BlazorWASMandWebApi.Application.ShopSales
{
    public class CreateSaleItemRequestValidator : AbstractValidator<CreateSaleItemRequest>
    {
        public CreateSaleItemRequestValidator(IReadRepositoryBase<Domain.ShopStock> shopStockRepository)
        {
            RuleFor(p => p.ShopStockId)
               .NotEmpty()
               .MustAsync(async (id, ct) =>
                            await shopStockRepository.FirstOrDefaultAsync(new ShopStockByIdWithIncludesAsTrackingSpec(id), ct) is not null)
                    .WithMessage((_, id) => $"{nameof(Domain.ShopStock)} Id: {id} Not Found.");

            RuleFor(p => p.NumBought)
                .GreaterThan(0);

        }
    }
}

