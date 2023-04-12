// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using LocalizeMessagesAndErrors.UnitTestingCode;

namespace Test.TestHelpers;

public static class ShardingHelpers
{
    public static List<IDatabaseSpecificMethods> GetDatabaseSpecificMethods()
    {
        return new List<IDatabaseSpecificMethods>
        {
            new SqlServerDatabaseSpecificMethods("en".SetupAuthPLoggingLocalizer()),
            new PostgresDatabaseSpecificMethods("en".SetupAuthPLoggingLocalizer()),
        };
    }
}