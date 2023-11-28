// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Test.DiTestHelpers
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
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