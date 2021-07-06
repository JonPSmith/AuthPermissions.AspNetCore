// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Example4.ShopCode.EfCoreClasses.SupportTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Example4.ShopCode.EfCoreCode
{
    public static class DataKeyQueryExtension
    {
        public static void AddUserIdQueryFilter(this IMutableEntityType entityData,
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
            Expression<Func<TEntity, bool>> filter = x => x.DataKey.StartsWith(dataKeyFilterProvider.DataKey);
            return filter;
        }
    }
}