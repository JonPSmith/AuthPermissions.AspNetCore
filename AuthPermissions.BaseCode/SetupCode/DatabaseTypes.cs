// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.SetupCode
{
    /// <summary>
    /// The different database types that AuthPermissions supports
    /// </summary>
    public enum AuthPDatabaseTypes
    {
        /// <summary>
        /// This is the default - AuthPermissions will throw an exception to say you must define the database type
        /// </summary>
        NotSet,
        /// <summary>
        /// This is a in-memory database - useful for unit testing
        /// </summary>
        SqliteInMemory,
        /// <summary>
        /// SQL Server database is used
        /// </summary>
        SqlServer,
        /// <summary>
        /// Postgres database is used (NOTE: the Enum name matches the EF Core's ProviderName
        /// </summary>
        PostgreSQL,
        /// <summary>
        /// This says that you have provided a database type other than the built-in <see cref="SqlServer"/> or <see cref="PostgreSQL"/>.
        /// </summary>
        CustomDatabase

    }
}