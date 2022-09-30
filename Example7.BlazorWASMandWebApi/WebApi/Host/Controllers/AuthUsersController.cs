
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.SupportCode.AddUsersServices;
using Example7.BlazorWASMandWebApi.Infrastructure.Identity;
using Example7.BlazorWASMandWebApi.Shared;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSwag.Annotations;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

public class AuthUsersController : VersionNeutralApiController
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuthUsersAdminService _authUsersAdmin;

    public AuthUsersController(
        UserManager<IdentityUser> userManager,
        IAuthUsersAdminService authUsersAdmin)
    {
        _userManager = userManager;
        _authUsersAdmin = authUsersAdmin;
    }

    [HttpGet]
    // [HasPermission(Example7Permissions.UserRead)]
    [OpenApiOperation("List users filtered by authUser tenant.", "")]
    public async Task<List<AuthUserDisplay>> ListAuthUsersFilteredByTenantAsync()
    {
        string? authDataKey = User.GetAuthDataKeyFromUser();
        IQueryable<AuthUser> userQuery = _authUsersAdmin.QueryAuthUsers(authDataKey);
        return await AuthUserDisplay.TurnIntoDisplayFormat(userQuery.OrderBy(x => x.Email)).ToListAsync();
    }

    [HttpGet("profile")]
    [HasPermission(Example7Permissions.UserRead)]
    public async Task<ActionResult<AuthUserDisplay>> GetCurrentAuthUserInfo()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            string? userId = User.GetUserIdFromUser();
            StatusGeneric.IStatusGeneric<AuthUser> status = await _authUsersAdmin.FindAuthUserByUserIdAsync(userId);

            return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(AuthUserDisplay.DisplayUserInfo(status.Result));
        }

        return Unauthorized();
    }

    [HttpPost]
    [AllowAnonymous]
    [OpenApiOperation("Creates a new user and tenant with roles.", "")]
    public async Task<ActionResult> CreateUserAndTenantAsync([FromServices] ISignInAndCreateTenant userRegisterInvite, CreateUserRequest request)
    {
        var newUserData = new AddNewUserDto
        {
            Email = request.Email,
            UserName = request.UserName,
            Password = request.Password,
            IsPersistent = false
        };
        var newTenantData = new AddNewTenantDto
        {
            TenantName = request.TenantName,
            Version = request.Version
        };
        var status = await userRegisterInvite.SignUpNewTenantAsync(newUserData, newTenantData);
        if (status.HasErrors)
            throw new Exception(status.GetAllErrors());

        return Ok(status.Message);
    }

    [HttpGet("view-sync-changes")]
    [HasPermission(Example7Permissions.UserSync)]
    public async Task<ActionResult<List<SyncAuthUserWithChange>>> SyncUsers()
    {
        return await _authUsersAdmin.SyncAndShowChangesAsync();
    }

    [HttpPost("apply-sync-changes")]
    [HasPermission(Example7Permissions.UserSync)]
    public async Task<ActionResult> SyncUsers(IEnumerable<SyncAuthUserWithChange> data)
    {
        var status = await _authUsersAdmin.ApplySyncChangesAsync(data);
        if (status.HasErrors)
            throw new Exception(status.GetAllErrors());

        return Ok(status.Message);
    }

    [HttpPut]
    [HasPermission(Example7Permissions.UserChange)]
    [OpenApiOperation("Update an authUser.", "")]
    public async Task<ActionResult> UpdateAsync(SetupManualUserChange change)
    {
        StatusGeneric.IStatusGeneric status = await _authUsersAdmin.UpdateUserAsync(
            change.UserId, change.Email, change.UserName, change.RoleNames, change.TenantName);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    // todo Change the input type to represent only required changes
    [HttpPut("roles")]
    [HasPermission(Example7Permissions.UserRolesChange)]
    [OpenApiOperation("Update an authUser's roles.", "")]
    public async Task<ActionResult> UpdateRolesAsync(SetupManualUserChange change)
    {
        StatusGeneric.IStatusGeneric status = await _authUsersAdmin.UpdateUserAsync(
            change.UserId, roleNames: change.RoleNames);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }

    [HttpDelete("{id}")]
    [HasPermission(Example7Permissions.UserRemove)]
    [OpenApiOperation("Delete an authUser.", "")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        var status = await _authUsersAdmin.DeleteUserAsync(id);

        return status.HasErrors
            ? throw new Exception(status.GetAllErrors())
            : Ok(status.Message);
    }
}

