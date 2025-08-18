using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DbfSharp.Core.Memo;

/// <summary>
/// dBase IV memo file (.DBT) reader
/// </summary>
public sealed class Db4MemoFile : IMemoFile
{
    private const int DefaultBlockSize = 512;
    private const byte FieldTerminator = 0x1F;
    private const uint ExpectedReservedValue = 0x0008FFFF; // 0xFF 0xFF 0x08 0x00 in little-endian (per official spec)

    private readonly FileStream? _fileStream;
    private readonly DbfReaderOptions _options;
    private readonly Lock _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// dBase IV memo block header
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct Db4MemoHeader(uint reserved, uint length)
    {
        public readonly uint Reserved = reserved;
        public readonly uint Length = length;

        /// <summary>
        /// Reads a memo header from the given span
        /// </summary>
        /// <param name="data">Span containing at least 8 bytes</param>
        /// <returns>The parsed header</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Db4MemoHeader ReadFromSpan(ReadOnlySpan<byte> data)
        {
            if (data.Length < 8)
            {
                throw new ArgumentException("Insufficient data for memo header", nameof(data));
            }

            var reserved = MemoryMarshal.Read<uint>(data);
            var length = MemoryMarshal.Read<uint>(data[4..]);

            return new Db4MemoHeader(reserved, length);
        }

        /// <summary>
        /// Gets whether this header appears to be valid
        /// </summary>
        public bool IsValid => Length is > 0 and <= int.MaxValue;

        /// <summary>
        /// Gets whether the reserved field matches the expected dBase IV signature
        /// </summary>
        public bool HasValidSignature => Reserved == ExpectedReservedValue;
    }

    /// <summary>
    /// Gets the memo file path
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets whether the memo file is valid and accessible
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Initializes a new instance of Db4MemoFile
    /// </summary>
    /// <param name="filePath">The path to the DBT file</param>
    /// <param name="options">Reader options</param>
    public Db4MemoFile(string filePath, DbfReaderOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);

        FilePath = filePath;
        _options = options;

        try
        {
            _fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                _options.BufferSize,
                FileOptions.SequentialScan
            );
            IsValid = true;
        }
        catch (Exception ex)
        {
            IsValid = false;
            _fileStream?.Dispose();

            if (!_options.IgnoreMissingMemoFile)
            {
                throw new InvalidDataException(
                    $"Failed to open memo file '{filePath}': {ex.Message}",
                    ex
                );
            }
        }
    }

    /// <summary>
    /// Gets a memo by its block index
    /// </summary>
    /// <param name="index">The memo block index</param>
    /// <returns>The memo data, or null if not found</returns>
    public MemoData? GetMemo(int index)
    {
        if (!IsValid || index <= 0)
        {
            return null;
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            lock (_lockObject)
            {
                return ReadMemoInternal(index);
            }
        }
        catch (Exception ex)
        {
            if (_options.ValidateFields)
            {
                throw new InvalidDataException(
                    $"Failed to read memo at index {index}: {ex.Message}",
                    ex
                );
            }

            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TextMemo? ReadMemoInternal(int index)
    {
        var blockPosition = (long)index * DefaultBlockSize;
        if (blockPosition + 8 > _fileStream!.Length) // need at least 8 bytes for header
        {
            return null;
        }

        _fileStream.Seek(blockPosition, SeekOrigin.Begin);

        Span<byte> headerBuffer = stackalloc byte[8];
        var headerBytesRead = _fileStream.Read(headerBuffer);
        if (headerBytesRead < 8)
        {
            return null;
        }

        var memoHeader = Db4MemoHeader.ReadFromSpan(headerBuffer);

        if (!memoHeader.IsValid)
        {
            return null;
        }

        // validate signature if strict validation is enabled
        if (_options.ValidateFields && !memoHeader.HasValidSignature)
        {
            throw new InvalidDataException(
                $"Invalid memo header signature at index {index}. "
                    + $"Expected 0x{ExpectedReservedValue:X8}, got 0x{memoHeader.Reserved:X8}"
            );
        }

        var dataLength = (int)memoHeader.Length;

        var remainingFileLength = _fileStream.Length - _fileStream.Position;
        if (dataLength <= remainingFileLength)
        {
            // use ArrayPool for large allocations, stack for small ones
            return dataLength <= 1024 ? ReadSmallMemo(dataLength) : ReadLargeMemo(dataLength);
        }

        if (_options.ValidateFields)
        {
            throw new InvalidDataException(
                $"Memo at index {index} claims length {dataLength} but only {remainingFileLength} bytes remain in file"
            );
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TextMemo? ReadSmallMemo(int dataLength)
    {
        Span<byte> buffer = stackalloc byte[dataLength];
        var bytesRead = _fileStream!.Read(buffer);

        if (bytesRead != dataLength)
        {
            return null;
        }

        var terminatorIndex = buffer.IndexOf(FieldTerminator);
        var actualData = terminatorIndex >= 0 ? buffer[..terminatorIndex] : buffer;

        if (actualData.IsEmpty)
        {
            return null;
        }

        // copy to managed memory since we're using stack alloc
        var result = new byte[actualData.Length];
        actualData.CopyTo(result);

        return new TextMemo(new ReadOnlyMemory<byte>(result));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TextMemo? ReadLargeMemo(int dataLength)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(dataLength);
        try
        {
            var actualBuffer = buffer.AsSpan(0, dataLength);

            var bytesRead = _fileStream!.Read(actualBuffer);
            if (bytesRead != dataLength)
            {
                return null;
            }

            var terminatorIndex = actualBuffer.IndexOf(FieldTerminator);

            var actualData = terminatorIndex >= 0 ? actualBuffer[..terminatorIndex] : actualBuffer;
            if (actualData.IsEmpty)
            {
                return null;
            }

            var result = new byte[actualData.Length];
            actualData.CopyTo(result);

            return new TextMemo(new ReadOnlyMemory<byte>(result));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Disposes of the memo file resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_disposed)
            {
                return;
            }

            _fileStream?.Dispose();
            _disposed = true;
        }
    }
}
