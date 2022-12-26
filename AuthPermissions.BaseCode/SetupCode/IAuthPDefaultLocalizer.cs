// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using LocalizeMessagesAndErrors;

namespace AuthPermissions.BaseCode.SetupCode;

/// <summary>
/// This provides the correct <see cref="IDefaultLocalizer"/> service for
/// any of AuthP's methods that support localizations.
/// This should be registered to the DI as a singleton 
/// </summary>
public interface IAuthPDefaultLocalizer
{
    /// <summary>
    /// Correct <see cref="IDefaultLocalizer"/> service for the AuthP to use on localized code.
    /// </summary>
    IDefaultLocalizer DefaultLocalizer { get; }
}