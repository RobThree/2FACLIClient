namespace TwoFA.Encryption;

internal interface IDataProtector
{
    void SaveEncrypted(string path, string data, string password);
    string LoadEncrypted(string path, string password);
}
