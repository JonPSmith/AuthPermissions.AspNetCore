// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using StatusGeneric;

namespace Example5.MvcWebApp.AzureAdB2C.Models;

public class AcceptInviteAzureAdDto
{
    public string Verify { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
}