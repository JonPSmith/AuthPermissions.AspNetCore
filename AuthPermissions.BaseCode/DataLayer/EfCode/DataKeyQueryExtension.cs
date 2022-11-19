// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq.Expressions;
using System.Reflection;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthPermissions.BaseCode.DataLayer.EfCode
{
    /// <summary>
    /// Contains code to allow you to automate the adding of a multi-tenant query filter to your application's DbContext
    /// See Example3.InvoiceCode and Example4.ShopCode projects with the two types of multi-tenant: single and hierarchical
    /// This more secure as you can't forget to add a multi-tenant query filter, which would let anyone access that data
    /// </summary>
    public static class DataKeyQueryExtension
    {
        /// <summary>
        /// This method will set up a single level tenant query filter exact match query filter
        /// See the Example3.InvoiceCode project with its single level multi-tenant database (InvoiceDbContext)
        /// </summary>
        /// <param name="entityData"></param>
        /// <param name="dataKey"></param>
        public static void AddSingleTenantReadWriteQueryFilter(this IMutableEntityType entityData,
            IDataKeyFilterReadOnly dataKey)
        {
            var methodToCall = typeof(DataKeyQueryExtension)
                .GetMethod(nameof(SetupSingleTenantQueryFilter),
                    BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, new object[] { dataKey });
            entityData.SetQueryFilter((LambdaExpression)filter);
            entityData.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetIsUnicode(false); //Make unicode
            entityData.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetMaxLength(AuthDbConstants.TenantDataKeySize);
            entityData.AddIndex(entityData.FindProperty(nameof(IDataKeyFilterReadWrite.DataKey)));
        }

        private static LambdaExpression SetupSingleTenantQueryFilter<TEntity>(IDataKeyFilterReadOnly dataKey)
            where TEntity : class, IDataKeyFilterReadWrite
        {
            Expression<Func<TEntity, bool>> filter = x => x.DataKey == dataKey.DataKey;
            return filter;
        }

        /// <summary>
        /// This method will set up a multi-tenant query filter using the "startswith" query filter
        /// See the Example4.ShopCode project with its hierarchical multi-tenant database (RetailDbContext)
        /// </summary>
        /// <param name="entityData"></param>
        /// <param name="dataKey"></param>
        public static void AddHierarchicalTenantReadOnlyQueryFilter(this IMutableEntityType entityData,
            IDataKeyFilterReadOnly dataKey)
        {
            var methodToCall = typeof(DataKeyQueryExtension)
                .GetMethod(nameof(SetupMultiTenantQueryFilter),
                    BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, new object[] { dataKey });
            entityData.SetQueryFilter((LambdaExpression) filter);
            entityData.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetIsUnicode(false); //Make unicode
            entityData.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetMaxLength(AuthDbConstants.TenantDataKeySize);
            entityData.AddIndex(entityData.FindProperty(nameof(IDataKeyFilterReadOnly.DataKey)));
        }

        private static LambdaExpression SetupMultiTenantQueryFilter<TEntity>(IDataKeyFilterReadOnly dataKey)
            where TEntity : class, IDataKeyFilterReadOnly
        {
            Expression<Func<TEntity, bool>> filter = x => x.DataKey.StartsWith(dataKey.DataKey);
            return filter;
        }

        /// <summary>
        /// This method will set up a single-level tenant query filter when using sharding
        /// The difference from the non-sharding version is if the tenant is in a database all on its own,
        /// then it sets a true, which will remove the effect of query filter to improve performance
        /// See the Example6 for an example of using this
        /// </summary>
        /// <param name="entityData"></param>
        /// <param name="dataKey"></param>
        public static void AddSingleTenantShardingQueryFilter(this IMutableEntityType entityData,
            IDataKeyFilterReadOnly dataKey)
        {
            var methodToCall = typeof(DataKeyQueryExtension)
                .GetMethod(nameof(SetupSingleTenantShardingQueryFilter),
                    BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, new object[] { dataKey });
            entityData.SetQueryFilter((LambdaExpression)filter);
            entityData.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetIsUnicode(false); //Make unicode
            entityData.GetProperty(nameof(IDataKeyFilterReadWrite.DataKey)).SetMaxLength(AuthDbConstants.TenantDataKeySize);
            entityData.AddIndex(entityData.FindProperty(nameof(IDataKeyFilterReadWrite.DataKey)));
        }

        /// <summary>
        /// This version will set a true if the DataKey == <see cref="MultiTenantExtensions.DataKeyNoQueryFilter"/>
        /// This removes the effect of the query filter in single-level tenant using sharding and is the only tenant in a database
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        private static LambdaExpression SetupSingleTenantShardingQueryFilter<TEntity>(IDataKeyFilterReadOnly dataKey)
            where TEntity : class, IDataKeyFilterReadWrite
        {
            Expression<Func<TEntity, bool>> filter = x =>
                dataKey.DataKey == MultiTenantExtensions.DataKeyNoQueryFilter ||
                x.DataKey == dataKey.DataKey;
            return filter;
        }
    }
}