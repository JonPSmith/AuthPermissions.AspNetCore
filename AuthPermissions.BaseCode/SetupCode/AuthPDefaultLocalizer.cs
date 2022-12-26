// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using LocalizeMessagesAndErrors;
using Microsoft.Extensions.DependencyInjection;
namespace AuthPermissions.BaseCode.SetupCode;

/// <summary>
/// This provides the correct <see cref="IDefaultLocalizer"/> service for
/// any of AuthP's methods that support localizations.
/// This should be registered to the DI as a singleton 
/// </summary>
public class AuthPDefaultLocalizer : IAuthPDefaultLocalizer
{
    /// <summary>
    /// This sets up the <see cref="AuthPDefaultLocalizer"/> with the correct <see cref="IDefaultLocalizer"/> service
    /// </summary>
    /// <param name="serviceProvider"></param>
    public AuthPDefaultLocalizer(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<AuthPermissionsOptions>();
        var factory = serviceProvider.GetRequiredService<IDefaultLocalizerFactory>();

        DefaultLocalizer = factory.Create(options.InternalData.AuthPResourceType);
    }

    /// <summary>
    /// Correct <see cref="IDefaultLocalizer"/> service for the AuthP to use on localized code.
    /// </summary>
    public IDefaultLocalizer DefaultLocalizer { get; }
}