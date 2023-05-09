namespace TwoFA.Encryption;

internal static class StreamExtensions
{
    public static Span<byte> ReadLengthEncodedBytes(this Stream stream)
        => stream.ReadBytes(stream.ReadInt());

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