// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AdminCode.Services.Internal;

/// <summary>
/// This contains the rules for 
/// </summary>
internal class ChangeRoleTypeChecks
{
    private readonly AuthPermissionsDbContext _context;

    public ChangeRoleTypeChecks(AuthPermissionsDbContext context)
    {
        _context = context;
    }

    public async Task<IStatusGeneric> CheckRoleTypeChangeAsync(RoleTypes originalRoleType, 
        RoleTypes newRoleType, string roleName, IDefaultLocalizer localizeDefault)
    {
        var status = new StatusGenericLocalizer(localizeDefault);
        FormattableString errorPrefix = $"You can't change Role {roleName} from {originalRoleType} to {newRoleType} because ";
        FormattableString errorSuffix;

        switch (originalRoleType, newRoleType)
        {
            case (RoleTypes.Normal, RoleTypes.TenantAutoAdd): //ERROR, impossible
            case (RoleTypes.Normal, RoleTypes.TenantAdminAdd): //ERROR, impossible
            case (RoleTypes.TenantAdminAdd, RoleTypes.TenantAutoAdd): //ERROR, 	OK
            case (RoleTypes.HiddenFromTenant, RoleTypes.TenantAutoAdd): //ERROR, 	impossible
            case (RoleTypes.HiddenFromTenant, RoleTypes.TenantAdminAdd): //ERROR, 	impossible
                errorSuffix = await UserErrorMessageAsync(roleName, false);
                if (errorSuffix != null)
                    status.AddErrorFormatted("UserError".ClassLocalizeKey(this, true),
                        errorPrefix, errorSuffix);
                return status;
            case (RoleTypes.Normal, RoleTypes.HiddenFromTenant): //Error if user has tenant, impossible
            case (RoleTypes.TenantAdminAdd, RoleTypes.HiddenFromTenant): //Error if user has tenant, 	ERROR
                errorSuffix = await UserErrorMessageAsync(roleName, true);
                status.AddErrorFormatted("UserError".ClassLocalizeKey(this, true),
                    errorPrefix, errorSuffix);
                return status;
            case (RoleTypes.TenantAutoAdd, RoleTypes.Normal): //impossible, ERROR
            case (RoleTypes.TenantAutoAdd, RoleTypes.HiddenFromTenant): //impossible, 	ERROR
                errorSuffix = await TenantErrorMessageAsync(roleName);
                status.AddErrorFormatted("TenantError".ClassLocalizeKey(this, true),
                    errorPrefix, errorSuffix);
                return status;
            case (RoleTypes.TenantAutoAdd, RoleTypes.TenantAdminAdd): //impossible, 	OK
            case (RoleTypes.TenantAdminAdd, RoleTypes.Normal): //impossible,	OK
            case (RoleTypes.HiddenFromTenant, RoleTypes.Normal): //OK, 	impossible
                //no error possible
                return status;
            default:
                throw new AuthPermissionsException("missing switch statement");
        }
    }

    private async Task<FormattableString> UserErrorMessageAsync(string roleName, bool filterOutNonTenantUsers)
    {
        var query = _context.AuthUsers.Where(x => x.UserRoles.Any(y => y.RoleName == roleName));
        if (filterOutNonTenantUsers)
            query = query.Where(x => x.TenantId != null);
        
        var numBadUser = await query.CountAsync();
        return numBadUser > 0
            ? $"{numBadUser} users are linked to it."
            : (FormattableString)null;
    }

    private async Task<FormattableString> TenantErrorMessageAsync(string roleName)
    {
        var numBadUser = await _context.RoleToPermissions.Where(x => x.RoleName == roleName)
            //.Select(x => x.Tenants.Select(y => y.TenantFullName))
            .CountAsync() ;
        return numBadUser > 0
            ? $"{numBadUser} tenants are linked to it."
            : (FormattableString)null;
    }

}