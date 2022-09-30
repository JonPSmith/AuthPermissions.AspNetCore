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
using AuthPermissions.BaseCode.DataLayer.EfCode;

namespace Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;

public static class AddEmailClaimMiddleware
{
    public static string FormAddedEmailClaimKey(this string userId) => $"AddEmailClaim-{userId}";

    public static void UseAddEmailClaimToUsers(this IApplicationBuilder app)
    {
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            var replacementUser = await AddEmailClaimToCurrentUser(context.RequestServices, context.User);
            if (replacementUser != null)
                context.User = replacementUser;

            await next();
        });
    }

    public static async Task<ClaimsPrincipal> AddEmailClaimToCurrentUser(IServiceProvider serviceProvider, ClaimsPrincipal user)
    {
        var userId = user.GetUserIdFromUser();
        if (userId != null)
        {
            //There is a logged-in user, so we see if the FileStore cache contains a new Permissions claim
            var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();

            var usersEmail = await fsCache.GetAsync(userId.FormAddedEmailClaimKey());
            if (usersEmail == null)
            {
                //Not set up yet, so we need to get the user's email and place it in the cache
                var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
                usersEmail = context.AuthUsers.Where(x => x.UserId == userId).Select(x => x.Email).FirstOrDefault();
                
                if (usersEmail == null)
                    return null; //shouldn't happen, but could in certain updates

                await fsCache.SetAsync(userId.FormAddedEmailClaimKey(), usersEmail);
            }

            //We need to add the Email from the cache
            var updateClaims = user.Claims.ToList();
            updateClaims.Add(new Claim(ClaimTypes.Email, usersEmail));

            var appIdentity = new ClaimsIdentity(updateClaims, user.Identity!.AuthenticationType);
            return new ClaimsPrincipal(appIdentity);
        }
        
        return null; //no change to the current user
    }
}