using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthUsers
{
    public class SyncUsersModel : PageModel
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;

        public SyncUsersModel(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        [BindProperty]
        public List<SyncAuthUserWithChange> Data { get; set; }

        public async Task OnGet()
        {
            Data = await _authUsersAdmin.SyncAndShowChangesAsync();
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authUsersAdmin.ApplySyncChangesAsync(Data);
            return status.HasErrors 
                ? RedirectToPage("ErrorPage", new { allErrors = status.GetAllErrors() }) 
                : RedirectToPage("ListUsers", new { message = status.Message });
        }
    }
}
