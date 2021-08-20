using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthUsers
{
    public class DeleteModel : PageModel
    {

        private readonly IAuthUsersAdminService _authUsersAdmin;

        public DeleteModel(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        [BindProperty] public string UserId { get; set; }
        [BindProperty] public string UserName { get; set; }
        [BindProperty] public string Email { get; set; }

        public async Task<IActionResult> OnGet(string userId)
        {
            var status = await _authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (status.HasErrors)
                RedirectToPage("ErrorPage", new { allErrors = status.GetAllErrors() });

            UserId = userId;
            UserName = status.Result.UserName;
            Email = status.Result.Email;

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authUsersAdmin.DeleteUserAsync(UserId);
            return status.HasErrors
                ? RedirectToPage("ErrorPage", new { allErrors = status.GetAllErrors() })
                : RedirectToPage("ListUsers", new { message = status.Message });
        }
    }
}
