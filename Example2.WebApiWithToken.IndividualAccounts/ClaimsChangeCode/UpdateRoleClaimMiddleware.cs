// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;
using System.Security.Claims;
using AuthPermissions.BaseCode.PermissionsCode;

namespace Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;

public static class UpdateRoleClaimMiddleware
{
    public static string FormReplacementPermissionsKey(this string userId) => $"ReplacementPermissions{userId}";

    /// <summary>
    /// This method registers the <see cref="ReplacePermissionsMiddleware"/> middleware code
    /// and should be placed after the UseAuthorization. This middleware will replace user's Permissions claim
    /// if there is a newer Permissions claim in the FileStore cache.
    /// (entries to the FileStore cache are triggered by database events that change a user's permissions)  
    /// </summary>
    /// <param name="app"></param>
    public static void UsePermissionsChange(this IApplicationBuilder app)
    {
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            var replacementUser = await ReplacePermissionsMiddleware(context.RequestServices, context.User);
            if (replacementUser != null)
                context.User = replacementUser;

            await next();
        });
    }

    /// <summary>
    /// This will replace the user's Permissions claim if there is a newer
    /// Permissions claim value in the FileStore cache for the current user.
    /// </summary>
    /// <param name="serviceProvider">Allows the middleware to </param>
    /// <param name="user">The current user. Can be null if </param>
    /// <returns>null means not change to the current user, otherwise it returns a new user to be used.</returns>
    public static async Task<ClaimsPrincipal> ReplacePermissionsMiddleware(IServiceProvider serviceProvider, ClaimsPrincipal user)
    {
        var userId = user.GetUserIdFromUser();
        if (userId != null)
        {
            //There is a logged-in user, so we see if the FileStore cache contains a new Permissions claim
            var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();

            var replacementPermissions = await fsCache.GetAsync(userId.FormReplacementPermissionsKey());
            if (replacementPermissions != null)
            {
                //Yes, we have a replacement permissions claim so update the User's claims

                var updateClaims = user.Claims.ToList();
                var found = updateClaims.FirstOrDefault(c =>
                    c.Type == PermissionConstants.PackedPermissionClaimType);
                updateClaims.Remove(found); //If claim wasn't found we still add the updated permissions claim
                updateClaims.Add(new Claim(PermissionConstants.PackedPermissionClaimType, replacementPermissions));

                var appIdentity = new ClaimsIdentity(updateClaims, user.Identity!.AuthenticationType);
                return new ClaimsPrincipal(appIdentity);
            }
        }
        
        return null; //no change to the current user
    }
}