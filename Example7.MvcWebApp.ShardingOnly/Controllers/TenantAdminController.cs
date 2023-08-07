// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.AddUsersServices;
using Example7.MvcWebApp.ShardingOnly.Models;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example7.MvcWebApp.ShardingOnly.Controllers
{
    public class TenantAdminController : Controller
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;

        public TenantAdminController(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        [HasPermission(Example7Permissions.UserRead)]
        public async Task<IActionResult> Index(string message)
        {
            var dataKey = User.GetAuthDataKeyFromUser();
            var userQuery = _authUsersAdmin.QueryAuthUsers(dataKey, User.GetDatabaseInfoNameFromUser());
            var usersToShow = await AuthUserDisplay.TurnIntoDisplayFormat(userQuery.OrderBy(x => x.Email)).ToListAsync();

            ViewBag.Message = message;

            return View(usersToShow);
        }

        public async Task<ActionResult> EditRoles(string userId)
        {
            var status = await SetupManualUserChange.PrepareForUpdateAsync(userId, _authUsersAdmin);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(status.Result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditRoles(SetupManualUserChange change)
        {
            var status = await _authUsersAdmin.UpdateUserAsync(change.UserId, roleNames: change.RoleNames);

            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }



        [HasPermission(Example7Permissions.InviteUsers)]
        public async Task<ActionResult> InviteUser([FromServices] IInviteNewUserService inviteService)
        {
            var setupInvite = new InviteUserSetup
            {
                AllRoleNames = await _authUsersAdmin.GetRoleNamesForUsersAsync(User.GetUserIdFromUser()),
                ExpirationTimesDropdown = inviteService.ListOfExpirationTimes()
            };

            return View(setupInvite);
        }

        [HasPermission(Example7Permissions.InviteUsers)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InviteUser([FromServices] IInviteNewUserService inviteUserServiceService, InviteUserSetup data)
        {
            var addUserData = new AddNewUserDto
            {
                Email = data.Email,
                Roles = data.RoleNames,
                TimeInviteExpires = data.InviteExpiration
            };
            var status = await inviteUserServiceService.CreateInviteUserToJoinAsync(addUserData, User.GetUserIdFromUser());
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            var inviteUrl = AbsoluteAction(Url, nameof(HomeController.AcceptInvite), "Home", new { verify = status.Result });

            return View("InviteUserUrl", new InviteUserResult(status.Message, inviteUrl));
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
