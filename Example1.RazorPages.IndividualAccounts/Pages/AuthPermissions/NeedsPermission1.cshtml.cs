using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthPermissions
{
    [HasPermission(Example1Permissions.Permission1)]
    public class NeedsPermission1Model : PageModel
    {
        public void OnGet()
        {
        }
    }
}
