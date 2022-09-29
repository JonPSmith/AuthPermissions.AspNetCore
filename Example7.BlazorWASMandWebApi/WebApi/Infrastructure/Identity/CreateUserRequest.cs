
namespace Example7.BlazorWASMandWebApi.Infrastructure.Identity;

public class CreateUserRequest
{
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string TenantName { get; set; } = default!;
    public string Version { get; set; } = default!;
}

