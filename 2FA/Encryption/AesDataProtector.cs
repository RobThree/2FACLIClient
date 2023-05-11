using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TwoFA.ResourceFiles;

namespace TwoFA.Encryption;

internal class AesDataProtector : IDataProtector
{
    private readonly AesDataProtectorOptions _options;
    private const int _saltlen = 16;
    private const int _derivedkeylength = 32;
    private static readonly byte[] _magicheader = "totp\x01"u8.ToArray();    // Magic header ("totp" + version: 1)

    public AesDataProtector(IOptions<AesDataProtectorOptions> options)
        => _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public void SaveEncrypted(string path, string data, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_saltlen);    // Generate random salt

        using var aes = CreateAES(GetPasswordDerivedBytes(password, salt), null);

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
            throw new InvalidOperationException(Translations.EX_VAULT_FILE_INVALID);
        }
        // Check version
        var version = header[_magicheader.Length - 1];
        var expectedversion = 1;
        if (version != expectedversion)
        {
            throw new NotSupportedException(string.Format(Translations.EX_INCOMPATIBLE_VAULT_VERSION, version, expectedversion));
        }

        var iv = fs.ReadLengthEncodedBytes().ToArray();         // Read IV
        var salt = fs.ReadLengthEncodedBytes().ToArray();       // Read salt

        using var aes = CreateAES(GetPasswordDerivedBytes(password, salt), iv);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(fs, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);
        try
        {
            return streamReader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new CryptographicException(Translations.EX_VAULT_DECRYPTION_FAILED, ex);
        }
    }

    private Aes CreateAES(byte[] key, byte[]? iv)
    {
        var aes = Aes.Create();
        aes.Mode = _options.CipherMode;
        aes.Key = key;
        aes.IV = iv ?? aes.IV;  // When no IV specified, use default IV

        return aes;
    }

    private byte[] GetPasswordDerivedBytes(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(Encoding.Default.GetBytes(password), salt, _options.Iterations, _options.HashAlgorithmName, _derivedkeylength);
}