using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthUsers
{
    public class ListUsersModel : PageModel
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;

        public ListUsersModel(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        public List<AuthUserDisplay> AuthUserList { get; private set; }
        public string Message { get; set; }

        public async Task OnGet(string message)
        {
            Message = message;
            var userQuery = _authUsersAdmin.QueryAuthUsers();
            AuthUserList = await AuthUserDisplay.TurnIntoDisplayFormat(userQuery.OrderBy(x => x.Email)).ToListAsync();
        }
    }
}
