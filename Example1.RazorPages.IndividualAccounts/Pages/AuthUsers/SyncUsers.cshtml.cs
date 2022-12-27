using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using GenericServices.AspNetCore;
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

            if (status.IsValid)
                return RedirectToPage("ListUsers", new { message = status.Message });

            //Errors 
            status.CopyErrorsToModelState(ModelState);
            return Page();
        }
    }
}
