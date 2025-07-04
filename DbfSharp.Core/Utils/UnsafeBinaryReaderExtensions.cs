#if UNSAFE_ENABLED

using System.Runtime.CompilerServices;

namespace DbfSharp.Core.Utils;

/// <summary>
/// High-performance unsafe extensions for BinaryReader
/// These methods require the UNSAFE_ENABLED compilation symbol and AllowUnsafeBlocks=true
/// </summary>
public static class UnsafeBinaryReaderExtensions
{
    /// <summary>
    /// Reads a structure from the binary reader using unsafe operations for maximum performance
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type to read</typeparam>
    /// <param name="reader">The binary reader</param>
    /// <returns>The read structure</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null</exception>
    /// <exception cref="EndOfStreamException">Thrown when not enough data is available</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T ReadStructUnsafe<T>(this BinaryReader reader) where T : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var size = sizeof(T);
        var bytes = reader.ReadBytes(size);
        
        if (bytes.Length != size)
            throw new EndOfStreamException($"Expected {size} bytes for {typeof(T).Name}, got {bytes.Length}");

        fixed (byte* ptr = bytes)
        {
            return *(T*)ptr;
        }
    }

    /// <summary>
    /// Reads multiple structures from the binary reader using unsafe operations
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type to read</typeparam>
    /// <param name="reader">The binary reader</param>
    /// <param name="count">The number of structures to read</param>
    /// <returns>An array of structures</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T[] ReadStructArrayUnsafe<T>(this BinaryReader reader, int count) where T : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == 0)
            return Array.Empty<T>();

        var result = new T[count];
        var size = sizeof(T);
        var totalBytes = size * count;
        
        var bytes = reader.ReadBytes(totalBytes);
        if (bytes.Length != totalBytes)
            throw new EndOfStreamException($"Expected {totalBytes} bytes for {count} {typeof(T).Name} structures, got {bytes.Length}");

        fixed (byte* bytePtr = bytes)
        fixed (T* resultPtr = result)
        {
            Buffer.MemoryCopy(bytePtr, resultPtr, totalBytes, totalBytes);
        }

        return result;
    }

    /// <summary>
    /// Reads data directly into a span using unsafe operations
    /// </summary>
    /// <typeparam name="T">The unmanaged type</typeparam>
    /// <param name="reader">The binary reader</param>
    /// <param name="destination">The destination span</param>
    /// <exception cref="EndOfStreamException">Thrown when not enough data is available</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ReadIntoSpanUnsafe<T>(this BinaryReader reader, Span<T> destination) where T : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        if (destination.IsEmpty)
            return;

        var size = sizeof(T);
        var totalBytes = size * destination.Length;
        var bytes = reader.ReadBytes(totalBytes);
        
        if (bytes.Length != totalBytes)
            throw new EndOfStreamException($"Expected {totalBytes} bytes, got {bytes.Length}");

        fixed (byte* bytePtr = bytes)
        fixed (T* destPtr = destination)
        {
            Buffer.MemoryCopy(bytePtr, destPtr, totalBytes, totalBytes);
        }
    }

    /// <summary>
    /// Converts a byte span to a value type using unsafe casting
    /// </summary>
    /// <typeparam name="T">The unmanaged value type</typeparam>
    /// <param name="bytes">The byte span</param>
    /// <returns>The converted value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T CastToStruct<T>(ReadOnlySpan<byte> bytes) where T : unmanaged
    {
        var size = sizeof(T);
        if (bytes.Length < size)
            throw new ArgumentException($"Not enough bytes: expected {size}, got {bytes.Length}");

        fixed (byte* ptr = bytes)
        {
            return *(T*)ptr;
        }
    }

    /// <summary>
    /// Converts a value type to bytes using unsafe casting
    /// </summary>
    /// <typeparam name="T">The unmanaged value type</typeparam>
    /// <param name="value">The value to convert</param>
    /// <returns>The byte representation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte[] StructToBytes<T>(T value) where T : unmanaged
    {
        var size = sizeof(T);
        var result = new byte[size];
        
        fixed (byte* ptr = result)
        {
            *(T*)ptr = value;
        }
        
        return result;
    }

    /// <summary>
    /// Fast memory comparison using unsafe operations
    /// </summary>
    /// <param name="span1">First span to compare</param>
    /// <param name="span2">Second span to compare</param>
    /// <returns>True if the spans contain identical data</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool SequenceEqualUnsafe(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
    {
        if (span1.Length != span2.Length)
            return false;

        if (span1.Length == 0)
            return true;

        fixed (byte* ptr1 = span1)
        fixed (byte* ptr2 = span2)
        {
            return memcmp(ptr1, ptr2, span1.Length) == 0;
        }
    }

    /// <summary>
    /// Fast memory search using unsafe operations
    /// </summary>
    /// <param name="haystack">The span to search in</param>
    /// <param name="needle">The value to search for</param>
    /// <returns>The index of the first occurrence, or -1 if not found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int IndexOfUnsafe(ReadOnlySpan<byte> haystack, byte needle)
    {
        if (haystack.IsEmpty)
            return -1;

        fixed (byte* ptr = haystack)
        {
            var result = memchr(ptr, needle, haystack.Length);
            return result == null ? -1 : (int)(result - ptr);
        }
    }

    // P/Invoke declarations for native memory functions
    [System.Runtime.InteropServices.DllImport("msvcrt.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private static extern unsafe int memcmp(byte* ptr1, byte* ptr2, int count);

    [System.Runtime.InteropServices.DllImport("msvcrt.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private static extern unsafe byte* memchr(byte* ptr, int value, int count);
}

#endif