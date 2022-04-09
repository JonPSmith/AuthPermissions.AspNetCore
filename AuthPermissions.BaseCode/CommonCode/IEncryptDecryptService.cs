// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.CommonCode;

public interface IEncryptDecryptService
{
    /// <summary>
    /// This encrypts a string using the Aes encryption with the key provided
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    string Encrypt(string text);

    /// <summary>
    /// This decrypts a string using the Aes encryption with the key provided
    /// </summary>
    /// <param name="encrypted"></param>
    /// <returns></returns>
    string Decrypt(string encrypted);
}