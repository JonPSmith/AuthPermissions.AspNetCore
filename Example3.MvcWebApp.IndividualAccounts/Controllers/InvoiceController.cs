using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example3.InvoiceCode.Dtos;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;

namespace Example3.MvcWebApp.IndividualAccounts.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly InvoicesDbContext _context;

        public InvoiceController(InvoicesDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var listInvoices = await InvoiceSummaryDto.SelectInvoices(_context.Invoices).ToListAsync();
            return View(listInvoices);
        }
    }
}
