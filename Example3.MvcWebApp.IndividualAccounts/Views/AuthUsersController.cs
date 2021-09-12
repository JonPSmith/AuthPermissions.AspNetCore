using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using Example3.MvcWebApp.IndividualAccounts.Models;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class AuthUsersController : Controller
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;

        public AuthUsersController(IAuthUsersAdminService authUsersAdmin)
        {
            _authUsersAdmin = authUsersAdmin;
        }

        // List users filtered by authUser tenant
        //[HasPermission(Example4Permissions.UserRead)]
        public async Task<ActionResult> Index(string message)
        {
            var authDataKey = User.GetAuthDataKeyFromUser();
            var userQuery = _authUsersAdmin.QueryAuthUsers(authDataKey);
            var usersToShow = await AuthUserDisplay.TurnIntoDisplayFormat(userQuery.OrderBy(x => x.Email)).ToListAsync();

            ViewBag.Message = message;

            return View(usersToShow);
        }

        public async Task<ActionResult> Edit(string userId)
        {
            var status = await SetupManualUserChange.PrepareForUpdateAsync(userId,_authUsersAdmin);
            if(status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(status.Result);
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
