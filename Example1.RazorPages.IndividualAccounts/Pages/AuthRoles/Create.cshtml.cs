using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using ExamplesCommonCode.CommonAdmin;
using GenericServices.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Example1.RazorPages.IndividualAccounts.Pages.AuthRoles
{
    public class CreateModel : PageModel
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public CreateModel(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        [BindProperty] public RoleCreateUpdateDto Data { get; set; }

        public void OnGet()
        {
            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            Data = RoleCreateUpdateDto.SetupForCreateUpdate(null, null, null, permissionsDisplay);
        }

        public async Task<IActionResult> OnPost()
        {
            var status = await _authRolesAdmin
                .CreateRoleToPermissionsAsync(Data.RoleName, Data.GetSelectedPermissionNames(), Data.Description);

            if (status.IsValid) 
                return RedirectToPage("ListRoles", new { message = status.Message });

            //Errors 
            status.CopyErrorsToModelState(ModelState);
            return Page();

        }
    }
}
