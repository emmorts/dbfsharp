using System.Buffers;
using System.Runtime.CompilerServices;
using DbfSharp.Core.Utils;

namespace DbfSharp.Core.Memo;

/// <summary>
/// dBase III memo file (.DBT) reader
/// </summary>
public sealed class Db3MemoFile : IMemoFile
{
    private const int DefaultBlockSize = 512;
    private const byte MemoTerminator = 0x1A;

    private readonly FileStream? _fileStream;
    private readonly DbfReaderOptions _options;
    private readonly Lock _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Gets the memo file path
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets whether the memo file is valid and accessible
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Initializes a new instance of Db3MemoFile
    /// </summary>
    /// <param name="filePath">The path to the DBT file</param>
    /// <param name="options">Reader options</param>
    public Db3MemoFile(string filePath, DbfReaderOptions options)
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
                throw new InvalidDataException($"Failed to open memo file '{filePath}': {ex.Message}", ex);
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
        if (blockPosition >= _fileStream!.Length)
        {
            return null;
        }

        _fileStream.Seek(blockPosition, SeekOrigin.Begin);

        using var bufferOwner = MemoryPool<byte>.Shared.Rent(DefaultBlockSize);
        var buffer = bufferOwner.Memory.Span[..DefaultBlockSize];

        using var dataBuilder = new ArrayPoolMemoryBuilder<byte>();

        while (true)
        {
            var bytesRead = _fileStream.Read(buffer);
            if (bytesRead == 0)
            {
                break;
            }

            // look for memo terminator
            var actualData = buffer[..bytesRead];
            var terminatorIndex = actualData.IndexOf(MemoTerminator);

            if (terminatorIndex >= 0)
            {
                // found terminator, add data up to terminator
                if (terminatorIndex > 0)
                {
                    dataBuilder.Append(actualData[..terminatorIndex]);
                }
                break;
            }

            // no terminator found, add all data and continue
            dataBuilder.Append(actualData);
        }

        return dataBuilder.Length > 0
            ? new TextMemo(dataBuilder.ToReadOnlyMemory())
            : null;
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
