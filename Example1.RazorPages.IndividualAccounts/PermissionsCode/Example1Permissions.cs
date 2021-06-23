// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Example1.RazorPages.IndividualAccounts.PermissionsCode
{
    public enum Example1Permissions : ushort
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
        [Obsolete("Some message to say why obsoleted, e.g. split into two members xxx and yyy in version 2.10.0")] 
        [Display(GroupName = "Old", Name = "Not used", Description = "example of old permission")]
        OldPermissionNotUsed = 100,

        /// <summary>
        /// A enum member with no <see cref="DisplayAttribute"/> can be used, but its not shown in the PermissionDisplay
        /// Useful if are working on new permissions but you don't want it to be used by admin people 
        /// </summary>
        AnotherPermission = 200,

        //Setting the AutoGenerateFilter to true in the display allows we can exclude this permissions
        //to admin users who aren't allowed alter this permissions
        //Useful for multi-tenant applications where you can set up company-level admin users
        [Display(GroupName = "SuperAdmin", Name = "AccessAll", 
            Description = "This allows the user to access every feature", AutoGenerateFilter = true)]
        AccessAll = ushort.MaxValue,
    }
}