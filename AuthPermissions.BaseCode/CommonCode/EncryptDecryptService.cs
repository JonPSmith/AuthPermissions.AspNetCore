// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Cryptography;
using System.Text;

namespace AuthPermissions.BaseCode.CommonCode;

//thanks to https://csharpcode.org/blog/simple-encryption-and-decryption-in-c/
//Updated to take in an encryption key via AuhP's options
/// <summary>
/// Class to Encrypt / Decrypt a string
/// </summary>
public class EncryptDecryptService : IEncryptDecryptService
{
    private readonly byte[] _keyBytes;

    /// <summary>
    /// This provides an AES Encrypt / Decrypt of a string
    /// </summary>
    /// <param name="options"></param>
    /// <exception cref="ArgumentException"></exception>
    public EncryptDecryptService(AuthPermissionsOptions options)
    {
        if (string.IsNullOrEmpty(options.EncryptionKey))
            throw new AuthPermissionsBadDataException(
                $"You must provide an EncryptionKey via the options's {nameof(AuthPermissionsOptions.EncryptionKey)} parameter.");

        if (options.EncryptionKey.Length < 16)
            throw new AuthPermissionsBadDataException(
                $"The options's {nameof(AuthPermissionsOptions.EncryptionKey)} string must be 16 or more in length.");

        _keyBytes = new byte[16];
        var skeyBytes = Encoding.UTF8.GetBytes(options.EncryptionKey);
        Array.Copy(skeyBytes, _keyBytes, Math.Min(_keyBytes.Length, skeyBytes.Length));
    }

    /// <summary>
    /// This encrypts a string using the Aes encryption with the key provided
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public string Encrypt(string text)
    {
        var b = Encoding.UTF8.GetBytes(text);
        var encrypted = GetAes().CreateEncryptor().TransformFinalBlock(b, 0, b.Length);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// This decrypts a string using the Aes encryption with the key provided
    /// </summary>
    /// <param name="encrypted"></param>
    /// <returns></returns>
    public string Decrypt(string encrypted)
    {
        var b = Convert.FromBase64String(encrypted);
        var decrypted = GetAes().CreateDecryptor().TransformFinalBlock(b, 0, b.Length);
        return Encoding.UTF8.GetString(decrypted);
    }

    private Aes GetAes()
    {
        Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 128;
        aes.Key = _keyBytes;
        aes.IV = _keyBytes;

        return aes;
    }
}