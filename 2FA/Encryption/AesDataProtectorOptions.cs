using System.Security.Cryptography;

namespace TwoFA.Encryption;

internal record AesDataProtectorOptions
{
    public string HashAlgorithm { get; init; } = "SHA256";
    public int Iterations { get; init; } = 500000;
    public CipherMode CipherMode { get; init; } = CipherMode.CBC;
    public int SaltLength { get; init; } = 16;
    public int KeyLength { get; init; } = 32;

    public HashAlgorithmName HashAlgorithmName
        => HashAlgorithm switch
        {
            "MD5" => HashAlgorithmName.MD5,
            "SHA1" => HashAlgorithmName.SHA1,
            "SHA256" => HashAlgorithmName.SHA256,
            "SHA384" => HashAlgorithmName.SHA384,
            "SHA512" => HashAlgorithmName.SHA512,
            _ => throw new NotSupportedException($"Hashalgorithm {HashAlgorithm} not supported")
        };
};
