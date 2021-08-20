using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthUsers
{
    public class ErrorPageModel : PageModel
    {

        public IEnumerable<string> Data { get; private set; }

        public void OnGet(string allErrors)
        {
            Data = allErrors.Split(Environment.NewLine);
        }
    }
}
