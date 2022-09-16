// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.BlazorWASMandWebApi.Domain;

public class RetailOutlet : IDataKeyFilterReadOnly
{
    private RetailOutlet() { } //Needed by EF Core

    public RetailOutlet(int authPTenantId, string fullName, string dataKey)
    {
        if (authPTenantId == 0) throw new ArgumentNullException(nameof(authPTenantId));

        AuthPTenantId = authPTenantId;
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        ShortName = ExtractEndLeftTenantName(FullName);
        DataKey = dataKey ?? throw new ArgumentNullException(nameof(dataKey));
    }

    public int RetailOutletId { get; private set; }

    /// <summary>
    /// This contains the fullname of the AuthP Tenant
    /// </summary>
    public string FullName { get; private set; } = default!;

    public string ShortName { get; private set; } = default!;

    /// <summary>
    /// This contains the datakey from the AuthP's Tenant
    /// </summary>
    public string DataKey { get; private set; } = default!;

    /// <summary>
    /// This is here in case a hierarchical AuthP Tenant has its position in the hierarchy changes.
    /// It this happens the DataKey and the FullName will change, so you need to update the RetailOutlet
    /// </summary>
    public int AuthPTenantId { get; private set; }

    //------------------------------------------------------------------------------
    //access methods

    public void UpdateDataKey(string newDataKey)
    {
        DataKey = newDataKey;
    }

    public void UpdateNames(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            throw new ArgumentException("The FullName cannot be null or empty");

        FullName = fullName;
        ShortName = ExtractEndLeftTenantName(FullName);
    }

    /// <summary>
    /// This will return a single tenant name. If it's hierarchical it returns the final name
    /// </summary>
    /// <param name="fullTenantName"></param>
    /// <returns></returns>
    private static string ExtractEndLeftTenantName(string fullTenantName)
    {
        var lastIndex = fullTenantName.LastIndexOf('|');
        var thisLevelTenantName = lastIndex < 0 ? fullTenantName : fullTenantName.Substring(lastIndex + 1).Trim();
        return thisLevelTenantName;
    }
}