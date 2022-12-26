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
        private readonly IStringLocalizer _localizer;

        public IndexModel(UserManager<IdentityUser> userManager, IStringLocalizer<AppResourceClass> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
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
