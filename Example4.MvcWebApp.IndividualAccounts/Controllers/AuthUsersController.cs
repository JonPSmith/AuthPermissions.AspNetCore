using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.DataKeyCode;
using AuthPermissions.DataLayer.EfCode;
using Example4.MvcWebApp.IndividualAccounts.Models;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.EntityFrameworkCore;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class AuthUsersController : Controller
    {
        private readonly IAuthUsersAdminService _authUsersAdmin;
        private readonly AuthPermissionsDbContext _context;

        public AuthUsersController(IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            _authUsersAdmin = authUsersAdmin;
            _context = context;
        }

        // List users filtered by authUser tenant
        //[HasPermission(Example4Permissions.UserRead)]
        public async Task<ActionResult> Index(string message)
        {
            var authDataKey = User.GetAuthDataKey();
            var userQuery = _authUsersAdmin.QueryAuthUsers(authDataKey);
            var usersToShow = await AuthUserDisplay.SelectQuery(userQuery.OrderBy(x => x.Email)).ToListAsync();

            ViewBag.Message = message;

            return View(usersToShow);
        }

        public async Task<ActionResult> Edit(string userId)
        {
            var status = await AuthUserChange.BuildAuthUserUpdateAsync(userId,_authUsersAdmin, _context);
            if(status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(status.Result);
        }

        public async Task<ActionResult> EditFromSync(AuthUserChange input)
        {
            if (input.FoundChange == SyncAuthUserChanges.Add)
            {
                await input.SetupDropDownListsAsync(_context);
                input.RoleNames = new List<string>();
                return View(nameof(Edit), input);
            }

            var status = await AuthUserChange.BuildAuthUserUpdateAsync(input.UserId, _authUsersAdmin, _context);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(nameof(Edit), status.Result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AuthUserChange input)
        {
            if (!ModelState.IsValid)
            {
                await input.SetupDropDownListsAsync(_context);//refresh dropdown
                return View(input);
            }
            
            var status = await input.ChangeAuthUserFromDataAsync(_authUsersAdmin, _context);
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
        public async Task<ActionResult> Delete(AuthUserDisplay user)
        {
            var status1 = await _authUsersAdmin.FindAuthUserByUserIdAsync(user.UserId);
            if (status1.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status1.GetAllErrors() });

            _context.Remove(status1.Result);
            var status2 = await _context.SaveChangesWithChecksAsync();
            if (status1.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status1.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = $"Successfully deleted the user {user.UserName ?? user.Email}" });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object) errorMessage);
        }
    }
}
