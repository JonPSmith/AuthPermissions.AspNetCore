// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AdminCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.CommonCode;

namespace AuthPermissions.SetupCode.Factories
{
    public interface IFindUserInfoServiceFactory
    {
        /// <summary>
        /// This factory returns a service that implements the <see cref="IFindUserInfoService"/> interface.
        /// This service is used by the service <see cref="BulkLoadUsersService"/> 
        /// </summary>
        /// <returns></returns>
        IFindUserInfoService GetOptionalService();
    }

    /// <summary>
    /// Factory to cover the <see cref="ISyncAuthenticationUsers"/>, which is optional
    /// </summary>
    public class FindUserInfoServiceFactory : IFindUserInfoServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public FindUserInfoServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Only call this if you need a service that implements the <see cref="IFindUserInfoService"/> interface.
        /// </summary>
        /// <returns>The returned service allows you to get the authorization provider user by its email</returns>
        public IFindUserInfoService GetOptionalService()
        {
            var result = (IFindUserInfoService) _serviceProvider.GetService(typeof(IFindUserInfoService));
            if (result == null)
                throw new AuthPermissionsException(
                    $"A service needed the {nameof(IFindUserInfoService)} service, but you haven't registered it." +
                    $"You can do this using the {nameof(RegisterExtensions.RegisterAuthenticationProviderReader)} configuration method.");

            return result;
        }
    }
}