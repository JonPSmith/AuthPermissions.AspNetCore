// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthPermissions.BaseCode.DataLayer.EfCode
{
    /// <summary>
    /// This forms the AuthP's EF Core database
    /// </summary>
    public class AuthPermissionsDbContext : DbContext
    {
        private readonly ICustomConfiguration _customConfiguration;

        /// <summary>
        /// This overcomes the exception if the class used in the tests which uses the <see cref="IModelCacheKeyFactory"/>
        /// to allow testing of an DbContext that works with SqlServer and PostgreSQL 
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="eventSetups">OPTIONAL: If provided, then a method will be run within the ctor</param>
        /// <param name="customConfiguration">OPTIONAL: This allows to provide a custom configuration to the DbContext</param>
        public AuthPermissionsDbContext(DbContextOptions<AuthPermissionsDbContext> options,
            IEnumerable<IDatabaseStateChangeEvent> eventSetups = null,
            ICustomConfiguration customConfiguration = null)
            : base(options)
        {
            foreach (var eventSetup in eventSetups ?? Array.Empty<IDatabaseStateChangeEvent>())
            {
                eventSetup.RegisterEventHandlers(this);
            }

            ProviderName = this.Database.ProviderName;
            _customConfiguration = customConfiguration;
        }

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
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
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
                else if (Database.IsNpgsql())
                {
                    //see https://www.npgsql.org/efcore/modeling/concurrency.html
                    //and https://github.com/npgsql/efcore.pg/issues/19#issuecomment-253346255
                    entityType.AddProperty("xmin", typeof(uint))
                        .SetColumnType("xid");
                    entityType.FindProperty("xmin")
                        .ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    entityType.FindProperty("xmin")
                        .IsConcurrencyToken = true;
                }
                //NOTE: Sqlite doesn't support concurrency support, but if needed it can be added
                //see https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html
            }

            //This allows a developer to add a custom configuration to this DbContext
            //Typical use is to set up the concurrency tokens parts when using a custom database type  
            _customConfiguration?.ApplyCustomConfiguration(modelBuilder);

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
                .Property(x => x.ParentDataKey)
                .IsUnicode(false);
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