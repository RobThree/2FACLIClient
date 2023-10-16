using Refit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using TwoFA.ResourceFiles;

namespace TwoFA;

internal class LastPassMFABackupDownloader
{
    public const string DEFAULTBASEADDRESS = "https://lastpass.com";
    private readonly string _hosturl;

    public LastPassMFABackupDownloader()
        : this(DEFAULTBASEADDRESS) { }

    public LastPassMFABackupDownloader(string hostUrl)
        => _hosturl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));

    public async Task<string> DownloadAsync(string username, string password, string? otp, CancellationToken cancellationToken = default)
    {
        var client = RestService.For<ILastPassClient>(_hosturl);

        var iterations = await ExecuteStep(
            Translations.STATUS_RETRIEVING_ITERATIONS,
            () => client.GetIterations(username, cancellationToken),
            (e) => new LastPassMFABackupDownloaderException(Translations.EX_FAILED_TO_RETRIEVE_ITERATIONS, e)
        ).ConfigureAwait(false);

        var login = CreateHash(username, password, iterations);

        var loginresult = ParseLoginResult(await ExecuteStep(
            Translations.STATUS_LOGGING_IN,
            () => client.Login(new LoginRequest(username, login.Hash, iterations, otp), cancellationToken),
            (e) => new LastPassMFABackupDownloaderException(Translations.EX_LOGIN_FAILED, e)
        ).ConfigureAwait(false));

        var mfadata = await ExecuteStep(
            Translations.STATUS_DOWNLOADING_MFA_BACKUP,
            () => client.GetMFABackupAsync(loginresult.Token, loginresult.SessionId, cancellationToken),
            (e) => new LastPassMFABackupDownloaderException(Translations.EX_MFA_BACKUP_DOWNLOAD_FAILED, e)
        ).ConfigureAwait(false);

        return await ExecuteStep(
            Translations.STATUS_DECRYPTING_MFA_BACKUP,
            () => DecryptMFABackupAsync(mfadata, login.Key),
            (e) => new LastPassMFABackupDownloaderException(Translations.EX_DECRYPT_MFA_BACKUP_FAILED, e)
        ).ConfigureAwait(false);
    }

    // Displays a status and then executes a given task, wrapped in a try/catch statement
    private static async Task<T> ExecuteStep<T>(string status, Func<Task<T>> valueFactory, Func<Exception, Exception> exceptionFactory)
    {
        Console.WriteLine(status);
        try { return await valueFactory().ConfigureAwait(false); } catch (Exception ex) { throw exceptionFactory(ex); }
    }

    private static async Task<string> DecryptMFABackupAsync(MFAData mfaData, byte[] key, CancellationToken cancellationToken = default)
    {
        var dataparts = mfaData.UserData.Split('|');
        var iv = Convert.FromBase64String(dataparts[0].Split('!')[1]);
        var ciphertext = Convert.FromBase64String(dataparts[1]);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(ciphertext);
        using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);
        return await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Login CreateHash(string username, string password, int iterations)
    {
        var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.Default.GetBytes(password), Encoding.Default.GetBytes(username), iterations, HashAlgorithmName.SHA256, 32);
        var loginhash = Rfc2898DeriveBytes.Pbkdf2(key, Encoding.Default.GetBytes(password), 1, HashAlgorithmName.SHA256, 32);
        return new(key, Convert.ToHexString(loginhash).ToLowerInvariant());
    }

    private static LoginResult ParseLoginResult(string result)
    {
        try
        {   // Try OK response?
            return ParseXML<LoginResult>(result);
        }
        catch (InvalidOperationException)
        {   // Try "error response"?
            var response = ParseXML<ErrorResponse>(result);
            throw new LastPassLoginException(response?.Error?.Message ?? string.Empty);
        }
    }

    private static T ParseXML<T>(string value)
    {
        using var reader = new StringReader(value);
        var serializer = new XmlSerializer(typeof(T));
        return (T)(serializer.Deserialize(reader) ?? throw new InvalidOperationException());
    }

    internal record MFAData(
        [property: JsonPropertyName("userData")] string UserData,
        [property: JsonPropertyName("userId")] int UserId
    );

    private record Login(byte[] Key, string Hash);

    internal record LoginRequest(
        [property: AliasAs("username")] string Username,
        [property: AliasAs("hash")] string Hash,
        [property: AliasAs("iterations")] int Iterations,
        [property: AliasAs("otp")] string? OTP
    )
    {
#pragma warning disable CA1822 // Mark members as static
        [AliasAs("method")] public string Method => "mobile";
        [AliasAs("web")] public int Web => 1;
        [AliasAs("xml")] public int Xml => 1;
        [AliasAs("imei")] public string IMEI => "LastPassAuthExport";
#pragma warning restore CA1822 // Mark members as static
    }

    internal interface ILastPassClient
    {
        [Get("/iterations.php")]
        internal Task<int> GetIterations(string email, CancellationToken cancellationToken = default);

        [Post("/login.php")]
        internal Task<string> Login([Body(BodySerializationMethod.UrlEncoded)] LoginRequest loginRequest, CancellationToken cancellationToken = default);

        [Get("/lmiapi/authenticator/backup")]
        internal Task<MFAData> GetMFABackupAsync([Header("X-CSRF-TOKEN")] string token, [Header("X-SESSION-ID")] string sessionid, CancellationToken cancellationToken = default);
    }
}

public class LastPassMFABackupDownloaderException : Exception
{
    public LastPassMFABackupDownloaderException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

public class LastPassLoginException : Exception
{
    public LastPassLoginException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

[XmlRoot("ok")]
public record LoginResult(  // Must be public for XML Serializer
    [property: XmlAttribute("token")] string Token,
    [property: XmlAttribute("sessionid")] string SessionId
)
{
    public LoginResult() : this(string.Empty, string.Empty) { }
}

[XmlRoot("response")]
public class ErrorResponse
{
    [XmlElement("error")]
    public required Error Error { get; init; }
}

public record Error(
    [property: XmlAttribute("message")] string Message,
    [property: XmlAttribute("cause")] string Cause,
    [property: XmlAttribute("email")] string Email,
    [property: XmlAttribute("security_email")] string SecurityEmail,
    [property: XmlAttribute("is_passwordless_login")] string IsPasswordlessLogin,
    [property: XmlAttribute("passwordless_device_id")] string PasswordlessDeviceId
)
{
    public Error() : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) { }
}