// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Example1.RazorPages.IndividualAccounts.PermissionsCode
{
    public enum Example1Permissions : short
    {
        NotSet = 0, //error condition

        //Here is an example of very detailed control over something
        [Display(GroupName = "Test", Name = "Access page", Description = "Used in a [HasPermission] attribute")]
        Permission1 = 1,

        [Display(GroupName = "Test", Name = "Display link", Description = "Used in User.UserHasThisPermission in page")]
        Permission2 = 2,

        //This is an example of what to do with permission you don't used anymore.
        //You don't want its number to be reused as it could cause problems 
        //Just mark it as obsolete and the PermissionDisplay code won't show it
        [Obsolete] [Display(GroupName = "Old", Name = "Not used", Description = "example of old permission")]
        OldPermissionNotUsed = 100,

        [Display(GroupName = "SuperAdmin", Name = "AccessAll", Description = "This allows the user to access every feature")]
        AccessAll = Int16.MaxValue,
    }
}