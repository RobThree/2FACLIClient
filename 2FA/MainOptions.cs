namespace TwoFA;

internal record MainOptions
{
    public string? VaultFile { get; init; } = "vault.dat";
}