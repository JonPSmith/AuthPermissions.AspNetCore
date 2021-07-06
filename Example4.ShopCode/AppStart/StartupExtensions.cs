// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NetCore.AutoRegisterDi;

namespace Example4.ShopCode.AppStart
{
    public static class StartupExtensions
    {
        public static void ServiceLayerRegister(this IServiceCollection services)
        {
            //This registers the classes in the current assembly that end in "Service" and have a public interface
            services.RegisterAssemblyPublicNonGenericClasses(Assembly.GetExecutingAssembly())
                .AsPublicImplementedInterfaces();
        }
    }
}