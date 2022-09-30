
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using System.ComponentModel.DataAnnotations;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Multitenancy;

public class CreateHierarchicalTenantRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(AuthDbConstants.TenantFullNameSize)]
    public string TenantName { get; set; } = default!;

    public int ParentId { get; set; }
}

