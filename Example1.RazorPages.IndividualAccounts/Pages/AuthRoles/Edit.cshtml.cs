using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using ExamplesCommonCode.CommonAdmin;
using GenericServices.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthRoles
{
    public class EditModel : PageModel
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public EditModel(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        [BindProperty] public RoleCreateUpdateDto Data { get; set; }

        public async Task<IActionResult> OnGet(string roleName)
        {
            var role = await
                _authRolesAdmin.QueryRoleToPermissions().SingleOrDefaultAsync(x => x.RoleName == roleName);
            if (role == null)
                return RedirectToPage("ErrorPage", new { allErrors = $"Could not find a role with the name '{roleName}" });

            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            Data = RoleCreateUpdateDto.SetupForCreateUpdate(role.RoleName, role.Description, role.PermissionNames,
                permissionsDisplay);

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authRolesAdmin
                .UpdateRoleToPermissionsAsync(Data.RoleName, Data.GetSelectedPermissionNames(), Data.Description);

            if (status.IsValid)
                return RedirectToPage("ListRoles", new { message = status.Message });

            //Errors 
            status.CopyErrorsToModelState(ModelState);
            return Page();
        }

    }
}
