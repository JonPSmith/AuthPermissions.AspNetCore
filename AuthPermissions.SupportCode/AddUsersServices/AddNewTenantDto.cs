// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This holds user data (via the inherit of the <see cref="AddUserDataDto"/>)
/// and the Tenant information.
/// </summary>
public class AddNewTenantDto : AddUserDataDto
{

    /// <summary>
    /// This holds the name of the version of the multi-tenant features the user has selected
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// This is the name of the new tenant the user wants to create
    /// </summary>
    public string TenantName { get; set; }

    /// <summary>
    /// If using hierarchical tenant, then you need to provide the TenantId of the parent
    /// </summary>
    public int ParentId { get; set; }

    /// <summary>
    /// If <see cref="TenantTypes.AddSharding"/> is on you must set this
    /// It true the new tenant has its own database, otherwise shared.
    /// NOTE: The 
    /// </summary>
    public bool? HasOwnDb { get; set; }


}