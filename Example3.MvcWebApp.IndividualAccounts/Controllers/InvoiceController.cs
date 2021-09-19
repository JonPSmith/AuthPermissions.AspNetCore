using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.Dtos;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;
using Example3.InvoiceCode.Services;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace Example3.MvcWebApp.IndividualAccounts.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly InvoicesDbContext _context;
        private readonly ICompanyNameService _companyService;

        public InvoiceController(InvoicesDbContext context, ICompanyNameService companyService)
        {
            _context = context;
            _companyService = companyService;
        }

        public async Task<IActionResult> Index(string message)
        {
            ViewBag.Message = message;

            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();

            var listInvoices = await InvoiceSummaryDto.SelectInvoices(_context.Invoices)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync();
            return View(listInvoices);
        }

        [HasPermission(Example3Permissions.InvoiceCreate)]
        public async Task<IActionResult> CreateInvoice()
        {
            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example3Permissions.InvoiceCreate)]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            ViewBag.CompanyName = await _companyService.GetCurrentCompanyNameAsync();

            var builder = new ExampleInvoiceBuilder(null);
            _context.Add(builder.CreateRandomInvoice(invoice.InvoiceName));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { message = $"Added the invoice '{invoice.InvoiceName}'." });

        }

    }
}
