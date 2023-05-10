﻿using System.Security.Cryptography;
using System.Text;

namespace TwoFA.Encryption;

public class AesDataProtector : IDataProtector
{
    private readonly HashAlgorithmName _hashalgorithmname;
    private readonly int _iterations;
    private const int _defaultiterations = 500000;
    private const int _saltlen = 16;
    private const int _derivedkeylength = 32;
    private static readonly byte[] _magicheader = "totp\x01"u8.ToArray();    // Magic header ("totp" + version: 1)

    public AesDataProtector(int iterations = _defaultiterations)
        : this(HashAlgorithmName.SHA256, iterations) { }

    public AesDataProtector(HashAlgorithmName hashAlgorithmName, int iterations = _defaultiterations)
    {
        _iterations = iterations;
        _hashalgorithmname = hashAlgorithmName;
    }

    public void SaveEncrypted(string path, string data, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_saltlen);    // Generate random salt

        using var aes = Aes.Create();
        aes.Key = GetPasswordDerivedBytes(password, salt);      // Generate key based on password with salt

        using var fs = File.Create(path);
        fs.Write(_magicheader);                                 // Write magic header
        fs.WriteLengthEncodedBytes(aes.IV);                     // Write IV
        fs.WriteLengthEncodedBytes(salt);                       // Write salt

        // Write the rest of the file as the AES encrypted version of the json file
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(cryptoStream);
        streamWriter.Write(data);
    }

    public string LoadEncrypted(string path, string password)
    {
        using var fs = File.OpenRead(path);

        // Check magic header
        var header = fs.ReadBytes(_magicheader.Length);
        if (!_magicheader.SequenceEqual(header.ToArray()))
        {
            throw new InvalidOperationException("Secrets file does not appear to be valid");
        }
        var version = header[_magicheader.Length - 1];
        var expectedversion = 1;
        if (version != expectedversion)
        {
            throw new InvalidOperationException($"Incompatible vault version {version}, expected {expectedversion}");
        }

        var iv = fs.ReadLengthEncodedBytes().ToArray();         // Read IV
        var salt = fs.ReadLengthEncodedBytes().ToArray();       // Read salt

        using var aes = Aes.Create();
        aes.Key = GetPasswordDerivedBytes(password, salt);      // Generate key based on password with salt
        aes.IV = iv;                                            // Set IV

        // The rest of the file is the AES encrypted contents of the json file
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(fs, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);
        try
        {
            return streamReader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to decrypt file", ex);
        }
    }

    private byte[] GetPasswordDerivedBytes(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(Encoding.Default.GetBytes(password), salt, _iterations, _hashalgorithmname, _derivedkeylength);
}
