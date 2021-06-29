// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AdminCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.CommonCode;

namespace AuthPermissions.SetupCode.Factories
{

    /// <summary>
    /// Factory to cover the <see cref="ISyncAuthenticationUsers"/> service
    /// </summary>
    public class FindUserInfoServiceFactory : IAuthPServiceFactory<IFindUserInfoService>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Needs IServiceProvider
        /// </summary>
        /// <param name="serviceProvider"></param>
        public FindUserInfoServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returned service that allows you to get the authorization provider user by its email
        /// </summary>
        /// <param name="throwExceptionIfNull">If no service found and this is true, then throw an exception</param>
        /// <param name="callingMethod">This contains the name of the calling method</param>
        /// <returns>The service, or null </returns>
        public IFindUserInfoService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            var service = (IFindUserInfoService)_serviceProvider.GetService(typeof(IFindUserInfoService));
            if (service == null && throwExceptionIfNull)
                throw new AuthPermissionsException(
                    $"A service (method {callingMethod}) needed the {nameof(IFindUserInfoService)} service, but you haven't registered it." +
                    $"You can do this using the {nameof(RegisterExtensions.RegisterAuthenticationProviderReader)} configuration method.");

            return service;
        }
    }
}