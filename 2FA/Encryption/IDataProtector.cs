namespace TwoFA.Encryption;

public interface IDataProtector
{
    void EncryptFile(string path, string password);
    string DecryptFile(string path, string password);
}
