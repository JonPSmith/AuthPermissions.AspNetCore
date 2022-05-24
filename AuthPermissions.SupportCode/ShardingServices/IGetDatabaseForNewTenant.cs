// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.SupportCode.ShardingServices;

public interface IGetDatabaseForNewTenant
{
    Task<string> FindBestDatabaseInfoNameAsync(bool hasOwnDb);
}