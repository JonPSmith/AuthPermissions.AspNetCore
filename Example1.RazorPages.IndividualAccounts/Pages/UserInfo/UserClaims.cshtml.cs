using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.UserInfo
{
    public class UserClaimsModel : PageModel
    {
        public ClaimsPrincipal ThisUser { get; set; }

        public void OnGet()
        {
            ThisUser = User;
        }
    }
}
