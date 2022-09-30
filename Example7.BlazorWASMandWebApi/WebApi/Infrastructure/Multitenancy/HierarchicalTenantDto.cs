
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.EntityFrameworkCore;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Multitenancy;

public class HierarchicalTenantDto
{
    public int TenantId { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(AuthDbConstants.TenantFullNameSize)]
    public string TenantFullName { get; set; } = default!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(AuthDbConstants.TenantFullNameSize)]
    public string TenantName { get; set; } = default!;

    public string DataKey { get; set; } = default!;

    //-------------------------------------------
    //used for Create and Move

    public List<KeyValuePair<int, string>> ListOfTenants { get; private set; } = default!;

    public int ParentId { get; set; }

    public static IQueryable<HierarchicalTenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
    {
        return inQuery.Select(x => new HierarchicalTenantDto
        {
            TenantId = x.TenantId,
            TenantFullName = x.TenantFullName,
            TenantName = x.GetTenantName(),
            DataKey = x.GetTenantDataKey()
        });
    }

    public static async Task<HierarchicalTenantDto> SetupForCreateAsync(IAuthTenantAdminService tenantAdminService)
    {
        var result = new HierarchicalTenantDto
        {
            ListOfTenants = await tenantAdminService.QueryTenants()
                .Select(x => new KeyValuePair<int, string>(x.TenantId, x.TenantFullName))
                .ToListAsync()
        };
        result.ListOfTenants.Insert(0, new KeyValuePair<int, string>(0, "< none >"));

        return result;
    }

    public static async Task<HierarchicalTenantDto> SetupForMoveAsync(Tenant tenant, IAuthTenantAdminService tenantAdminService)
    {
        var result = new HierarchicalTenantDto
        {
            TenantId = tenant.TenantId,
            TenantFullName = tenant.TenantFullName,
            TenantName = tenant.GetTenantName(),

            ListOfTenants = await tenantAdminService.QueryTenants()
                .Select(x => new KeyValuePair<int, string>(x.TenantId, x.TenantFullName))
                .ToListAsync(),
            ParentId = tenant.ParentTenantId ?? 0
        };
        result.ListOfTenants.Insert(0, new KeyValuePair<int, string>(0, "< none >"));

        return result;
    }

    public static HierarchicalTenantDto SetupForEdit(Tenant tenant)
    {
        return new HierarchicalTenantDto
        {
            TenantId = tenant.TenantId,
            TenantFullName = tenant.TenantFullName,
            TenantName = tenant.GetTenantName()
        };
    }

    public static async Task<HierarchicalTenantDto> SetupForDeleteAsync(Tenant tenant, IAuthTenantAdminService tenantAdminService)
    {
        return new HierarchicalTenantDto
        {
            TenantId = tenant.TenantId,
            TenantFullName = tenant.TenantFullName,
            TenantName = tenant.GetTenantName(),
            DataKey = tenant.GetTenantDataKey(),

            ListOfTenants = (await tenantAdminService.GetHierarchicalTenantChildrenViaIdAsync(tenant.TenantId))
                .Select(x => new KeyValuePair<int, string>(x.TenantId, x.TenantFullName))
                .ToList()
        };
    }
}

