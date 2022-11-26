// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using LocalizeMessagesAndErrors;
using Test.StubClasses;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestStubLocalizeWithDefault
{

    [Fact]
    public void TestAddErrorString()
    {
        //SETUP
        var stubLocalizer = new StubLocalizeWithDefault<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.AddErrorString("test".MethodLocalizeKey(this), "An Error");

        //VERIFY
        status.Errors.Single().ToString().ShouldEqual("An Error");
    }

    [Fact]
    public void TestSetMessageString()
    {
        //SETUP
        var stubLocalizer = new StubLocalizeWithDefault<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.SetMessageString("test".MethodLocalizeKey(this), "My status Message");

        //VERIFY
        status.Message.ShouldEqual("My status Message");
    }


    [Fact]
    public void TestSetMessageFormatted()
    {
        //SETUP
        var stubLocalizer = new StubLocalizeWithDefault<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.SetMessageFormatted("test".MethodLocalizeKey(this), $"My status Message");

        //VERIFY
        status.Message.ShouldEqual("My status Message");
    }
}