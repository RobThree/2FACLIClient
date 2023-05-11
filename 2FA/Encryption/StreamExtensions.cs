namespace TwoFA.Encryption;

internal static class StreamExtensions
{
    /// <summary>
    /// Reads a number of bytes from the stream. The number of bytes is determined by a prefixed int32 value.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>A <see cref="byte[]"/> read from the stream.</returns>
    public static async ValueTask<byte[]> ReadLengthEncodedBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
        => await stream.ReadBytesAsync(await stream.ReadIntAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Writes a number of bytes to the stream. The total length is written as an int32, followed by the actual value.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    public static async ValueTask WriteLengthEncodedBytesAsync(this Stream stream, byte[] value, CancellationToken cancellationToken = default)
    {
        var buffer = BitConverter.GetBytes(value.Length);
        await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the next 4 bytes in the stream and returns the integer value represented by the 4 bytes.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The integer value represented by the 4 bytes.</returns>
    public static async ValueTask<int> ReadIntAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4];
        await stream.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken);
        return BitConverter.ToInt32(buffer);
    }

    /// <summary>
    /// Reads the specified number of bytes from the stream and returns those bytes as a <see cref="Span{byte}"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="len">The number of butes to read.</param>
    /// <returns>The requested number of bytes from the stream.</returns>
    public static async ValueTask<byte[]> ReadBytesAsync(this Stream stream, int len, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[len];
        await stream.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        return buffer;
    }
}