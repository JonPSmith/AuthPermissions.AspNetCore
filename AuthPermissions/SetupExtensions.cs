// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public static class SetupExtensions
    {
        public static RegisterData RegisterAuthPermissions<TEnumPermissions>(this IServiceCollection services, 
            AuthPermissionsOptions options = null) where TEnumPermissions : Enum
        {
            options ??= new AuthPermissionsOptions();

            //Register internal Services


            //Register external Services
            //This is needed by the policy 
            services.AddSingleton(new EnumTypeService(typeof(TEnumPermissions)));

            return new RegisterData(services, options);
        }

        public static RegisterData UsingEfCoreSqlServer(this RegisterData regData, string connectionString)
        {
            regData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlServer(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(PermissionConstants.MigrationsHistoryTableName)));

            return regData;
        }

        public static RegisterData UsingInMemoryDatabaseForTesting(this RegisterData regData)
        {
            return regData;
        }

        //NOTE: Only works with in-memory database
        public static RegisterData AddTestUsersRolesEtc(this RegisterData regData)
        {
            return regData;
        }



    }
}