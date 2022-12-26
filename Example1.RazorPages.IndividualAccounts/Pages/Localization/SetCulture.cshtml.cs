using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace Example1.RazorPages.IndividualAccounts.Pages.Localization;

public class SetCultureModel : PageModel
{
    private readonly AuthPermissionsOptions _authOptions;

    public SetCultureModel(AuthPermissionsOptions authOptions)
    {
        _authOptions = authOptions ;
    }

    [BindProperty] public List<SelectListItem> CultureList { get; set; }
    [BindProperty] public string SetLanguage { get; set; }

    public void OnGet()
    {
        CultureList = _authOptions.InternalData.SupportedCultures
            .Select(x => new CultureInfo(x))
            .Select(c => new SelectListItem { Value = c.Name, Text = c.DisplayName })
            .ToList();
    }

    public IActionResult OnPost()
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(SetLanguage))
        );

        return RedirectToPage("/Index");
    }
}