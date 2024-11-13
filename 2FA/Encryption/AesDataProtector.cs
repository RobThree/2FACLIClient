using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TwoFA.ResourceFiles;

namespace TwoFA.Encryption;

internal class AesDataProtector(IOptions<AesDataProtectorOptions> options) : IDataProtector
{
    private readonly AesDataProtectorOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private const int _saltlen = 16;
    private const int _derivedkeylength = 32;
    private static readonly byte[] _magicheader = "totp"u8.ToArray();    // Magic header
    private const byte _version = 1;

    public async Task SaveEncryptedAsync(string path, string data, string password, CancellationToken cancellationToken = default)
    {
        var salt = RandomNumberGenerator.GetBytes(_saltlen);    // Generate random salt

        using var aes = CreateAES(GetPasswordDerivedBytes(password, salt), null);

        using var fs = File.Create(path);
        await fs.WriteAsync(_magicheader, cancellationToken).ConfigureAwait(false);             // Write magic header
        await fs.WriteByteAsync(_version, cancellationToken).ConfigureAwait(false);             // Write vault version
        await fs.WriteLengthEncodedBytesAsync(aes.IV, cancellationToken).ConfigureAwait(false); // Write IV
        await fs.WriteLengthEncodedBytesAsync(salt, cancellationToken).ConfigureAwait(false);   // Write salt

        // Write the rest of the file as the AES encrypted version of the json file
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(cryptoStream);
        await streamWriter.WriteAsync(data).ConfigureAwait(false);                              // Write data
    }

    public async Task<string> LoadEncryptedAsync(string path, string password, CancellationToken cancellationToken = default)
    {
        using var fs = File.OpenRead(path);

        // Check magic header
        var header = await fs.ReadBytesAsync(_magicheader.Length, cancellationToken).ConfigureAwait(false);     // Read magic header
        if (!_magicheader.SequenceEqual([.. header]))
        {
            throw new InvalidOperationException(Translations.EX_VAULT_FILE_INVALID);
        }
        // Check version
        var version = await fs.ReadByteAsync(cancellationToken).ConfigureAwait(false);                          // Read vault version
        if (version != _version)
        {
            throw new NotSupportedException(string.Format(Translations.EX_INCOMPATIBLE_VAULT_VERSION, version, _version));
        }

        var iv = (await fs.ReadLengthEncodedBytesAsync(cancellationToken).ConfigureAwait(false)).ToArray();     // Read IV
        var salt = (await fs.ReadLengthEncodedBytesAsync(cancellationToken).ConfigureAwait(false)).ToArray();   // Read salt

        using var aes = CreateAES(GetPasswordDerivedBytes(password, salt), iv);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(fs, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);
        try
        {
            return await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);                  // Read data
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