
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.SupportCode.DownStatusCode;
using Example7.BlazorWASMandWebApi.Infrastructure.Multitenancy;
using Example7.BlazorWASMandWebApi.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSwag.Annotations;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

public class TenantsController : VersionNeutralApiController
{
    private readonly IAuthTenantAdminService _authTenantAdmin;
    private readonly ISetRemoveStatus _upDownService;

    public TenantsController(IAuthTenantAdminService authTenantAdmin, ISetRemoveStatus upDownService)
    {
        _authTenantAdmin = authTenantAdmin;
        _upDownService = upDownService;
    }

    [HttpGet]
    [HasPermission(Example7Permissions.TenantList)]
    [OpenApiOperation("Get a list of all tenants.", "")]
    public async Task<List<HierarchicalTenantDto>> GetListAsync()
    {
        return await HierarchicalTenantDto.TurnIntoDisplayFormat(_authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantFullName)
                .ToListAsync();
    }

    [HttpGet("{id:int}")]
    [HasPermission(Example7Permissions.TenantList)]
    [OpenApiOperation("Get tenant details.", "")]
    public async Task<HierarchicalTenantDto?> GetAsync(int id)
    {
        var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
        return status.HasErrors
           ? throw new Exception(status.GetAllErrors())
           : HierarchicalTenantDto.TurnIntoDisplayFormat(new List<Tenant> { status.Result }.AsQueryable()).SingleOrDefault();
    }

    [HttpPost]
    [HasPermission(Example7Permissions.TenantCreate)]
    [OpenApiOperation("Create a new tenant.", "")]
    public async Task<ActionResult> CreateAsync(CreateHierarchicalTenantRequest request)
    {
        var status = await _authTenantAdmin.AddHierarchicalTenantAsync(
            request.TenantName,
            request.ParentId);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    [HttpPut]
    [HasPermission(Example7Permissions.TenantUpdate)]
    [OpenApiOperation("Update a tenant.", "")]
    public async Task<ActionResult> UpdateAsync(UpdateHierarchicalTenantRequest request)
    {
        var removeDownAsync = await _upDownService.SetTenantDownWithDelayAsync(TenantDownVersions.Update, request.TenantId);
        var status = await _authTenantAdmin.UpdateTenantNameAsync(request.TenantId, request.TenantName);
        await removeDownAsync();

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    [HttpPost("move-hierarchy-level")]
    [HasPermission(Example7Permissions.TenantMove)]
    [OpenApiOperation("Move hierarchical tenant to another parent.", "")]
    public async Task<ActionResult<string>> MoveHierarchicalTenantToAnotherParentAsync(MoveHierarchicalTenantRequest request)
    {
        //A hierarchical Move requires both the tenant being moved and the tenant receiving the moved tenant 
        var removeDownAsync = await _upDownService.SetTenantDownWithDelayAsync(TenantDownVersions.Update, request.TenantId, request.ParentId);
        var status = await _authTenantAdmin
               .MoveHierarchicalTenantToAnotherParentAsync(request.TenantId, request.ParentId);
        await removeDownAsync();

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    [HttpDelete("{tenantId:int}")]
    [HasPermission(Example7Permissions.TenantDelete)]
    [OpenApiOperation("Delete a tenant.", "")]
    public async Task<ActionResult> DeleteAsync(int tenantId)
    {
        //This will permanently stop logged-in user from accessing the the delete tenant
        var removeDownAsync = await _upDownService.SetTenantDownWithDelayAsync(TenantDownVersions.Deleted, tenantId);
        var status = await _authTenantAdmin.DeleteTenantAsync(tenantId);
        if (status.HasErrors)
            await removeDownAsync();

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }
}