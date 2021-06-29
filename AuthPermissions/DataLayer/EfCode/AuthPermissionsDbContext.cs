// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthPermissions.DataLayer.EfCode
{
    public class AuthPermissionsDbContext : DbContext
    {
        public AuthPermissionsDbContext(DbContextOptions<AuthPermissionsDbContext> options)
            : base(options)
        { }

        public DbSet<AuthUser> AuthUsers { get; set; }
        public DbSet<RoleToPermissions> RoleToPermissions { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<UserToRole> UserToRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("authp");
            
            //Add concurrency token to every entity 
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.AddProperty("ConcurrencyToken", typeof(byte[]))
                    .SetColumnType("ROWVERSION");
                entityType.FindProperty("ConcurrencyToken")
                    .ValueGenerated = ValueGenerated.OnAddOrUpdate;
                entityType.FindProperty("ConcurrencyToken")
                    .IsConcurrencyToken = true;
            }

            modelBuilder.Entity<AuthUser>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<UserToRole>()
                .HasKey(x => new { x.UserId, x.RoleName });

            modelBuilder.Entity<Tenant>().HasKey(x => x.TenantId);
            modelBuilder.Entity<Tenant>()
                .HasIndex(x => x.TenantName)
                .IsUnique();
            modelBuilder.Entity<Tenant>()
                .HasIndex(x => x.ParentDataKey);

            modelBuilder.Entity<RefreshToken>()
                .Property(x => x.TokenValue)
                .IsUnicode(false)
                .HasMaxLength(AuthDbConstants.RefreshTokenValueSize)
                .IsRequired();

            modelBuilder.Entity<RefreshToken>()
                .HasKey(x => x.TokenValue);
        }
    }
}