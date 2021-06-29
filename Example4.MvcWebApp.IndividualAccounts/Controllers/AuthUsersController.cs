using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.DataKeyCode;
using AuthPermissions.DataLayer.EfCode;
using Example4.MvcWebApp.IndividualAccounts.Models;
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
            var status = await AuthUserUpdate.BuildAuthUserUpdateAsync(userId,_authUsersAdmin, _context);
            if(status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(status.Result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AuthUserUpdate input)
        {
            if (!ModelState.IsValid)
            {
                await input.SetupAllRoleNamesAsync(_context);//refresh dropdown
                return View(input);
            }
            
            var status = await input.UpdateAuthUserFromDataAsync(_authUsersAdmin, _context);
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
        public ActionResult SyncUsers(AuthUserUpdate data)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }



        // GET: AuthUsersController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AuthUsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object) errorMessage);
        }
    }
}
