using System.Runtime.InteropServices;

namespace DbfSharp.Core.Memo;

/// <summary>
/// Visual FoxPro memo file (.FPT) reader
/// Based on the VFPMemoFile implementation from Python dbfread
/// </summary>
public sealed class VfpMemoFile : IMemoFile
{
    private readonly FileStream _fileStream;
    private readonly BinaryReader _reader;
    private readonly VfpMemoHeader _header;
    private readonly DbfReaderOptions _options;
    private bool _disposed;

    /// <summary>
    /// Visual FoxPro memo file header structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct VfpMemoHeader(uint nextBlock, ushort reserved1, ushort blockSize, uint reserved2)
    {
        public readonly uint NextBlock = nextBlock; // Next available block
        public readonly ushort Reserved1 = reserved1; // Reserved
        public readonly ushort BlockSize = blockSize; // Size of each block
        public readonly uint Reserved2 = reserved2; // Reserved (504 bytes total)

        public static VfpMemoHeader Read(BinaryReader reader)
        {
            var nextBlock = reader.ReadUInt32();
            var reserved1 = reader.ReadUInt16();
            var blockSize = reader.ReadUInt16();

            // Skip the remaining 504 bytes of reserved space
            reader.BaseStream.Seek(504, SeekOrigin.Current);

            return new VfpMemoHeader(nextBlock, reserved1, blockSize, 0);
        }
    }

    /// <summary>
    /// Visual FoxPro memo block header
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct VfpMemoBlockHeader(uint type, uint length)
    {
        public readonly uint Type = type; // Memo type (0=Picture, 1=Text, 2=Object)
        public readonly uint Length = length; // Length of memo data

        public static VfpMemoBlockHeader Read(BinaryReader reader)
        {
            var type = reader.ReadUInt32();
            var length = reader.ReadUInt32();
            return new VfpMemoBlockHeader(type, length);
        }
    }

    /// <summary>
    /// Memo type enumeration
    /// </summary>
    private enum MemoType : uint
    {
        Picture = 0,
        Text = 1,
        Object = 2
    }

    /// <summary>
    /// Gets the memo file path
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets whether the memo file is valid and accessible
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Initializes a new instance of VfpMemoFile
    /// </summary>
    /// <param name="filePath">The path to the FPT file</param>
    /// <param name="options">Reader options</param>
    public VfpMemoFile(string filePath, DbfReaderOptions options)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        try
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.BufferSize);
            _reader = new BinaryReader(_fileStream);
            _header = VfpMemoHeader.Read(_reader);
            IsValid = true;

            // Validate header
            if (_header.BlockSize == 0)
            {
                IsValid = false;
                throw new InvalidDataException("Invalid memo file: block size is zero");
            }
        }
        catch (Exception ex)
        {
            IsValid = false;
            _fileStream?.Dispose();
            _reader?.Dispose();

            if (!_options.IgnoreMissingMemoFile)
                throw new InvalidDataException($"Failed to open memo file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a memo by its block index
    /// </summary>
    /// <param name="index">The memo block index</param>
    /// <returns>The memo data, or null if not found</returns>
    public byte[]? GetMemo(int index)
    {
        if (!IsValid || index <= 0 || _disposed)
            return null;

        try
        {
            // Calculate block position
            var blockPosition = (long)index * _header.BlockSize;

            // Check if position is within file bounds
            if (blockPosition >= _fileStream.Length)
                return null;

            // Seek to block position
            _fileStream.Seek(blockPosition, SeekOrigin.Begin);

            // Read memo block header
            var blockHeader = VfpMemoBlockHeader.Read(_reader);

            // Validate memo length
            if (blockHeader.Length == 0 || blockHeader.Length > int.MaxValue)
                return null;

            // Check if we have enough data
            var remainingBytes = _fileStream.Length - _fileStream.Position;
            if (blockHeader.Length > remainingBytes)
                return null;

            // Read memo data
            var memoData = _reader.ReadBytes((int)blockHeader.Length);
            if (memoData.Length != blockHeader.Length)
                return null;

            // Return typed memo data based on memo type
            return CreateTypedMemo(blockHeader.Type, memoData);
        }
        catch (Exception ex)
        {
            if (_options.ValidateFields)
                throw new InvalidDataException($"Failed to read memo at index {index}: {ex.Message}", ex);

            return null;
        }
    }

    /// <summary>
    /// Creates typed memo data based on the memo type
    /// </summary>
    /// <param name="type">The memo type</param>
    /// <param name="data">The raw memo data</param>
    /// <returns>Typed memo data</returns>
    private static byte[] CreateTypedMemo(uint type, byte[] data)
    {
        return (MemoType)type switch
        {
            MemoType.Picture => new PictureMemo(data),
            MemoType.Text => new TextMemo(data),
            MemoType.Object => new ObjectMemo(data),
            _ => new BinaryMemo(data) // Unknown type, treat as binary
        };
    }

    /// <summary>
    /// Disposes of the memo file resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _fileStream?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// dBase III memo file (.DBT) reader
/// </summary>
public sealed class Db3MemoFile : IMemoFile
{
    private readonly FileStream _fileStream;
    private readonly BinaryReader _reader;
    private readonly DbfReaderOptions _options;
    private bool _disposed;

    /// <summary>
    /// Gets the memo file path
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets whether the memo file is valid and accessible
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Initializes a new instance of Db3MemoFile
    /// </summary>
    /// <param name="filePath">The path to the DBT file</param>
    /// <param name="options">Reader options</param>
    public Db3MemoFile(string filePath, DbfReaderOptions options)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        try
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.BufferSize);
            _reader = new BinaryReader(_fileStream);
            IsValid = true;
        }
        catch (Exception ex)
        {
            IsValid = false;
            _fileStream?.Dispose();
            _reader?.Dispose();

            if (!_options.IgnoreMissingMemoFile)
                throw new InvalidDataException($"Failed to open memo file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a memo by its block index
    /// </summary>
    /// <param name="index">The memo block index</param>
    /// <returns>The memo data, or null if not found</returns>
    public byte[]? GetMemo(int index)
    {
        if (!IsValid || index <= 0 || _disposed)
            return null;

        try
        {
            const int blockSize = 512;
            var blockPosition = (long)index * blockSize;

            if (blockPosition >= _fileStream.Length)
                return null;

            _fileStream.Seek(blockPosition, SeekOrigin.Begin);

            var data = new List<byte>();
            var buffer = new byte[blockSize];

            while (true)
            {
                var bytesRead = _fileStream.Read(buffer, 0, blockSize);
                if (bytesRead == 0)
                    break;

                // Look for memo terminator (0x1A)
                var terminatorIndex = Array.IndexOf(buffer, (byte)0x1A, 0, bytesRead);
                if (terminatorIndex >= 0)
                {
                    // Found terminator, add data up to terminator
                    data.AddRange(buffer.Take(terminatorIndex));
                    break;
                }

                // No terminator found, add all data and continue
                data.AddRange(buffer.Take(bytesRead));
            }

            return data.Count > 0 ? data.ToArray() : null;
        }
        catch (Exception ex)
        {
            if (_options.ValidateFields)
                throw new InvalidDataException($"Failed to read memo at index {index}: {ex.Message}", ex);

            return null;
        }
    }

    /// <summary>
    /// Disposes of the memo file resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _fileStream?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// dBase IV memo file (.DBT) reader
/// </summary>
public sealed class Db4MemoFile : IMemoFile
{
    private readonly FileStream _fileStream;
    private readonly BinaryReader _reader;
    private readonly DbfReaderOptions _options;
    private bool _disposed;

    /// <summary>
    /// dBase IV memo block header
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct Db4MemoHeader(uint reserved, uint length)
    {
        public readonly uint Reserved = reserved; // Always 0xFF 0xFF 0x08 0x08
        public readonly uint Length = length; // Length of memo data

        public static Db4MemoHeader Read(BinaryReader reader)
        {
            var reserved = reader.ReadUInt32();
            var length = reader.ReadUInt32();
            return new Db4MemoHeader(reserved, length);
        }
    }

    /// <summary>
    /// Gets the memo file path
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets whether the memo file is valid and accessible
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Initializes a new instance of Db4MemoFile
    /// </summary>
    /// <param name="filePath">The path to the DBT file</param>
    /// <param name="options">Reader options</param>
    public Db4MemoFile(string filePath, DbfReaderOptions options)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        try
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.BufferSize);
            _reader = new BinaryReader(_fileStream);
            IsValid = true;
        }
        catch (Exception ex)
        {
            IsValid = false;
            _fileStream?.Dispose();
            _reader?.Dispose();

            if (!_options.IgnoreMissingMemoFile)
                throw new InvalidDataException($"Failed to open memo file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a memo by its block index
    /// </summary>
    /// <param name="index">The memo block index</param>
    /// <returns>The memo data, or null if not found</returns>
    public byte[]? GetMemo(int index)
    {
        if (!IsValid || index <= 0 || _disposed)
            return null;

        try
        {
            const int blockSize = 512;
            var blockPosition = (long)index * blockSize;

            if (blockPosition >= _fileStream.Length)
                return null;

            _fileStream.Seek(blockPosition, SeekOrigin.Begin);

            // Read memo header
            var memoHeader = Db4MemoHeader.Read(_reader);

            // Validate length
            if (memoHeader.Length == 0 || memoHeader.Length > int.MaxValue)
                return null;

            // Read memo data
            var data = _reader.ReadBytes((int)memoHeader.Length);
            if (data.Length != memoHeader.Length)
                return null;

            // Remove field terminators (0x1F is common in dBase IV)
            var terminatorIndex = Array.IndexOf(data, (byte)0x1F);
            if (terminatorIndex >= 0)
            {
                Array.Resize(ref data, terminatorIndex);
            }

            return data;
        }
        catch (Exception ex)
        {
            if (_options.ValidateFields)
                throw new InvalidDataException($"Failed to read memo at index {index}: {ex.Message}", ex);

            return null;
        }
    }

    /// <summary>
    /// Disposes of the memo file resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _fileStream?.Dispose();
            _disposed = true;
        }
    }
}