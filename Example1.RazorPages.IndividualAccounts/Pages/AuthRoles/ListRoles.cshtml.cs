using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthRoles
{
    public class ListRolesModel : PageModel
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public ListRolesModel(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        public List<RoleWithPermissionNamesDto> AuthRolesList { get; private set; }
        public string Message { get; set; }

        public async Task OnGet(string message)
        {
            Message = message;
            AuthRolesList = await
                _authRolesAdmin.QueryRoleToPermissions().ToListAsync();
        }
    }
}
