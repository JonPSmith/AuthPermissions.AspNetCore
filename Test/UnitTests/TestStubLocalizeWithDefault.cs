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
        var stubLocalizer = new StubLocalizeDefaultWithLogging<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.AddErrorString("test".MethodLocalizeKey(this), "An Error");

        //VERIFY
        status.Errors.Single().ToString().ShouldEqual("An Error");
        stubLocalizer.SameKeyButDiffFormat.ShouldEqual(false);
    }

    [Fact]
    public void TestSetMessageString()
    {
        //SETUP
        var stubLocalizer = new StubLocalizeDefaultWithLogging<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.SetMessageString("test".MethodLocalizeKey(this), "Status Message1");

        //VERIFY
        status.Message.ShouldEqual("Status Message1");
        stubLocalizer.SameKeyButDiffFormat.ShouldEqual(false);
    }


    [Fact]
    public void TestSetMessageFormatted()
    {
        //SETUP
        var stubLocalizer = new StubLocalizeDefaultWithLogging<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.SetMessageFormatted("test".MethodLocalizeKey(this), $"Status Message{2}");

        //VERIFY
        status.Message.ShouldEqual("Status Message2");
        stubLocalizer.SameKeyButDiffFormat.ShouldEqual(false);
    }

    [Fact]
    public void TestSetMessage_SameKeyButDiffFormat()
    {
        //SETUP
        var stubLocalizer = new StubLocalizeDefaultWithLogging<LocalizeResources>();

        //ATTEMPT
        var status = new StatusGenericLocalizer<LocalizeResources>("en", stubLocalizer);
        status.AddErrorString("test".MethodLocalizeKey(this), "First Error message");
        status.AddErrorString("test".MethodLocalizeKey(this), "Second Error message");

        //VERIFY
        stubLocalizer.SameKeyButDiffFormat.ShouldEqual(true);
    }
}