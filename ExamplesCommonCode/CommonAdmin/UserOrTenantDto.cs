// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace ExamplesCommonCode.CommonAdmin;

public class UserOrTenantDto
{
    public UserOrTenantDto(bool user, string name)
    {
        Type = user ? "User" : "Tenant";
        Name = name;
    }

    public string Type { get;  }
    public string Name { get; }
}