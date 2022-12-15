using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private readonly IOptions<RequestLocalizationOptions> _locOptions;

    public SetCultureModel(IOptions<RequestLocalizationOptions> locOptions)
    {
        _locOptions = locOptions;
    }

    [BindProperty] public List<SelectListItem> CultureList { get; set; }
    [BindProperty] public string SetLanguage { get; set; }

    public void OnGet()
    {
        CultureList = _locOptions.Value.SupportedUICultures
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