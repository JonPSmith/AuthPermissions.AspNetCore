using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices;
using Example5.MvcWebApp.AzureAdB2C.Models;
using Example5.MvcWebApp.AzureAdB2C.PermissionCode;
using ExamplesCommonCode.CommonAdmin;
using LocalizeMessagesAndErrors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example5.MvcWebApp.AzureAdB2C.Controllers
{
    public class AuthUsersController : Controller
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;

        public AuthUsersController(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        //[HasPermission(Example4Permissions.UserRead)]
        public async Task<ActionResult> Index(string message)
        {
            var userQuery = _authUsersAdmin.QueryAuthUsers();
            var usersToShow = await AuthUserDisplay.TurnIntoDisplayFormat(userQuery.OrderBy(x => x.Email)).ToListAsync();

            ViewBag.Message = message;

            return View(usersToShow);
        }

        [HasPermission(Example5Permissions.InviteUsers)]
        public async Task<ActionResult> InviteUser([FromServices] IInviteNewUserService inviteService)
        {
            var setupInvite = new InviteUserSetup
            {
                AllRoleNames = await _authUsersAdmin.GetRoleNamesForUsersAsync(User.GetUserIdFromUser()),
                ExpirationTimesDropdown = inviteService.ListOfExpirationTimes()
            };

            return View(setupInvite);
        }

        [HasPermission(Example5Permissions.InviteUsers)]
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

        [HasPermission(Example5Permissions.UserChange)]
        public async Task<ActionResult> Edit(string userId)
        {
            var status = await SetupManualUserChange.PrepareForUpdateAsync(userId,_authUsersAdmin);
            if(status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(status.Result);
        }

        public async Task<ActionResult> Create(string userId)
        {
            var authUserChange = await SetupManualUserChange.PrepareForCreateAsync(userId, _authUsersAdmin);
            return View(authUserChange);
        }

        /// <summary>
        /// This can be from the sync display, taking the recommended updates from the sync
        /// It can be a create or an update 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<ActionResult> EditFromSync(SetupManualUserChange input)
        {
            switch (input.FoundChangeType)
            {
                case SyncAuthUserChangeTypes.NoChange:
                    return RedirectToAction(nameof(Index),
                        new { message = "The entry was marked as 'No Change' so it was ignored." });
                case SyncAuthUserChangeTypes.Create:
                    var createData = await SetupManualUserChange.PrepareForCreateAsync(input.UserId, _authUsersAdmin);
                    return View(nameof(Create), createData);
                case SyncAuthUserChangeTypes.Update:
                    var status = await SetupManualUserChange.PrepareForUpdateAsync(input.UserId, _authUsersAdmin);
                    if (status.HasErrors)
                        return RedirectToAction(nameof(ErrorDisplay),
                            new { errorMessage = status.GetAllErrors() });
                    return View(nameof(Edit), status.Result);
                case SyncAuthUserChangeTypes.Delete:
                    return RedirectToAction(nameof(Delete), new { userId = input.UserId});
            }

            throw new ArgumentOutOfRangeException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateUpdate([FromServices] IAuthPDefaultLocalizer localizeProvider, 
            SetupManualUserChange input)
        {
            if (!ModelState.IsValid)
            {
                await input.SetupDropDownListsAsync(_authUsersAdmin);//refresh dropdown
                return View(input.FoundChangeType.ToString(),  input.FoundChangeType);
            }

            var status = await input.ChangeAuthUserFromDataAsync(_authUsersAdmin, localizeProvider);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new {message = status.Message});
        }


        public async Task<ActionResult> SyncUsers()
        {
            var syncChanges = await _authUsersAdmin.SyncAndShowChangesAsync();
            return View(syncChanges);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]        
        //NOTE: the input be called "data" because we are using JavaScript to send that info back
        public async Task<ActionResult> SyncUsers(IEnumerable<SyncAuthUserWithChange> data)
        {
            var status = await _authUsersAdmin.ApplySyncChangesAsync(data);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors()});

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }



        // GET: AuthUsersController/Delete/5
        public async Task<ActionResult> Delete(string userId)
        {
            var status = await _authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(AuthUserDisplay.DisplayUserInfo(status.Result));
        }

        // POST: AuthUsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(AuthIdAndChange input)
        {
            var status = await _authUsersAdmin.DeleteUserAsync(input.UserId);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object) errorMessage);
        }
    }
}
