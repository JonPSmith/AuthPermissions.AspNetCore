// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Test.StubClasses;

namespace Test.TestHelpers;

public static class DefaultLocalizerHelpers 
{

    public static IAuthPDefaultLocalizer SetupAuthPLoggingLocalizer(this string cultureOfMessage, Type resourceType = null)
    {
        return new TestAuthPDefaultLocalizer( 
            new StubDefaultLocalizerWithLogging(cultureOfMessage, resourceType ?? typeof(DefaultLocalizerHelpers)));
    }
}

public class TestAuthPDefaultLocalizer : IAuthPDefaultLocalizer
{
    public TestAuthPDefaultLocalizer(IDefaultLocalizer defaultLocalizer)
    {
        DefaultLocalizer = defaultLocalizer;
    }

    /// <summary>
    /// Correct <see cref="IDefaultLocalizer"/> service for the AuthP to use on localized code.
    /// </summary>
    public IDefaultLocalizer DefaultLocalizer { get; }
}