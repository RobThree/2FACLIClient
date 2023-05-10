namespace TwoFA.Encryption;

public interface IDataProtector
{
    void SaveEncrypted(string path, string data, string password);
    string LoadEncrypted(string path, string password);
}
