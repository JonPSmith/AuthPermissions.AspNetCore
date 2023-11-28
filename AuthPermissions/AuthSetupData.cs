// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    /// <summary>
    /// This class carries data through the setup extensions
    /// </summary>
    public class AuthSetupData
    {
        internal AuthSetupData(IServiceCollection services, AuthPermissionsOptions options)
        {
            Services = services;
            Options = options;
        }

        /// <summary>
        /// The DI ServiceCollection which AuthPermissions services, constants and policies are registered to
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// This holds the AuthPermissions options
        /// </summary>
        public AuthPermissionsOptions Options { get; }
    }
}