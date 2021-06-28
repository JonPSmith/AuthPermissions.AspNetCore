// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace AuthPermissions
{
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
    }
}