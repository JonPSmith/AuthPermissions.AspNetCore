
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

public class LoggedInUserController : VersionNeutralApiController
{
    public async Task<AuthUserDisplay?> AuthUserInfo([FromServices] IAuthUsersAdminService service)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.GetUserIdFromUser();
            var status = await service.FindAuthUserByUserIdAsync(userId);

            if (status.HasErrors)
                throw new Exception(status.GetAllErrors());

            return AuthUserDisplay.DisplayUserInfo(status.Result);
        }
        return null;
    }

    public List<string> UserPermissions([FromServices] IUsersPermissionsService service)
    {
        return service.PermissionsFromUser(HttpContext.User);
    }
}

