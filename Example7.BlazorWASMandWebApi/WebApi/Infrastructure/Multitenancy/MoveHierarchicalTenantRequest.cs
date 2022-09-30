
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using System.ComponentModel.DataAnnotations;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Multitenancy;

public class MoveHierarchicalTenantRequest
{
    public int TenantId { get; set; }

    public int ParentId { get; set; }
}

