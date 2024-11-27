using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Example3.MvcWebApp.IndividualAccounts.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) {}
        
        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            //This allows you to add more that one migration on this database 
            optionsBuilder.ConfigureWarnings(x => x.Ignore(RelationalEventId.PendingModelChangesWarning));
            base.OnConfiguring(optionsBuilder);
        }
    }
}
