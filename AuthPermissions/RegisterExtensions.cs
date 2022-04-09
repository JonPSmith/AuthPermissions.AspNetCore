// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    /// <summary>
    /// This contains extension methods to register your code to be used with AuthP's code
    /// </summary>
    public static class RegisterExtensions
    {
        /// <summary>
        /// Use this to provide the <see cref="IFindUserInfoService"/> service which AuthP uses to synchronize its user database
        /// against the users in the application's Authentication Provider. Used in the <see cref="AuthUsersAdminService"/> sync code.
        /// </summary>
        /// <typeparam name="TUserLookup"></typeparam>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData RegisterFindUserInfoService<TUserLookup>(this AuthSetupData setupData)
            where TUserLookup : class, IFindUserInfoService
        {
            setupData.Services.AddTransient<IFindUserInfoService, TUserLookup>();

            return setupData;
        }

        /// <summary>
        /// Use this to provide the <see cref="ISyncAuthenticationUsers"/> service which AuthP uses to synchronize its user database
        /// against the users in the application's Authentication Provider. Used in the <see cref="AuthUsersAdminService"/> sync code.
        /// </summary>
        /// <typeparam name="TSyncProviderReader"></typeparam>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData RegisterAuthenticationProviderReader<TSyncProviderReader>(
            this AuthSetupData setupData)
            where TSyncProviderReader : class, ISyncAuthenticationUsers
        {
            setupData.Services.AddTransient<ISyncAuthenticationUsers, TSyncProviderReader>();

            return setupData;
        }

        /// <summary>
        /// Use this to provide the <see cref="ITenantChangeService"/> service which AuthP uses to alter your tenant data when you call the
        /// AuthP tenant's methods to Update, Move, or Delete a tenant
        /// This is used to check that the AuthP entities and your application entities are in the same database.
        /// </summary>
        /// <typeparam name="TTenantChangeService"></typeparam>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData RegisterTenantChangeService<TTenantChangeService>(
            this AuthSetupData setupData)
            where TTenantChangeService : class, ITenantChangeService
        {
            setupData.Services.AddTransient<ITenantChangeService, TTenantChangeService>();

            return setupData;
        }
    }
}