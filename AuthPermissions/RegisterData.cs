// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public class RegisterData
    {
        public RegisterData(IServiceCollection services, AuthPermissionsOptions options)
        {
            Services = services;
            Options = options;
        }

        public IServiceCollection Services { get; }

        public AuthPermissionsOptions Options { get; }
    }
}