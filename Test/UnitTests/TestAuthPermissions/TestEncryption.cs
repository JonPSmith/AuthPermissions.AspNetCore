// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions;

public class TestEncryption
{
    private readonly ITestOutputHelper _output;

    public TestEncryption(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestEncryptDecrypt()
    {
        //SETUP
        var authOptions = new AuthPermissionsOptions
        {
            EncryptionKey = "asfafffggdgerxbd"
        };

        var encyptor = new EncryptDecryptService(authOptions);
        var testString = "The cat on the ö and had a great Ō.";

        //ATTEMPT
        var encrypted = encyptor.Encrypt(testString);
        var decrypted = encyptor.Decrypt(encrypted);

        //VERIFY
        decrypted.ShouldEqual(testString);
        _output.WriteLine($"Original string length = {testString.Length}, encrypted string length = {encrypted.Length}, ratio = {encrypted.Length/testString.Length:P}");
    }
}