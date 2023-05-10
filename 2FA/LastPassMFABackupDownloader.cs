using Refit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace TwoFA;

public class LastPassMFABackupDownloader
{
    private readonly string _hosturl;

    public LastPassMFABackupDownloader()
        : this("https://lastpass.com") { }

    public LastPassMFABackupDownloader(string hostUrl)
        => _hosturl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));

    public async Task<string> DownloadAsync(string username, string password, string? otp)
    {
        try
        {
            var client = RestService.For<ILastPass>(_hosturl);
            var iterations = await client.GetIterations(username).ConfigureAwait(false);
            var login = CreateHash(username, password, iterations);
            var loginresult = ParseLoginResult(await client.Login(new LoginRequest(
                username,
                login.Hash,
                iterations,
                otp
            )));
            var mfadata = await client.GetMFABackup(loginresult.Token, loginresult.SessionId).ConfigureAwait(false);
            return DecodeMFA(mfadata, login.Key);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download MFA Vault ({ex.Message})");
        }
    }

    private static string DecodeMFA(MFAData mfaData, byte[] key)
    {
        var dataparts = mfaData.UserData.Split('|');
        var iv = Convert.FromBase64String(dataparts[0].Split('!')[1]);
        var ciphertext = Convert.FromBase64String(dataparts[1]);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(ciphertext);
        using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);
        return streamReader.ReadToEnd();
    }

    private static Login CreateHash(string username, string password, int iterations)
    {
        var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.Default.GetBytes(password), Encoding.Default.GetBytes(username), iterations, HashAlgorithmName.SHA256, 32);
        var loginhash = Rfc2898DeriveBytes.Pbkdf2(key, Encoding.Default.GetBytes(password), 1, HashAlgorithmName.SHA256, 32);
        return new(key, Convert.ToHexString(loginhash).ToLowerInvariant());
    }

    private static LoginResult ParseLoginResult(string result)
    {
        var serializer = new XmlSerializer(typeof(LoginResult));
        using var reader = new StringReader(result);
        return (LoginResult)serializer.Deserialize(reader)!;
    }

    internal record MFAData(
        [property: JsonPropertyName("userData")] string UserData,
        [property: JsonPropertyName("userId")] int UserId
    );

    [XmlRoot("ok")]
    public record LoginResult(  // Must be public for XML Serializer
        [property: XmlAttribute("token")] string Token,
        [property: XmlAttribute("sessionid")] string SessionId
    )
    {
        public LoginResult() : this(string.Empty, string.Empty) { }
    }

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

    internal interface ILastPass
    {
        [Get("/iterations.php")]
        internal Task<int> GetIterations(string email);

        [Post("/login.php")]
        internal Task<string> Login([Body(BodySerializationMethod.UrlEncoded)] LoginRequest loginRequest);

        [Get("/lmiapi/authenticator/backup")]
        internal Task<MFAData> GetMFABackup([Header("X-CSRF-TOKEN")] string token, [Header("X-SESSION-ID")] string sessionid);
    }
}