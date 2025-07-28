using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DbfSharp.Core.Utils;

/// <summary>
/// Extension methods for BinaryReader to improve performance and provide additional functionality
/// </summary>
public static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads a structure from the binary reader using safe marshaling
    /// </summary>
    /// <typeparam name="T">The structure type to read</typeparam>
    /// <param name="reader">The binary reader</param>
    /// <returns>The read structure</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null</exception>
    /// <exception cref="EndOfStreamException">Thrown when not enough data is available</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadStruct<T>(this BinaryReader reader)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(reader);

        var size = Marshal.SizeOf<T>();
        var bytes = reader.ReadBytes(size);

        if (bytes.Length != size)
        {
            throw new EndOfStreamException(
                $"Expected {size} bytes for {typeof(T).Name}, got {bytes.Length}"
            );
        }

        // Use safe marshaling instead of unsafe code
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Reads multiple structures from the binary reader
    /// </summary>
    /// <typeparam name="T">The structure type to read</typeparam>
    /// <param name="reader">The binary reader</param>
    /// <param name="count">The number of structures to read</param>
    /// <returns>An array of structures</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadStructArray<T>(this BinaryReader reader, int count)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var result = new T[count];
        var size = Marshal.SizeOf<T>();
        var totalBytes = size * count;

        var bytes = reader.ReadBytes(totalBytes);
        if (bytes.Length != totalBytes)
        {
            throw new EndOfStreamException(
                $"Expected {totalBytes} bytes for {count} {typeof(T).Name} structures, got {bytes.Length}"
            );
        }

        // Use safe marshaling for each structure
        for (var i = 0; i < count; i++)
        {
            var offset = i * size;
            var structBytes = new byte[size];
            Array.Copy(bytes, offset, structBytes, 0, size);

            var handle = GCHandle.Alloc(structBytes, GCHandleType.Pinned);
            try
            {
                result[i] = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        return result;
    }

    /// <summary>
    /// Reads a null-terminated string with a maximum length
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="maxLength">The maximum length to read</param>
    /// <param name="encoding">The encoding to use for string conversion</param>
    /// <returns>The decoded string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadNullTerminatedString(
        this BinaryReader reader,
        int maxLength,
        System.Text.Encoding encoding
    )
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        var bytes = reader.ReadBytes(maxLength);
        var nullIndex = Array.IndexOf(bytes, (byte)0);

        if (nullIndex >= 0)
        {
            Array.Resize(ref bytes, nullIndex);
        }

        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Reads a fixed-length string and trims padding
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="length">The length to read</param>
    /// <param name="encoding">The encoding to use</param>
    /// <param name="trimChars">Characters to trim (defaults to null bytes and spaces)</param>
    /// <returns>The decoded and trimmed string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadFixedString(
        this BinaryReader reader,
        int length,
        System.Text.Encoding encoding,
        byte[]? trimChars = null
    )
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (length == 0)
        {
            return string.Empty;
        }

        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new EndOfStreamException($"Expected {length} bytes, got {bytes.Length}");
        }

        // Trim padding characters
        trimChars ??= new byte[] { 0, 32 }; // null and space

        var endIndex = bytes.Length;
        while (endIndex > 0 && Array.IndexOf(trimChars, bytes[endIndex - 1]) >= 0)
        {
            endIndex--;
        }

        if (endIndex == 0)
        {
            return string.Empty;
        }

        return encoding.GetString(bytes, 0, endIndex);
    }

    /// <summary>
    /// Skips the specified number of bytes
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="count">The number of bytes to skip</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Skip(this BinaryReader reader, int count)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
        {
            return;
        }

        // Try to seek if possible (faster than reading)
        if (reader.BaseStream.CanSeek)
        {
            reader.BaseStream.Seek(count, SeekOrigin.Current);
        }
        else
        {
            // Fall back to reading and discarding
            var buffer = new byte[Math.Min(count, 8192)];
            var remaining = count;

            while (remaining > 0)
            {
                var toRead = Math.Min(remaining, buffer.Length);
                var bytesRead = reader.Read(buffer, 0, toRead);

                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Reached end of stream while skipping bytes");
                }

                remaining -= bytesRead;
            }
        }
    }

    /// <summary>
    /// Peeks at the next byte without advancing the position
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <returns>The next byte, or -1 if at end of stream</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PeekByte(this BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (!reader.BaseStream.CanSeek)
        {
            throw new NotSupportedException("Stream must support seeking to peek");
        }

        var position = reader.BaseStream.Position;
        try
        {
            return reader.BaseStream.ReadByte();
        }
        finally
        {
            reader.BaseStream.Position = position;
        }
    }

    /// <summary>
    /// Reads bytes into a span for zero-copy operations
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="buffer">The buffer to read into</param>
    /// <returns>The number of bytes read</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Read(this BinaryReader reader, Span<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return reader.BaseStream.Read(buffer);
    }

    /// <summary>
    /// Reads exactly the specified number of bytes or throws an exception
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="buffer">The buffer to read into</param>
    /// <exception cref="EndOfStreamException">Thrown when not enough bytes are available</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadExactly(this BinaryReader reader, Span<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = reader.BaseStream.Read(buffer[totalRead..]);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException($"Expected {buffer.Length} bytes, got {totalRead}");
            }

            totalRead += bytesRead;
        }
    }

    /// <summary>
    /// Reads a little-endian integer of the specified size
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="size">The size in bytes (1, 2, 4, or 8)</param>
    /// <returns>The integer value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadLittleEndianInteger(this BinaryReader reader, int size)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return size switch
        {
            1 => reader.ReadByte(),
            2 => reader.ReadInt16(),
            4 => reader.ReadInt32(),
            8 => reader.ReadInt64(),
            _ => throw new ArgumentException($"Unsupported integer size: {size}", nameof(size)),
        };
    }

    /// <summary>
    /// Reads a big-endian integer of the specified size
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <param name="size">The size in bytes (1, 2, 4, or 8)</param>
    /// <returns>The integer value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadBigEndianInteger(this BinaryReader reader, int size)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return size switch
        {
            1 => reader.ReadByte(),
            2 => BinaryPrimitives.ReverseEndianness(reader.ReadInt16()),
            4 => BinaryPrimitives.ReverseEndianness(reader.ReadInt32()),
            8 => BinaryPrimitives.ReverseEndianness(reader.ReadInt64()),
            _ => throw new ArgumentException($"Unsupported integer size: {size}", nameof(size)),
        };
    }
}

/// <summary>
/// Helper methods for binary primitives operations
/// </summary>
public static class BinaryPrimitives
{
    /// <summary>
    /// Reverses the endianness of a 16-bit integer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReverseEndianness(short value)
    {
        return (short)((value << 8) | ((value >> 8) & 0xFF));
    }

    /// <summary>
    /// Reverses the endianness of a 32-bit integer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReverseEndianness(int value)
    {
        return (int)ReverseEndianness((uint)value);
    }

    /// <summary>
    /// Reverses the endianness of a 64-bit integer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReverseEndianness(long value)
    {
        return (long)ReverseEndianness((ulong)value);
    }

    /// <summary>
    /// Reverses the endianness of an unsigned 32-bit integer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseEndianness(uint value)
    {
        return ((value & 0x000000FFU) << 24)
            | ((value & 0x0000FF00U) << 8)
            | ((value & 0x00FF0000U) >> 8)
            | ((value & 0xFF000000U) >> 24);
    }

    /// <summary>
    /// Reverses the endianness of an unsigned 64-bit integer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseEndianness(ulong value)
    {
        return ((value & 0x00000000000000FFUL) << 56)
            | ((value & 0x000000000000FF00UL) << 40)
            | ((value & 0x0000000000FF0000UL) << 24)
            | ((value & 0x00000000FF000000UL) << 8)
            | ((value & 0x000000FF00000000UL) >> 8)
            | ((value & 0x0000FF0000000000UL) >> 24)
            | ((value & 0x00FF000000000000UL) >> 40)
            | ((value & 0xFF00000000000000UL) >> 56);
    }
}
