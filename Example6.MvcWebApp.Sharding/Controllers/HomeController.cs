using System.Diagnostics;
using Example6.MvcWebApp.Sharding.Models;
using Example6.SingleLevelSharding.Dtos;
using Example6.SingleLevelSharding.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Example6.MvcWebApp.Sharding.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task< IActionResult> Index(string message)
        {
            ViewBag.Message = message;


            if (AddTenantNameClaim.GetTenantNameFromUser(User) == null)
                return View(new AppSummary());

            return RedirectToAction("Index", "Invoice");
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
