using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DbfSharp.Core.Memo;

/// <summary>
/// Visual FoxPro memo file (.FPT) reader
/// Based on the VFPMemoFile implementation from Python dbfread
/// </summary>
public sealed class VfpMemoFile : IMemoFile
{
    private const int HeaderSize = 512; // VFP memo file header is always 512 bytes
    private const int BlockHeaderSize = 8; // block header is 8 bytes (type + length)
    private const int DefaultBlockSize = 64;
    private const int SmallMemoThreshold = 4096;

    private readonly FileStream? _fileStream;
    private readonly VfpMemoHeader _header;
    private readonly DbfReaderOptions _options;
    private readonly Lock _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Visual FoxPro memo file header structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct VfpMemoHeader(
        uint nextBlock,
        ushort reserved1,
        ushort blockSize,
        uint reserved2
    )
    {
        public readonly uint NextBlock = nextBlock; // next available block
        public readonly ushort Reserved1 = reserved1; // reserved
        public readonly ushort BlockSize = blockSize; // size of each block
        public readonly uint Reserved2 = reserved2; // reserved (remaining bytes)

        /// <summary>
        /// Reads a VFP memo header from the given span
        /// </summary>
        /// <param name="data">Span containing at least 512 bytes</param>
        /// <returns>The parsed header</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VfpMemoHeader ReadFromSpan(ReadOnlySpan<byte> data)
        {
            if (data.Length < HeaderSize)
            {
                throw new ArgumentException(
                    $"Insufficient data for memo header, need {HeaderSize} bytes",
                    nameof(data)
                );
            }

            var nextBlock = MemoryMarshal.Read<uint>(data);
            var reserved1 = MemoryMarshal.Read<ushort>(data[4..]);
            var blockSize = MemoryMarshal.Read<ushort>(data[6..]);

            return new VfpMemoHeader(nextBlock, reserved1, blockSize, 0);
        }

        /// <summary>
        /// Gets whether this header appears to be valid
        /// </summary>
        public bool IsValid => BlockSize > 0;

        /// <summary>
        /// Gets the actual block size, using default if header value is invalid
        /// </summary>
        public int EffectiveBlockSize => BlockSize > 0 ? BlockSize : DefaultBlockSize;
    }

    /// <summary>
    /// Visual FoxPro memo block header
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct VfpMemoBlockHeader(uint type, uint length)
    {
        public readonly uint Type = type; // memo type (0=Picture, 1=Text, 2=Object)
        public readonly uint Length = length; // length of memo data

        /// <summary>
        /// Reads a memo block header from the given span
        /// </summary>
        /// <param name="data">Span containing at least 8 bytes</param>
        /// <returns>The parsed block header</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VfpMemoBlockHeader ReadFromSpan(ReadOnlySpan<byte> data)
        {
            if (data.Length < BlockHeaderSize)
            {
                throw new ArgumentException(
                    $"Insufficient data for block header, need {BlockHeaderSize} bytes",
                    nameof(data)
                );
            }

            var type = MemoryMarshal.Read<uint>(data);
            var length = MemoryMarshal.Read<uint>(data[4..]);

            return new VfpMemoBlockHeader(type, length);
        }

        /// <summary>
        /// Gets whether this block header appears to be valid
        /// </summary>
        public bool IsValid => Length is > 0 and <= int.MaxValue;

        /// <summary>
        /// Gets the memo type as an enum
        /// </summary>
        public MemoType MemoType => (MemoType)Type;
    }

    /// <summary>
    /// Memo type enumeration
    /// </summary>
    private enum MemoType : uint
    {
        Picture = 0,
        Text = 1,
        Object = 2,
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
    /// Initializes a new instance of VfpMemoFile
    /// </summary>
    /// <param name="filePath">The path to the FPT file</param>
    /// <param name="options">Reader options</param>
    public VfpMemoFile(string filePath, DbfReaderOptions options)
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

            _header = ReadHeaderFromFile();

            if (!_header.IsValid)
            {
                throw new InvalidDataException(
                    $"Invalid memo file header: block size is {_header.BlockSize}"
                );
            }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VfpMemoHeader ReadHeaderFromFile()
    {
        if (_fileStream!.Length < HeaderSize)
        {
            throw new InvalidDataException(
                $"File too small for VFP memo header, need {HeaderSize} bytes"
            );
        }

        // use ArrayPool for header reading since it's exactly 512 bytes
        var headerBuffer = ArrayPool<byte>.Shared.Rent(HeaderSize);
        try
        {
            var actualBuffer = headerBuffer.AsSpan(0, HeaderSize);
            _fileStream.Seek(0, SeekOrigin.Begin);

            var bytesRead = _fileStream.Read(actualBuffer);
            if (bytesRead < HeaderSize)
            {
                throw new InvalidDataException(
                    $"Could not read complete header, got {bytesRead} bytes"
                );
            }

            return VfpMemoHeader.ReadFromSpan(actualBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
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
    private MemoData? ReadMemoInternal(int index)
    {
        var blockPosition = (long)index * _header.EffectiveBlockSize;
        if (blockPosition + BlockHeaderSize > _fileStream!.Length)
        {
            return null;
        }

        _fileStream.Seek(blockPosition, SeekOrigin.Begin);

        Span<byte> blockHeaderBuffer = stackalloc byte[BlockHeaderSize];
        var headerBytesRead = _fileStream.Read(blockHeaderBuffer);
        if (headerBytesRead < BlockHeaderSize)
        {
            return null;
        }

        var blockHeader = VfpMemoBlockHeader.ReadFromSpan(blockHeaderBuffer);

        if (!blockHeader.IsValid)
        {
            return null;
        }

        var dataLength = (int)blockHeader.Length;

        var remainingFileLength = _fileStream.Length - _fileStream.Position;
        if (dataLength <= remainingFileLength)
        {
            return dataLength <= SmallMemoThreshold
                ? ReadSmallMemo(blockHeader.MemoType, dataLength)
                : ReadLargeMemo(blockHeader.MemoType, dataLength);
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
    private MemoData? ReadSmallMemo(MemoType memoType, int dataLength)
    {
        Span<byte> buffer = stackalloc byte[dataLength];
        var bytesRead = _fileStream!.Read(buffer);

        if (bytesRead != dataLength)
        {
            return null;
        }

        var result = new byte[dataLength];
        buffer.CopyTo(result);

        return CreateTypedMemo(memoType, new ReadOnlyMemory<byte>(result));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MemoData? ReadLargeMemo(MemoType memoType, int dataLength)
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

            var result = new byte[dataLength];
            actualBuffer.CopyTo(result);

            return CreateTypedMemo(memoType, new ReadOnlyMemory<byte>(result));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Creates typed memo data based on the memo type
    /// </summary>
    /// <param name="memoType">The memo type</param>
    /// <param name="data">The raw memo data</param>
    /// <returns>Typed memo data</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MemoData CreateTypedMemo(MemoType memoType, ReadOnlyMemory<byte> data)
    {
        return memoType switch
        {
            MemoType.Picture => new PictureMemo(data),
            MemoType.Text => new TextMemo(data),
            MemoType.Object => new ObjectMemo(data),
            _ => new BinaryMemo(data), // Unknown type, treat as binary
        };
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
