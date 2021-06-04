// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.Classes;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.DataLayer.EfCode
{
    public class AuthPermissionsDbContext : DbContext
    {
        public AuthPermissionsDbContext(DbContextOptions<AuthPermissionsDbContext> options)
            : base(options)
        { }

        public DbSet<AuthUser> Users { get; set; }
        public DbSet<RoleToPermissions> RoleToPermissions { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<UserToRole> UserToRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("authp");

            modelBuilder.Entity<UserToRole>()
                .HasKey(x => new { x.UserId, x.RoleName });

            modelBuilder.Entity<Tenant>().HasKey(x => x.TenantId);
            modelBuilder.Entity<Tenant>()
                .Property("_parentDataKey")
                .HasColumnName("ParentDataKey");
        }
    }
}