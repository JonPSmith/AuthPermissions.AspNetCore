using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorApp.IndividualAccounts.Pages.AuthBuiltIn
{
    [Authorize]
    public class LoggedInAuthorizeModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
