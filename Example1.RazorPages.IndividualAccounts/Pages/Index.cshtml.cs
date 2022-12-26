using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AuthPermissions.BaseCode;
using Example1.RazorPages.IndividualAccounts.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Example1.RazorPages.IndividualAccounts.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [ModelBinder] 
        public AppSummary AppSummary { get; } = new AppSummary();

        [ModelBinder] 
        public List<IdentityUser> Users { get; private set; }

        public void OnGet()
        {
            Users = _userManager.Users.ToList();
        }
    }
}
