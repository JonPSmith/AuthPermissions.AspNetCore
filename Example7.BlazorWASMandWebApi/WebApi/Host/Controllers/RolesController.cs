
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.PermissionsCode;
using Example7.BlazorWASMandWebApi.Shared;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSwag.Annotations;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

public class RolesController : VersionNeutralApiController
{
    private readonly IAuthRolesAdminService _authRolesAdmin;

    public RolesController(IAuthRolesAdminService authRolesAdmin)
    {
        _authRolesAdmin = authRolesAdmin;
    }

    [HttpGet]
    [HasPermission(Example7Permissions.RoleRead)]
    [OpenApiOperation("Get a list of all roles.", "")]
    public async Task<List<RoleWithPermissionNamesDto>> GetListAsync()
    {
        string? userId = User.GetUserIdFromUser();
        return await _authRolesAdmin.QueryRoleToPermissions(userId)
                .OrderBy(x => x.RoleType)
                .ToListAsync();
    }

    [HttpGet("permissions")]
    [HasPermission(Example7Permissions.PermissionRead)]
    [OpenApiOperation("Get permissions. This should not be used by a user that has a tenant.", "")]
    public List<PermissionDisplay> ListPermissions()
    {
        return _authRolesAdmin.GetPermissionDisplay(false);
    }

    [HttpPost("permissions")]
    [HasPermission(Example7Permissions.RoleChange)]
    [OpenApiOperation("Update a role's permission names and optionally it's description. This should not be used by a user that has a tenant.", "")]
    public async Task<IActionResult> Edit(RoleCreateUpdateDto input)
    {
        StatusGeneric.IStatusGeneric status = await _authRolesAdmin
            .UpdateRoleToPermissionsAsync(input.RoleName, input.GetSelectedPermissionNames(), input.Description, input.RoleType);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    [HttpPost]
    [HasPermission(Example7Permissions.RoleChange)]
    [OpenApiOperation("Create a role. This should not be used by a user that has a tenant.", "")]
    public async Task<ActionResult> RegisterRoleAsync(RoleCreateUpdateDto input)
    {
        StatusGeneric.IStatusGeneric status = await _authRolesAdmin
                .CreateRoleToPermissionsAsync(input.RoleName, input.GetSelectedPermissionNames(), input.Description, input.RoleType);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    [HttpDelete]
    [HasPermission(Example7Permissions.RoleChange)]
    [OpenApiOperation("Delete a role. This should not be used by a user that has a tenant.", "")]
    public async Task<ActionResult> DeleteAsync(RoleDeleteConfirmDto input)
    {
        StatusGeneric.IStatusGeneric status = await _authRolesAdmin.DeleteRoleAsync(input.RoleName, input.ConfirmDelete?.Trim() == input.RoleName);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }
}