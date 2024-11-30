# Updating your code from AuthPermissions.AspNetCore to EF Core 9

EF Core 9 has added code to ensure that migrations are executed correctly - see EF Core 9's [updated migrations](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew#migrations). The AuthP library already have code to ensure that migrations are executed correctly, but one of the EF Core 9's changes is to check that only one migration is executed on a database. It does this to ensure that migrations don't clash.

AuthP does apply two migrations when building multi-tenant applications. This means you need to add a `OnConfiguring` the code shown to your tenant's `DbContext`. The code below comes from [Example3's tenant `DbContext`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.InvoiceCode/EfCoreCode/InvoicesDbContext.cs) - the new code for EF Core 9 is the new `OnConfiguring` method. See below.

```c#
namespace Example3.MvcWebApp.IndividualAccounts.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) {}

    /// <summary>
    /// This is needed for EF Core 9 and above  when building a multi-tenant application.
    /// This allows you to add more than one migration on this database
    /// NOTE: You don't need to add this code if you are building a Sharding-Only type multi-tenant.  
    /// </summary>
    /// <param name="optionsBuilder"></param>
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => 
           x.Ignore(RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    //other code left out...
}
```

NOTE: If you get an `InvalidOperationException` with the text shown below on startup, then you haven't added the `OnConfiguring` shown above, then you haven't added the code above.

```text
An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': The model for context '-your tenant context-' has pending changes. Add a new migration before updating the database. This exception can be suppressed or logged by passing event ID 'RelationalEventId.PendingModelChangesWarning' to the 'ConfigureWarnings' method in 'DbContext.OnConfiguring' or 'AddDbContext'.
```

END
