// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions;
using AuthPermissions.AdminCode;

namespace Example6.SingleLevelSharding.Services;

/// <summary>
/// This adds the tenant name as a claim. This speeds up the showing of the tenant name in the display
/// </summary>
public class AddTenantNameClaim : IClaimsAdder
{
    public const string TenantNameClaimType = "TenantName";

    private readonly IAuthUsersAdminService _userAdmin;

    public AddTenantNameClaim(IAuthUsersAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public async Task<Claim?> AddClaimToUserAsync(string userId)
    {
        var user = (await _userAdmin.FindAuthUserByUserIdAsync(userId)).Result;

        return user?.UserTenant?.TenantFullName == null
            ? null
            : new Claim(TenantNameClaimType, user.UserTenant.TenantFullName);
    }

    public static string GetTenantNameFromUser(ClaimsPrincipal user)
    {
        return user?.Claims.FirstOrDefault(x => x.Type == TenantNameClaimType)?.Value;
    }
}