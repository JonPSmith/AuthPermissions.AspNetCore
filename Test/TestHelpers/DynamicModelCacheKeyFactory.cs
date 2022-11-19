// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Test.TestHelpers;

public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime = true)
        => context is AuthPermissionsDbContext dynamicContext
            ? (context.GetType(), dynamicContext.ProviderName)
            : (object)context.GetType();
}