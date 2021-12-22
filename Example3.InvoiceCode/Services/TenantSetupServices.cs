// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using Example3.InvoiceCode.Dtos;
using Microsoft.AspNetCore.Identity;
using StatusGeneric;

namespace Example3.InvoiceCode.Services;

public interface ITenantSetupServices
{
    /// <summary>
    /// This does three things (with lots of checks)
    /// - Adds the new user to the the individual account
    /// - Adds an AuthUser for this person
    /// - Creates the tenant with the correct tenant roles
    /// </summary>
    /// <param name="dto">The information from the user</param>
    /// <returns>Status</returns>
    Task<IStatusGeneric> CreateNewTenantAsync(CreateTenantDto dto);
}

public class TenantSetupServices : ITenantSetupServices
{
    private readonly IAuthTenantAdminService _tenantAdminService;
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly UserManager<IdentityUser> _userManager;

    public TenantSetupServices(IAuthTenantAdminService tenantAdminService, IAuthUsersAdminService authUsersAdmin, UserManager<IdentityUser> userManager)
    {
        _tenantAdminService = tenantAdminService;
        _authUsersAdmin = authUsersAdmin;
        _userManager = userManager;
    }

    /// <summary>
    /// This does three things (with lots of checks)
    /// - Adds the new user to the the individual account
    /// - Adds an AuthUser for this person
    /// - Creates the tenant with the correct tenant roles
    /// </summary>
    /// <param name="dto">The information from the user</param>
    /// <returns>Status</returns>
    public async Task<IStatusGeneric> CreateNewTenantAsync(CreateTenantDto dto)
    {
        var status = new StatusGenericHandler
        {
            Message =
                $"Successfully created the tenant called '{dto.TenantName}' and registered you as the tenant admin"
        };

        var tenantVersion = dto.GetTenantVersionType();

        if (tenantVersion == TenantVersionTypes.NotSet)
            throw new AuthPermissionsException("The Version string in the dto wasn't set properly");

        //Check if tenant name is available
        if (_tenantAdminService.QueryTenants().Any(x => x.TenantFullName == dto.TenantName))
            return status.AddError($"The tenant name '{dto.TenantName}' is already taken", new []{nameof(CreateTenantDto.TenantName) });

        //Add the new user if not already there
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                result.Errors.Select(x => x.Description).ToList().ForEach(error => status.AddError(error));
            }
        }
        else
        {
            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new AuthPermissionsException("The user was known, but the password was wrong");
        }
        
        //Check if user is already in the AuthUsers (because a AuthUser can only be linked to one tenant)
        if ((await _authUsersAdmin.FindAuthUserByEmailAsync(dto.Email)).Result != null)
            return status.AddError("You are already registered as a user.");

        //Now we can create the tenant
        //TODO: add version Roles
        var tenantStatus = await _tenantAdminService.AddSingleTenantAsync(dto.TenantName);
        if (tenantStatus.HasErrors)
            return tenantStatus;

        //TODO: add tenant admin and other roles to this user
        return await _authUsersAdmin.AddNewUserAsync(user.Id, dto.Email, null, new List<string>(), dto.TenantName);

    }
}