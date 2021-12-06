// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthPermissions.DataLayer.EfCode
{
    /// <summary>
    /// This forms the AuthP's EF Core database
    /// </summary>
    public class AuthPermissionsDbContext : DbContext
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="options"></param>
        public AuthPermissionsDbContext(DbContextOptions<AuthPermissionsDbContext> options)
            : base(options)
        { }

        /// <summary>
        /// The list of AuthUsers defining what roles and tenant that user has
        /// </summary>
        public DbSet<AuthUser> AuthUsers { get; set; }
        /// <summary>
        /// A list of all the AuthP's Roles, each with the permissions in each Role
        /// </summary>
        public DbSet<RoleToPermissions> RoleToPermissions { get; set; }
        /// <summary>
        /// When using AuthP's multi-tenant feature these define each tenant and the DataKey to access data in that tenant
        /// </summary>
        public DbSet<Tenant> Tenants { get; set; }
        /// <summary>
        /// This links AuthP's Roles to a AuthUser
        /// </summary>
        public DbSet<UserToRole> UserToRoles { get; set; }
        /// <summary>
        /// If you use AuthP's JWT refresh token, then the tokens are held in this entity
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }


        /// <summary>
        /// Set up AuthP's setup
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("authp");
            
            //Add concurrency token to every entity 
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (Database.IsSqlServer())
                {
                    entityType.AddProperty("ConcurrencyToken", typeof(byte[]))
                        .SetColumnType("ROWVERSION");
                    entityType.FindProperty("ConcurrencyToken")
                        .ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    entityType.FindProperty("ConcurrencyToken")
                        .IsConcurrencyToken = true;
                }
                //NOTE: Sqlite doesn't support concurrency support, but if needed it can be added
                //see https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html
            }

            modelBuilder.Entity<AuthUser>()
                .HasIndex(x => x.Email)
                .IsUnique();
            modelBuilder.Entity<AuthUser>()
                .HasIndex(x => x.UserName)
                .IsUnique();

            modelBuilder.Entity<AuthUser>()
                .HasMany(x => x.UserRoles)
                .WithOne()
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<RoleToPermissions>()
                .HasIndex(x => x.RoleType);

            modelBuilder.Entity<UserToRole>()
                .HasKey(x => new { x.UserId, x.RoleName });

            modelBuilder.Entity<Tenant>().HasKey(x => x.TenantId);
            modelBuilder.Entity<Tenant>()
                .HasIndex(x => x.TenantFullName)
                .IsUnique();
            modelBuilder.Entity<Tenant>()
                .HasIndex(x => x.ParentDataKey);
            modelBuilder.Entity<Tenant>()
                .HasMany(x => x.TenantRoles)
                .WithMany(x => x.Tenants);

            modelBuilder.Entity<RefreshToken>()
                .Property(x => x.TokenValue)
                .IsUnicode(false)
                .HasMaxLength(AuthDbConstants.RefreshTokenValueSize)
                .IsRequired();

            modelBuilder.Entity<RefreshToken>()
                .HasKey(x => x.TokenValue);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.AddedDateUtc)
                .IsUnique();

        }
    }
}