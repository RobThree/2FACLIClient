namespace TwoFA;

internal record TwoFAOptions
{
    public Algorithm Algorithm { get; init; } = Algorithm.SHA1;
    public int Digits { get; init; } = 6;
    public TimeSpan Period { get; init; } = TimeSpan.FromSeconds(30);
}
