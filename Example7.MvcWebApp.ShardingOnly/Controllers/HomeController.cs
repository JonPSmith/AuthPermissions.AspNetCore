// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using AuthPermissions.SupportCode.AddUsersServices;
using Example7.SingleLevelShardingOnly.Services;
using Example7.MvcWebApp.ShardingOnly.Models;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Drawing;
using AuthPermissions.AspNetCore.ShardingServices;

namespace Example7.MvcWebApp.ShardingOnly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(string message)
        {
            ViewBag.Message = message;

            if (AddTenantNameClaim.GetTenantNameFromUser(User) == null)
                return View(new AppSummary());

            return RedirectToAction("Index", "Invoice");
        }

        public IActionResult CreateTenant([FromServices] IGetSetShardingEntries service)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", new { message = "You can't create a new tenant because you are all ready logged in." });

            return View(new AddNewTenantDto
            {
                HasOwnDb = true,
                PossibleRegions = service.GetConnectionStringNames()
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTenant([FromServices] ISignInAndCreateTenant userRegisterInvite,
            AddNewTenantDto tenantDto, AddNewUserDto newUserDto)
        {
            var status = await userRegisterInvite.SignUpNewTenantWithVersionAsync(newUserDto, tenantDto,
                Example7CreateTenantVersions.TenantSetupData);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index),
                new { message = status.Message });
        }

        [AllowAnonymous]
        public ActionResult AcceptInvite(string verify)
        {
            return View(new AcceptInviteDto { Verify = verify });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AcceptInvite([FromServices] IInviteNewUserService inviteUserServiceService,
            string verify, string email, string userName, string password, bool isPersistent)
        {
            var status = await inviteUserServiceService.AddUserViaInvite(verify, email, null, password, isPersistent);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index),
                new { message = status.Message });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
