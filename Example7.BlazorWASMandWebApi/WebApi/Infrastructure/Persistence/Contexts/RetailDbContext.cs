using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using Example7.BlazorWASMandWebApi.Domain;
using Microsoft.EntityFrameworkCore;
using IDataKeyFilterReadOnly = Example7.BlazorWASMandWebApi.Domain.IDataKeyFilterReadOnly;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Persistence.Contexts;

public class RetailDbContext : DbContext, IDataKeyFilterReadOnly
{
    public string DataKey { get; }

    public RetailDbContext(DbContextOptions<RetailDbContext> options, IGetDataKeyFromUser dataKeyFilter)
        : base(options)
    {
        // The DataKey is null when: no one is logged in, its a background service, or user hasn't got an assigned tenant
        // In these cases its best to set the data key that doesn't match any possible DataKey 
        DataKey = dataKeyFilter?.DataKey ?? "stop any user without a DataKey to access the data";
    }

    public DbSet<RetailOutlet> RetailOutlets => Set<RetailOutlet>();
    public DbSet<ShopStock> ShopStocks => Set<ShopStock>();
    public DbSet<ShopSale> ShopSales => Set<ShopSale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // It is currently not possible to define multiple query filters on the same entity - only the last one will be applied.
        // However, you can define a single filter with multiple conditions using the logical AND operator (&& in C#).
        // See https://docs.microsoft.com/en-us/ef/core/querying/filters
        // This way you can chain multiple query filters for the entity.
        modelBuilder
           .AppendGlobalQueryFilter<IDataKeyFilterReadOnly>(s => s.DataKey.StartsWith(DataKey));

        modelBuilder.HasDefaultSchema("retail");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IDataKeyFilterReadOnly).IsAssignableFrom(entityType.ClrType))
            {
                entityType.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetIsUnicode(false); //Make unicode
                entityType.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetMaxLength(AuthDbConstants.TenantDataKeySize);    //and small for single multi-tenant
                entityType.AddIndex(entityType.FindProperty(nameof(IDataKeyFilterReadOnly.DataKey))!);
            }
            else
            {
                throw new Exception(
                    $"You haven't added the {nameof(IDataKeyFilterReadOnly)} to the entity {entityType.ClrType.Name}");
            }

            foreach (var mutableProperty in entityType.GetProperties())
            {
                if (mutableProperty.ClrType == typeof(decimal))
                {
                    mutableProperty.SetPrecision(9);
                    mutableProperty.SetScale(2);
                }
            }
        }
    }
}
