// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public class RegisterData
    {
        public RegisterData(IServiceCollection callerServices, AuthPermissionsOptions options)
        {
            CallerServices = callerServices;
            Options = options;
        }

        public IServiceCollection CallerServices { get; }

        public AuthPermissionsOptions Options { get; }
    }
}