using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthRoles
{
    public class CreateModel : PageModel
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public CreateModel(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        [BindProperty] public RoleCreateUpdateDto Data { get; set; }

        public void OnGet()
        {
            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            Data = RoleCreateUpdateDto.SetupForCreateUpdate(null, null, null, permissionsDisplay);
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authRolesAdmin
                .CreateRoleToPermissionsAsync(Data.RoleName, Data.GetSelectedPermissionNames(), Data.Description);

            return status.HasErrors
                ? RedirectToPage("ErrorPage", new { allErrors = status.GetAllErrors() })
                : RedirectToPage("ListRoles", new { message = status.Message });
        }
    }
}
