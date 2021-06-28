// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;

namespace AuthPermissions.SetupCode.Factories
{
    public interface ISyncAuthenticationUsersFactory
    {
        /// <summary>
        /// Only call this if you need a service that implements the <see cref="ISyncAuthenticationUsers"/> interface.
        /// This service will return all the active users in the authentication provider in your application
        /// </summary>
        /// <returns></returns>
        ISyncAuthenticationUsers GetRequiredService();
    }

    /// <summary>
    /// Factory to cover the <see cref="ISyncAuthenticationUsers"/>, which is optional
    /// </summary>
    public class SyncAuthenticationUsersFactory : ISyncAuthenticationUsersFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SyncAuthenticationUsersFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Only call this if you need a service that implements the <see cref="ISyncAuthenticationUsers"/> interface.
        /// </summary>
        /// <returns>The returned service will return all the active users in the authentication provider in your application</returns>
        public ISyncAuthenticationUsers GetRequiredService()
        {
            var result = (ISyncAuthenticationUsers) _serviceProvider.GetService(typeof(ISyncAuthenticationUsers));
            if (result == null)
                throw new AuthPermissionsException(
                    $"A service needed the {nameof(ISyncAuthenticationUsers)} service, but you haven't registered it." +
                    $"You can do this using the {nameof(RegisterExtensions.RegisterAuthenticationProviderReader)} configuration method.");

            return result;
        }
    }
}