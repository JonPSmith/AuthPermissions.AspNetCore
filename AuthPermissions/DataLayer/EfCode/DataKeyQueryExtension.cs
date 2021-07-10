// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using AuthPermissions.CommonCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthPermissions.DataLayer.EfCode
{
    /// <summary>
    /// Contains code to allow you to automate the adding of a multi-tenant query filter to your application's DbContext
    /// See the Example4.ShopCode project with its hierarchical multi-tenant database (RetailDbContext)
    /// This more secure as you can't forget to add a multi-tenant query filter, which would let anyone access that data
    /// </summary>
    public static class DataKeyQueryExtension
    {

        /// <summary>
        /// This method will set up a multi-tenant query filter using the "startswith" query filter
        /// See the Example4.ShopCode project with its hierarchical multi-tenant database (RetailDbContext)
        /// </summary>
        /// <param name="entityData"></param>
        /// <param name="dataKeyFilterProvider"></param>
        public static void AddStartsWithQueryFilter(this IMutableEntityType entityData,
            IDataKeyFilter dataKeyFilterProvider)
        {
            var methodToCall = typeof(DataKeyQueryExtension)
                .GetMethod(nameof(SetupMultiTenantQueryFilter),
                    BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, new object[] { dataKeyFilterProvider });
            entityData.SetQueryFilter((LambdaExpression) filter);
            entityData.AddIndex(entityData.FindProperty(nameof(IDataKeyFilter.DataKey)));
        }

        private static LambdaExpression SetupMultiTenantQueryFilter<TEntity>(IDataKeyFilter dataKeyFilterProvider)
            where TEntity : class, IDataKeyFilter
        {
            Expression<Func<TEntity, bool>> filter = x => x.DataKey.StartsWith(
                dataKeyFilterProvider.DataKey);
            return filter;
        }
    }
}