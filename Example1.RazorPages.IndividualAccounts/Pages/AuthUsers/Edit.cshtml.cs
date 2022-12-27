using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Example1.RazorPages.IndividualAccounts.Model;
using GenericServices.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthUsers
{
    public class EditModel : PageModel
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;

        public EditModel(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        [BindProperty]
        public EditDto Data { get; set; }

        public async Task<IActionResult> OnGet(string userId)
        {
            var status = await EditDto.PrepareForUpdateAsync(userId, _authUsersAdmin);
            if (status.HasErrors)
                return RedirectToPage("ErrorPage", new { allErrors = status.GetAllErrors() });

            Data = status.Result;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authUsersAdmin.UpdateUserAsync(Data.UserId, Data.Email, Data.UserName, Data.SelectedRoleNames);

            if (status.IsValid)
                return RedirectToPage("ListUsers", new { message = status.Message });

            //Errors 
            status.CopyErrorsToModelState(ModelState);
            return Page();
        }
    }
}
