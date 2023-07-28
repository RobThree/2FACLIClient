using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using TwoFA.Encryption;

namespace TwoFA.Tests;

[TestClass]
public class AesDataProtectorTests
{
    private static readonly string _testvalue = "Hello world!";
    private static readonly string _testpassword = "Sup3rS3cr3t!^";

    [TestMethod]
    public async Task EncryptDecrypt_Defaults()
    {
        var target = new AesDataProtector(Options.Create(new AesDataProtectorOptions
        {
            // Default values
            HashAlgorithm = "SHA256",
            Iterations = 500000,
            CipherMode = CipherMode.CBC,
            SaltLength = 16,
            KeyLength = 32
        }));

        var testfile = nameof(EncryptDecrypt_Defaults);

        // Encrypt and save value
        await target.SaveEncryptedAsync(testfile, _testvalue, _testpassword).ConfigureAwait(false);
        // Load and decrypt value
        var result = await target.LoadEncryptedAsync(testfile, _testpassword).ConfigureAwait(false);

        Assert.AreEqual(_testvalue, result);
    }

    [TestMethod]
    public async Task EncryptDecrypt_NonDefaults()
    {
        var target = new AesDataProtector(Options.Create(new AesDataProtectorOptions
        {
            // Default values
            HashAlgorithm = "SHA384",
            Iterations = 1234567,
            CipherMode = CipherMode.CFB,
            SaltLength = 32,
            KeyLength = 16
        }));

        var testfile = nameof(EncryptDecrypt_NonDefaults);

        // Encrypt and save value
        await target.SaveEncryptedAsync(testfile, _testvalue, _testpassword).ConfigureAwait(false);
        // Load and decrypt value
        var result = await target.LoadEncryptedAsync(testfile, _testpassword).ConfigureAwait(false);

        Assert.AreEqual(_testvalue, result);
    }
}