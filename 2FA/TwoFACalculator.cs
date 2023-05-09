using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TwoFA;

public partial class TwoFACalculator
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
        public const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        [GeneratedRegex($"[^{Base32Alphabet}]", RegexOptions.Compiled)]
        private static partial Regex _b32re();
        private static readonly Dictionary<char, byte> _base32lookup = Base32Alphabet.Select((c, i) => new { c, i }).ToDictionary(v => v.c, v => (byte)v.i);

        public static byte[] Decode(string value)
        {
            // Have anything to decode?
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Remove padding
            value = value.TrimEnd('=');

            // Quick-exit if nothing to decode
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<byte>();
            }

            // Make sure string contains only chars from Base32 "alphabet"
            if (_b32re().IsMatch(value))
            {
                throw new ArgumentException("Invalid base32 string", nameof(value));
            }

            // Decode Base32 value (not world's most efficient or beatiful code but it gets the job done.
            var bits = string.Concat(value.Select(c => Convert.ToString(_base32lookup[c], 2).PadLeft(5, '0')));
            return Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        }
    }
}