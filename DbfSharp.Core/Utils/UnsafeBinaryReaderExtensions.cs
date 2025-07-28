#if UNSAFE_ENABLED
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DbfSharp.Core.Utils;

/// <summary>
/// High-performance unsafe extensions for BinaryReader
/// These methods require the UNSAFE_ENABLED compilation symbol and AllowUnsafeBlocks=true
/// Uses modern .NET intrinsics and cross-platform optimizations
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
    public static unsafe T ReadStructUnsafe<T>(this BinaryReader reader)
        where T : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var size = sizeof(T);
        var bytes = reader.ReadBytes(size);

        if (bytes.Length != size)
            throw new EndOfStreamException(
                $"Expected {size} bytes for {typeof(T).Name}, got {bytes.Length}"
            );

        return MemoryMarshal.Read<T>(bytes);
    }

    /// <summary>
    /// Reads multiple structures from the binary reader using unsafe operations
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type to read</typeparam>
    /// <param name="reader">The binary reader</param>
    /// <param name="count">The number of structures to read</param>
    /// <returns>An array of structures</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadStructArrayUnsafe<T>(this BinaryReader reader, int count)
        where T : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == 0)
            return Array.Empty<T>();

        var result = new T[count];
        var size = Unsafe.SizeOf<T>();
        var totalBytes = size * count;

        var bytes = reader.ReadBytes(totalBytes);
        if (bytes.Length != totalBytes)
            throw new EndOfStreamException(
                $"Expected {totalBytes} bytes for {count} {typeof(T).Name} structures, got {bytes.Length}"
            );

        MemoryMarshal.Cast<byte, T>(bytes).CopyTo(result);
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
    public static void ReadIntoSpanUnsafe<T>(this BinaryReader reader, Span<T> destination)
        where T : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        if (destination.IsEmpty)
            return;

        var size = Unsafe.SizeOf<T>();
        var totalBytes = size * destination.Length;
        var bytes = reader.ReadBytes(totalBytes);

        if (bytes.Length != totalBytes)
            throw new EndOfStreamException($"Expected {totalBytes} bytes, got {bytes.Length}");

        var sourceSpan = MemoryMarshal.Cast<byte, T>(bytes);
        sourceSpan.CopyTo(destination);
    }

    /// <summary>
    /// Converts a byte span to a value type using safe marshaling
    /// </summary>
    /// <typeparam name="T">The unmanaged value type</typeparam>
    /// <param name="bytes">The byte span</param>
    /// <returns>The converted value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T CastToStruct<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (bytes.Length < size)
            throw new ArgumentException($"Not enough bytes: expected {size}, got {bytes.Length}");

        return MemoryMarshal.Read<T>(bytes);
    }

    /// <summary>
    /// Converts a value type to bytes using safe marshaling
    /// </summary>
    /// <typeparam name="T">The unmanaged value type</typeparam>
    /// <param name="value">The value to convert</param>
    /// <returns>The byte representation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] StructToBytes<T>(T value)
        where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        var result = new byte[size];
        MemoryMarshal.Write(result, ref Unsafe.AsRef(value));
        return result;
    }

    /// <summary>
    /// Fast memory comparison using .NET's optimized SequenceEqual
    /// </summary>
    /// <param name="span1">First span to compare</param>
    /// <param name="span2">Second span to compare</param>
    /// <returns>True if the spans contain identical data</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SequenceEqualUnsafe(
        ReadOnlySpan<byte> span1,
        ReadOnlySpan<byte> span2
    )
    {
        return span1.SequenceEqual(span2);
    }

    /// <summary>
    /// Fast memory search using .NET's optimized IndexOf
    /// </summary>
    /// <param name="haystack">The span to search in</param>
    /// <param name="needle">The value to search for</param>
    /// <returns>The index of the first occurrence, or -1 if not found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfUnsafe(ReadOnlySpan<byte> haystack, byte needle)
    {
        return haystack.IndexOf(needle);
    }

    /// <summary>
    /// Fast memory search for a sequence using .NET's optimized IndexOf
    /// </summary>
    /// <param name="haystack">The span to search in</param>
    /// <param name="needle">The sequence to search for</param>
    /// <returns>The index of the first occurrence, or -1 if not found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfSequenceUnsafe(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        return haystack.IndexOf(needle);
    }

    /// <summary>
    /// Fast memory filling using .NET's optimized Fill
    /// </summary>
    /// <param name="destination">The span to fill</param>
    /// <param name="value">The value to fill with</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillUnsafe(Span<byte> destination, byte value)
    {
        destination.Fill(value);
    }

    /// <summary>
    /// Fast memory copying using .NET's optimized CopyTo
    /// </summary>
    /// <param name="source">The source span</param>
    /// <param name="destination">The destination span</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyUnsafe(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException("Destination span is too small");
        
        source.CopyTo(destination);
    }

    /// <summary>
    /// Reads a structure directly from a stream using a stackalloc buffer for small structures
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type to read</typeparam>
    /// <param name="stream">The stream to read from</param>
    /// <returns>The read structure</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T ReadStructFromStreamUnsafe<T>(Stream stream)
        where T : unmanaged
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var size = sizeof(T);
        
        // stackalloc for small structures to avoid heap alloc
        if (size <= 1024) // should be reasonable stack alloc limit
        {
            Span<byte> buffer = stackalloc byte[size];
            var bytesRead = stream.Read(buffer);
            
            if (bytesRead != size)
                throw new EndOfStreamException(
                    $"Expected {size} bytes for {typeof(T).Name}, got {bytesRead}"
                );

            return MemoryMarshal.Read<T>(buffer);
        }
        else
        {
            // fallback to heap allocation for large structures
            var buffer = new byte[size];
            var bytesRead = stream.Read(buffer);
            
            if (bytesRead != size)
                throw new EndOfStreamException(
                    $"Expected {size} bytes for {typeof(T).Name}, got {bytesRead}"
                );

            return MemoryMarshal.Read<T>(buffer);
        }
    }

    /// <summary>
    /// Reads structures directly from a stream into a pre-allocated array
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type to read</typeparam>
    /// <param name="stream">The stream to read from</param>
    /// <param name="destination">The destination array</param>
    /// <param name="offset">The offset in the destination array</param>
    /// <param name="count">The number of structures to read</param>
    /// <returns>The number of structures actually read</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadStructArrayFromStreamUnsafe<T>(
        Stream stream, 
        T[] destination, 
        int offset, 
        int count)
        where T : unmanaged
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (offset < 0 || offset >= destination.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0 || offset + count > destination.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == 0)
            return 0;

        var destSpan = destination.AsSpan(offset, count);
        var byteSpan = MemoryMarshal.AsBytes(destSpan);
        var bytesRead = stream.Read(byteSpan);
        
        var structSize = Unsafe.SizeOf<T>();
        return bytesRead / structSize;
    }
}

#endif