// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ExamplesCommonCode.UsefulCode;

//thanks to https://csharpcode.org/blog/simple-encryption-and-decryption-in-c/
//Updated to take in an encryption key
public class EncryptDecrypt
{
    private readonly byte[] _keyBytes;

    public EncryptDecrypt(string keyText)
    {
        if (keyText.Length < 16)
            throw new ArgumentException("You must provide at least 16 characters for a key", nameof(keyText));

        _keyBytes = new byte[16];
        var skeyBytes = Encoding.UTF8.GetBytes(keyText);
        Array.Copy(skeyBytes, _keyBytes, Math.Min(_keyBytes.Length, skeyBytes.Length));
    }

    public string Encrypt(string text)
    {
        var b = Encoding.UTF8.GetBytes(text);
        var encrypted = GetAes().CreateEncryptor().TransformFinalBlock(b, 0, b.Length);
        return Convert.ToBase64String(encrypted);
    }

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