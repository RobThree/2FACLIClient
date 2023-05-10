namespace TwoFA.Encryption;

internal static class StreamExtensions
{
    /// <summary>
    /// Reads a number of bytes from the stream. The number of bytes is determined by a prefixed int32 value.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>A <see cref="Span{byte}"/> read from the stream.</returns>
    public static Span<byte> ReadLengthEncodedBytes(this Stream stream)
        => stream.ReadBytes(stream.ReadInt());

    /// <summary>
    /// Writes a number of bytes to the stream. The total length is written as an int32, followed by the actual value.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    public static void WriteLengthEncodedBytes(this Stream stream, Span<byte> value)
    {
        stream.Write(BitConverter.GetBytes(value.Length));
        stream.Write(value);
    }

    /// <summary>
    /// Reads the next 4 bytes in the stream and returns the integer value represented by the 4 bytes.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The integer value represented by the 4 bytes.</returns>
    public static int ReadInt(this Stream stream)
    {
        var buffer = new Span<byte>(new byte[4]);
        stream.ReadExactly(buffer);
        return BitConverter.ToInt32(buffer);
    }

    /// <summary>
    /// Reads the specified number of bytes from the stream and returns those bytes as a <see cref="Span{byte}"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="len">The number of butes to read.</param>
    /// <returns>The requested number of bytes from the stream.</returns>
    public static Span<byte> ReadBytes(this Stream stream, int len)
    {
        var buffer = new Span<byte>(new byte[len]);
        stream.ReadExactly(buffer);
        return buffer;
    }
}