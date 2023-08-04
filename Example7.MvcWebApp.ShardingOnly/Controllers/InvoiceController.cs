// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using Example7.SingleLevelShardingOnly.AppStart;
using Example7.SingleLevelShardingOnly.Dtos;
using Example7.SingleLevelShardingOnly.EfCoreClasses;
using Example7.SingleLevelShardingOnly.EfCoreCode;
using Example7.SingleLevelShardingOnly.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example7.MvcWebApp.ShardingOnly.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ShardingOnlyDbContext _context;

        public InvoiceController(ShardingOnlyDbContext context)
        {
            _context = context;
        }

        [HasPermission(Example7Permissions.InvoiceRead)]
        public async Task<IActionResult> Index(string message)
        {
            ViewBag.Message = message;

            var listInvoices = await InvoiceSummaryDto.SelectInvoices(_context.Invoices)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync();
            return View(listInvoices);
        }

        [HasPermission(Example7Permissions.InvoiceCreate)]
        public IActionResult CreateInvoice()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example7Permissions.InvoiceCreate)]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            var builder = new ExampleInvoiceBuilder();
            var newInvoice = builder.CreateRandomInvoice(AddTenantNameClaim.GetTenantNameFromUser(User), invoice.InvoiceName);
            _context.Add(newInvoice);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { message = $"Added the invoice '{newInvoice.InvoiceName}'." });
        }
    }
}
