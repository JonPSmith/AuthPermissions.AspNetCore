// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;

namespace AuthPermissions.SetupCode.Factories
{

    /// <summary>
    /// Factory to cover the <see cref="ISyncAuthenticationUsers"/> service
    /// </summary>
    public class SyncAuthenticationUsersFactory : IAuthPServiceFactory<ISyncAuthenticationUsers>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Needs IServiceProvider
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SyncAuthenticationUsersFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returned a service which provides all the active users in the authentication provider in your application
        /// </summary>
        /// <param name="throwExceptionIfNull">If no service found and this is true, then throw an exception</param>
        /// <param name="callingMethod">This contains the name of the calling method</param>
        /// <returns>The service, or null </returns>
        public ISyncAuthenticationUsers GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            var service = (ISyncAuthenticationUsers)_serviceProvider.GetService(typeof(ISyncAuthenticationUsers));
            if (service == null && throwExceptionIfNull)
                throw new AuthPermissionsException(
                    $"A service (method {callingMethod}) needed the {nameof(ISyncAuthenticationUsers)} service, but you haven't registered it." +
                    $"You can do this using the {nameof(RegisterExtensions.RegisterAuthenticationProviderReader)} configuration method.");

            return service;
        }
    }
}