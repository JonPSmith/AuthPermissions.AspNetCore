// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.MvcWebApp.ShardingOnly.Models;

public class AcceptInviteDto
{
    public string Verify { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public bool IsPersistent { get; set; }
}