// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example3.MvcWebApp.IndividualAccounts.Models;

public class InviteUserDto
{
    public InviteUserDto(string email, string tenantName, string url)
    {
        Email = email;
        TenantName = tenantName;
        Url = url;
    }

    public string Email { get; }
    public string TenantName { get;  }
    public string Url { get;  }

}