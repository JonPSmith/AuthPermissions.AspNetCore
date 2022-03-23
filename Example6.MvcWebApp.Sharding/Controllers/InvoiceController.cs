using AuthPermissions.AspNetCore;
using Example6.MvcWebApp.Sharding.PermissionsCode;
using Example6.SingleLevelSharding.AppStart;
using Example6.SingleLevelSharding.Dtos;
using Example6.SingleLevelSharding.EfCoreClasses;
using Example6.SingleLevelSharding.EfCoreCode;
using Example6.SingleLevelSharding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example6.MvcWebApp.Sharding.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ShardingSingleDbContext _context;

        public InvoiceController(ShardingSingleDbContext context)
        {
            _context = context;
        }

        [HasPermission(Example6Permissions.InvoiceRead)]
        public async Task<IActionResult> Index(string message)
        {
            ViewBag.Message = message;

            var listInvoices = await InvoiceSummaryDto.SelectInvoices(_context.Invoices)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync();
            return View(listInvoices);
        }

        [HasPermission(Example6Permissions.InvoiceCreate)]
        public IActionResult CreateInvoice()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example6Permissions.InvoiceCreate)]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            var builder = new ExampleInvoiceBuilder(null);
            var newInvoice = builder.CreateRandomInvoice(AddTenantNameClaim.GetTenantNameFromUser(User), invoice.InvoiceName);
            _context.Add(newInvoice);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { message = $"Added the invoice '{newInvoice.InvoiceName}'." });
        }

    }
}
