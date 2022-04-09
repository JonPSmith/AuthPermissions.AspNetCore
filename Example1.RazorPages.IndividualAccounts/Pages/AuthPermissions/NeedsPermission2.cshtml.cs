using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.PermissionsCode;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthPermissions
{
    public class NeedsPermission2Model : PageModel
    {
        public IActionResult OnGet()
        {
            if (!User.HasPermission(Example1Permissions.Permission2))
                return Challenge();

            return Page();
        }
    }
}
