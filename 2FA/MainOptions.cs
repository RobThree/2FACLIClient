namespace TwoFA;

internal record MainOptions
{
    public string VaultFile { get; init; } = "vault.dat";
    public string Locale { get; init; } = "en-US";
}