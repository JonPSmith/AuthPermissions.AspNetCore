// Copyright (c) 2018 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace Example1.RazorPages.IndividualAccounts.PermissionsCode
{
    /// <summary>
    /// This is an example of how you would manage what optional parts of your system a user can access
    /// NOTE: You can add Display attributes (as done on Permissions) to give more information about a module
    /// </summary>
    [Flags]
    public enum Example1PaidForModules : long
    {
        None = 0,
        Feature1 = 1,
        Feature2 = 2,
        Feature3 = 4
    }
}