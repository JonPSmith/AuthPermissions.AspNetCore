// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Test.DiTestHelpers
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        /// <summary>
        /// This is needed for EF Core 9 and above  when building a multi-tenant application.
        /// This allows you to add more than one migration on this database
        /// NOTE: You don't need to add this code if you are building a Sharding-Only type multi-tenant.  
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(x => x.Ignore(RelationalEventId.PendingModelChangesWarning));
            base.OnConfiguring(optionsBuilder);
        }
    }

    public class CustomIdentityUser : IdentityUser
    {
        public string FullName { get; set; }
    }

    public class CustomApplicationDbContext : IdentityDbContext<CustomIdentityUser>
    {
        public CustomApplicationDbContext(DbContextOptions<CustomApplicationDbContext> options)
            : base(options)
        {
        }
    }
}