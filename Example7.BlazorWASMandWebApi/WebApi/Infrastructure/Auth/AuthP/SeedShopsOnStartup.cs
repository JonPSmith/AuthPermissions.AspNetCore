using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using Example7.BlazorWASMandWebApi.Domain;
using Example7.BlazorWASMandWebApi.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Auth.AuthP;

public class SeedShopsOnStartup
{
    private readonly RetailDbContext _context;
    private readonly IAuthTenantAdminService _authTenantAdmin;

    public const string SeedStockText = @"Dress4U: Flower dress|50, Tiny dress|22
Tie4U: Blue tie|15, Red tie|20, Green tie|10
Shirt4U: White shirt|40, Blue shirt|30
NY Dress4U: Modern dress|65, Nice dress|30 
Boston Shirt4U: White shirt|40, Blue shirt|30
Cats Place: Cat food (large)|40, Cat food (small)|10  
Kitten Place: Scratch pole|60, Play mouse|5, Cat food (small)|12";

    public SeedShopsOnStartup(RetailDbContext context, IAuthTenantAdminService authTenantAdmin)
    {
        _context = context;
        _authTenantAdmin = authTenantAdmin;
    }

    /// <summary>
    /// This does the following
    /// 1) finds all the end leaf tenants and creates a <see cref="Example4.ShopCode.EfCoreClasses.RetailOutlet"/> using that tenant
    /// 2) It then adds some stock to each retail outlet
    /// </summary>
    /// <returns></returns>
    public async Task CreateShopsAndSeedStockAsync(string seedStockText)
    {
        var tenantsThatAreShops = await _authTenantAdmin.QueryEndLeafTenants().ToListAsync();

        var retailLookup = tenantsThatAreShops.Select(x =>
                new RetailOutlet(x.TenantId, x.TenantFullName, x.GetTenantDataKey()))
            .ToDictionary(x => x.ShortName);

        _context.AddRange(retailLookup.Values);

        AddStockToShops(retailLookup, seedStockText);

        await _context.SaveChangesAsync();
    }

    public void AddStockToShops(Dictionary<string, RetailOutlet> retailLookup, string seedStockText)
    {
        //find the correct line delimiter char to cover different counties 
        var splitChar = seedStockText.Contains('\n') ? '\n' : Environment.NewLine.First();

        foreach (var line in seedStockText.Split(splitChar))
        {
            var colonIndex = line.IndexOf(':');
            var shopName = line.Substring(0, colonIndex);
            if (!retailLookup.TryGetValue(shopName, out var shop))
                throw new AuthPermissionsException($"Could not find a shop of name '{shopName}'");

            var eachStock = from stockAndPrice in line.Substring(colonIndex + 1).Split(',')
                            let parts = stockAndPrice.Split('|').Select(x => x.Trim()).ToArray()
                            select new { Name = parts[0], Price = decimal.Parse(parts[1]) };
            foreach (var stock in eachStock)
            {
                var newStock = new ShopStock(stock.Name, stock.Price, 5, shop);
                _context.Add(newStock);
            }
        }
    }
}