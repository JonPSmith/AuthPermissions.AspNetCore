// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;

namespace Example3.InvoiceCode.Services
{
    public interface ICompanyNameService
    {
        /// <summary>
        /// This returns the company name if a tenant user is logged in. Otherwise it is null
        /// </summary>
        /// <returns></returns>
        Task<string> GetCurrentCompanyNameAsync();
    }

    public class CompanyNameService : ICompanyNameService
    {
        private readonly InvoicesDbContext _context;

        public CompanyNameService(InvoicesDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This returns the company name if a tenant user is logged in. Otherwise it is null
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetCurrentCompanyNameAsync()
        {
            var company = await _context.Companies.SingleOrDefaultAsync();
            return company?.CompanyName;
        }
    }
}