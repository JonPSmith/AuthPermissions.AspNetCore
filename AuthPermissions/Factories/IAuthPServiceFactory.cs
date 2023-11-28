// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace AuthPermissions.Factories
{
    /// <summary>
    /// Generic factory method to handle services that are (optionally) registered by the developer
    /// </summary>
    /// <typeparam name="TServiceInterface"></typeparam>
    public interface IAuthPServiceFactory<out TServiceInterface> where TServiceInterface : class
    {
        /// <summary>
        /// This returns the service registered to the <see type="TServiceInterface"/> interface
        /// </summary>
        /// <param name="throwExceptionIfNull">If no service found and this is true, then throw an exception</param>
        /// <param name="callingMethod">This contains the name of the calling method</param>
        /// <returns></returns>
        TServiceInterface GetService(bool throwExceptionIfNull = true, [CallerMemberName] string callingMethod = "" );
    }
}