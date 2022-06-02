using Example5.MvcWebApp.AzureAdB2C.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.SupportCode.AddUsersServices;
using Example5.MvcWebApp.AzureAdB2C.PermissionCode;

namespace Example5.MvcWebApp.AzureAdB2C.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var appSummary = new AppSummaryPlus();

            if (User.Identity?.IsAuthenticated != true)
                appSummary.WhatTypeOfAuthUser = "There is no logged in user";
            else if (User.Claims.All(x => x.Type != PermissionConstants.PackedPermissionClaimType))
                appSummary.WhatTypeOfAuthUser = "Logged in user is not known by AuthP";
            else if (User.GetPackedPermissionsFromUser() == null)
                appSummary.WhatTypeOfAuthUser = "Logged in user is in AuthP, but no setup";
            else if (User.HasPermission(Example5Permissions.UserRead))
                appSummary.WhatTypeOfAuthUser = "Logged in user is an AuthP Admin user";
            else
                appSummary.WhatTypeOfAuthUser = "Logged in user is an AuthP normal user";

            return View(appSummary);
        }

        [AllowAnonymous]
        public ActionResult AcceptInvite(string verify)
        {
            return View(new AcceptInviteAzureAdDto { Verify = verify });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AcceptInvite([FromServices] IInviteNewUserService inviteUserServiceService,
            AcceptInviteAzureAdDto data)
        {
            var status = await inviteUserServiceService.AddUserViaInvite(data.Verify, data.Email, data.UserName);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View("AcceptedInvite", new InviteAddedDto(status.Message, status.Result.Password));
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
