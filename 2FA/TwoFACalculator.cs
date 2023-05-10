using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace TwoFA;

internal partial class TwoFACalculator
{
    private readonly TwoFAOptions _options;

    public TwoFACalculator(IOptions<TwoFAOptions> options)
        => _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public string GetCode(string secret)
        => GetCode(secret, DateTimeOffset.UtcNow);

    public string GetCode(string secret, DateTimeOffset dateTime)
    {
        using var algo = (KeyedHashAlgorithm)(CryptoConfig.CreateFromName("HMAC" + (Enum.GetName(_options.Algorithm) ?? throw new InvalidOperationException())) ?? throw new InvalidOperationException());
        algo.Key = Base32.Decode(secret);
        var ts = BitConverter.GetBytes(GetTimeSlice(dateTime.ToUnixTimeSeconds(), 0, (int)_options.Period.TotalSeconds));
        var hashhmac = algo.ComputeHash(new byte[] { 0, 0, 0, 0, ts[3], ts[2], ts[1], ts[0] });
        var offset = hashhmac[^1] & 0x0F;
        return $@"{((
            (hashhmac[offset + 0] << 24) |
            (hashhmac[offset + 1] << 16) |
            (hashhmac[offset + 2] << 8) |
            hashhmac[offset + 3]
        ) & 0x7FFFFFFF) % (long)Math.Pow(10, _options.Digits)}".PadLeft(_options.Digits, '0');
    }

    private static long GetTimeSlice(long timestamp, int offset, int period) => (timestamp / period) + (offset * period);

    internal static partial class Base32
    {
        private const string _base32alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private static readonly Dictionary<char, byte> _base32lookup = _base32alphabet.Select((c, i) => new { c, i }).ToDictionary(v => v.c, v => (byte)v.i);

        public static byte[] Decode(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // Remove padding
            value = value.TrimEnd('=');

            // Decode Base32 value (not world's most efficient or beatiful code but it gets the job done.
            var bits = string.Concat(value.Select(c => Convert.ToString(_base32lookup[c], 2).PadLeft(5, '0')));
            return Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        }
    }
}