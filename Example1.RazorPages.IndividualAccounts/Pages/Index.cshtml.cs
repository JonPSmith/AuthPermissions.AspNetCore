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

        public IndexModel(UserManager<IdentityUser> userManager, IStringLocalizerFactory factory)
        {
            _userManager = userManager;
            var type = typeof(ResourceLocalize);
            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);
            _localizer = factory.Create("ResourceLocalize", assemblyName.Name);
        }

        [ModelBinder] 
        public AppSummary AppSummary { get; } = new AppSummary();

        [ModelBinder] 
        public List<IdentityUser> Users { get; private set; }

        public void OnGet()
        {
            Users = _userManager.Users.ToList();
            var loc = _localizer["Test"];
            var result = loc.ResourceNotFound ? "Not found: " + loc.SearchedLocation : loc.Value;
        }
    }
}
