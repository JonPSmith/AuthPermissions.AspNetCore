// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;

namespace Test.StubClasses;

public class StubDatabaseInformationOptions : DatabaseInformationOptions
{
    public StubDatabaseInformationOptions(AuthPDatabaseTypes dbType = AuthPDatabaseTypes.SqlServer)
    {
        Name = "Default Database";
        ConnectionName = "DefaultConnection";
        DatabaseType = dbType.ToString();
    }
}