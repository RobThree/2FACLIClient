namespace TwoFA.Encryption;

internal interface IDataProtector
{
    Task SaveEncryptedAsync(string path, string data, string password, CancellationToken cancellationToken = default);
    Task<string> LoadEncryptedAsync(string path, string password, CancellationToken cancellationToken = default);
}
