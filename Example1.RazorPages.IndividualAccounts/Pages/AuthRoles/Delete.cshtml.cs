using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using GenericServices.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthRoles
{
    public class DeleteModel : PageModel
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public DeleteModel(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        [BindProperty] public string RoleName { get; set; }

        public IActionResult OnGet(string roleName)
        {
            RoleName = roleName;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authRolesAdmin.DeleteRoleAsync(RoleName, true);

            if (status.IsValid)
                return RedirectToPage("ListRoles", new { message = status.Message });

            //Errors 
            status.CopyErrorsToModelState(ModelState);
            return Page();
        }
    }
}
