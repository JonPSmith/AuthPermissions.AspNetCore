// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions
{
    /// <summary>
    /// Various checks using the the setup stage of AuthP
    /// </summary>
    public static class SetupCheckExtensions
    {
        /// <summary>
        /// Check the connection string
        /// </summary>
        /// <param name="connectionString"></param>
        public static void CheckConnectString(this string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new AuthPermissionsException("You must provide a connection string to the database");
        }

        /// <summary>
        /// This checks that the <see cref="SetupInternalData.AuthPAuthenticationType"/> enum is set to IndividualAccounts
        /// </summary>
        /// <param name="setupData"></param>
        public static void CheckAuthorizationIsIndividualAccounts(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.AuthPAuthenticationType != AuthPAuthenticationTypes.IndividualAccounts)
                throw new AuthPermissionsException(
                    "You must add the UsingIndividualAccounts method before you call this method");
        }

        /// <summary>
        /// This checks that the <see cref="SetupInternalData.AuthPAuthenticationType"/> enum is set to an auth (unless its in unit test mode)
        /// </summary>
        /// <param name="setupData"></param>
        public static void CheckThatAuthorizationTypeIsSetIfNotInUnitTestMode(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.AuthPDatabaseType == AuthPDatabaseTypes.SqliteInMemory)
                return;

            if (setupData.Options.InternalData.AuthPAuthenticationType ==
                AuthPAuthenticationTypes.NotSet)
                throw new AuthPermissionsException(
                    $"You must add a Using... method to define what authentication you are using, e.g. .IndividualAccountsAuthentication() for Individual Accounts");

        }

        /// <summary>
        /// This checks that the <see cref="SetupInternalData.AuthPDatabaseType"/> enum is set to a database type
        /// </summary>
        /// <param name="setupData"></param>
        public static void CheckDatabaseTypeIsSet(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.AuthPDatabaseType == AuthPDatabaseTypes.NotSet)
                throw new AuthPermissionsException("You must register a database type for the AuthP's database.");
        }

        /// <summary>
        /// This checks that the <see cref="SetupInternalData.AuthPDatabaseType"/> enum is set to a database type
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="callingMethod">DO NOT USE - used to get the calling method name</param>
        public static void CheckDatabaseTypeIsSetToSqliteInMemory(this AuthSetupData setupData, [CallerMemberName] string callingMethod = "")
        {
            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.SqliteInMemory)
                throw new AuthPermissionsException(
                    $"You can only call the {callingMethod} method if you used the {nameof(AuthPermissions.SetupExtensions.UsingInMemoryDatabase)} method.");
        }


    }
}