// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace AuthPermissions.CommonCode;

//thanks to https://csharpcode.org/blog/simple-encryption-and-decryption-in-c/
//Updated to take in an encryption key
/// <summary>
/// Class to Encrypt / Decrypt a string
/// </summary>
public class EncryptDecrypt
{
    private readonly byte[] _keyBytes;

    /// <summary>
    /// This encrypts a string using 
    /// </summary>
    /// <param name="keyText"></param>
    /// <exception cref="ArgumentException"></exception>
    public EncryptDecrypt(string keyText)
    {
        if (keyText.Length < 16)
            throw new ArgumentException("You must provide at least 16 characters for a key", nameof(keyText));

        _keyBytes = new byte[16];
        var skeyBytes = Encoding.UTF8.GetBytes(keyText);
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