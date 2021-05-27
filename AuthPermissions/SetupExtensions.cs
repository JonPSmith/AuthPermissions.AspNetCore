// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.PermissionsCode.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public static class SetupExtensions
    {
        public static RegisterData RegisterAuthPermissions<TEnumPermissions>(this IServiceCollection externalServices, 
            AuthPermissionsOptions options = null) where TEnumPermissions : Enum
        {
            options ??= new AuthPermissionsOptions();

            //Register internal Services


            //Register external Services
            //This is needed by the policy 
            externalServices.AddSingleton(new EnumTypeService(typeof(TEnumPermissions)));

            return new RegisterData(externalServices, options);
        }

        public static RegisterData UsingEfCoreSqlServer(this RegisterData regData, string connectionString)
        {
            regData.Options.InternalServiceCollection.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlServer(connectionString));

            return regData;
        }

        //Checks

    }
}