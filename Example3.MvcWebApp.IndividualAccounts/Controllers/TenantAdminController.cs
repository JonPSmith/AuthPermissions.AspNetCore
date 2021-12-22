using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.CommonCode;
using Example3.InvoiceCode.Services;
using Example3.MvcWebApp.IndividualAccounts.Models;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.IdentityModel.Tokens;

namespace Example3.MvcWebApp.IndividualAccounts.Controllers
{
    public class TenantAdminController : Controller
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;
        private readonly ICompanyNameService _companyService;

        public TenantAdminController(IAuthUsersAdminService authUsersAdmin, ICompanyNameService companyService)
        {
            _authUsersAdmin = authUsersAdmin;
            _companyService = companyService;
        }

        [HasPermission(Example3Permissions.EmployeeRead)]
        public async Task<IActionResult> Index(string message)
        {
            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();
            var dataKey = User.GetAuthDataKeyFromUser();

            var userQuery = _authUsersAdmin.QueryAuthUsers(dataKey);
            var usersToShow = await AuthUserDisplay.TurnIntoDisplayFormat(userQuery.OrderBy(x => x.Email)).ToListAsync();

            ViewBag.Message = message;

            return View(usersToShow);
        }

        [HasPermission(Example3Permissions.EmployeeRevokeActivate)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RevokeActivate(string userId, bool activate)
        {
            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();

            var findUserStatus = await _authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (findUserStatus.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = findUserStatus.GetAllErrors() });

            var newRoles = new List<string>();
            if (activate) newRoles.Add("Tenant User");

            var updateStatus = await _authUsersAdmin.UpdateUserAsync(userId, findUserStatus.Result.Email,
                findUserStatus.Result.UserName, newRoles, findUserStatus.Result.UserTenant.TenantFullName);
            if (updateStatus.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = findUserStatus.GetAllErrors() });

            return RedirectToAction(nameof(Index),
                new { message = $"{(activate ? "Activated" : "Revoked")} the user {findUserStatus.Result.UserName ?? findUserStatus.Result.Email}" });
        }

        [HasPermission(Example3Permissions.InviteUsers)]
        public async Task<ActionResult> InviteUser()
        {
            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();

            return View();
        }

        [HasPermission(Example3Permissions.InviteUsers)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InviteUser([FromServices] ITenantSetupService tenantSetup, string email)
        {
            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();
            var currentUser = (await _authUsersAdmin.FindAuthUserByUserIdAsync(User.Claims.GetUserIdFromClaims()))
                .Result;

            if (currentUser == null || currentUser.TenantId == null)
                return RedirectToAction(nameof(ErrorDisplay), new { errorMessage = "must be logged in and have a tenant" });

            var verify = tenantSetup.InviteUserToJoinTenantAsync((int)currentUser.TenantId, email);
            var inviteUrl = AbsoluteAction(Url, nameof(HomeController.AcceptInvite), "Home",  new { verify });

            return View("InviteUserUrl", new InviteUserDto(email, currentUser.UserTenant.TenantFullName, inviteUrl));
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }

        //-------------------------------------------------------

        //Thanks to https://stackoverflow.com/questions/30755827/getting-absolute-urls-using-asp-net-core
        public string AbsoluteAction(IUrlHelper url,
            string actionName,
            string controllerName,
            object routeValues = null)
        {
            string scheme = HttpContext.Request.Scheme;
            return url.Action(actionName, controllerName, routeValues, scheme);
        }
    }
}
