using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using TwoFA.ResourceFiles;

namespace TwoFA;

internal partial class TwoFACalculator
{
    private readonly TwoFACalculatorOptions _options;

    public TwoFACalculator(IOptions<TwoFACalculatorOptions> options)
        => _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public string GetCode(string secret)
        => GetCode(secret, DateTimeOffset.UtcNow);

    public string GetCode(string secret, DateTimeOffset dateTime)
    {
        var algoname = "HMAC" + (Enum.GetName(_options.Algorithm) ?? throw new NotSupportedException(string.Format(Translations.EX_HASHALGORITHM_NOT_SUPPORTED, _options.Algorithm)));
        using var algo = (KeyedHashAlgorithm)(CryptoConfig.CreateFromName(algoname) ?? throw new InvalidOperationException(string.Format(Translations.EX_FAILED_TO_INITIALIZE_HASHALGO, algoname)));
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

            // Remove padding and uppercase the string (apparently there are cases where a lowercase string is stored
            // somehow? See https://github.com/RobThree/2FACLIClient/issues/2)
            value = value.TrimEnd('=').ToUpperInvariant();

            // Decode Base32 value (not world's most efficient or beautiful code but it gets the job done.
            var bits = string.Concat(value.Select(c => Convert.ToString(_base32lookup[c], 2).PadLeft(5, '0')));
            return Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        }
    }
}