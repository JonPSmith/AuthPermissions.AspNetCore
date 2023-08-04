// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.SingleLevelShardingOnly.Dtos;

public class CreateTenantDto
{
    public string TenantName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Version { get; set; }

    public TenantVersionTypes GetTenantVersionType()
    {
        return string.IsNullOrWhiteSpace(Version) 
            ? TenantVersionTypes.NotSet 
            : Enum.Parse<TenantVersionTypes>(Version);
    }


    public override string ToString()
    {
        return $"{nameof(TenantName)}: {TenantName}, {nameof(Email)}: {Email}, {nameof(Password)}: {Password}, {nameof(Version)}: {Version}";
    }
}