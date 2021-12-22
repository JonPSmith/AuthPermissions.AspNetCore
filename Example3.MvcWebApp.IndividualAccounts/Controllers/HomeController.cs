using Example3.MvcWebApp.IndividualAccounts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.Dtos;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.Services;
using Microsoft.AspNetCore.Authorization;

namespace Example3.MvcWebApp.IndividualAccounts.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task< IActionResult> Index([FromServices] ICompanyNameService service, string message)
        {
            ViewBag.Message = message;

            var companyName = await service.GetCurrentCompanyNameAsync();

            if (companyName == null)
                return View(new AppSummary());

            return RedirectToAction("Index", "Invoice");
        }

        public IActionResult CreateTenant()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", new { message = "You can't create a new tenant because you are all ready logged in." });

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTenant(CreateTenantDto data)
        {

            return RedirectToAction("Index", new {message = "bad"});
            //return RedirectToAction("Index", "Invoice");
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
