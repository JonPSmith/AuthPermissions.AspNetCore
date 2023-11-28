// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;

namespace AuthPermissions.Factories
{

    /// <summary>
    /// Factory to cover the <see cref="ISyncAuthenticationUsers"/> service
    /// </summary>
    public class TenantChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Needs IServiceProvider
        /// </summary>
        /// <param name="serviceProvider"></param>
        public TenantChangeServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returned service that allows you to get the authorization provider user by its email
        /// </summary>
        /// <param name="throwExceptionIfNull">If no service found and this is true, then throw an exception</param>
        /// <param name="callingMethod">This contains the name of the calling method</param>
        /// <returns>The service, or null </returns>
        public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            var service = (ITenantChangeService)_serviceProvider.GetService(typeof(ITenantChangeService));
            if (service == null && throwExceptionIfNull)
                throw new AuthPermissionsException(
                    $"A service (method {callingMethod}) needed the {nameof(ITenantChangeService)} service, but you haven't registered it." +
                    $"You can do this using the {nameof(RegisterExtensions.RegisterTenantChangeService)} configuration method.");

            return service;
        }
    }
}