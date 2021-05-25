using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthBuiltIn
{
    [Authorize]
    public class LoggedInAuthorizeModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
