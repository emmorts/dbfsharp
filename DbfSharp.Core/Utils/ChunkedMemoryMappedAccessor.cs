using System.IO.MemoryMappedFiles;

namespace DbfSharp.Core.Utils;

/// <summary>
/// Provides chunked access to memory-mapped files to avoid virtual address space issues with large files.
/// Maps only portions of the file at a time to reduce memory pressure.
/// </summary>
internal sealed class ChunkedMemoryMappedAccessor(
    MemoryMappedFile memoryMappedFile,
    long fileSize,
    long chunkSize = ChunkedMemoryMappedAccessor.DefaultChunkSize
) : IDisposable
{
    private readonly MemoryMappedFile _memoryMappedFile =
        memoryMappedFile ?? throw new ArgumentNullException(nameof(memoryMappedFile));
    private readonly long _chunkSize = Math.Min(chunkSize, fileSize); // don't exceed file size
    private readonly Lock _lock = new();

    private MemoryMappedViewAccessor? _currentChunk;
    private long _currentChunkStartOffset = -1;
    private long _currentChunkEndOffset = -1;
    private bool _disposed;

    /// <summary>
    /// Default chunk size: 256 MB - large enough for efficiency, small enough to avoid virtual address space issues
    /// </summary>
    private const long DefaultChunkSize = 256 * 1024 * 1024; // 256 MB

    /// <summary>
    /// Gets the total capacity (file size)
    /// </summary>
    public long Capacity => fileSize;

    /// <summary>
    /// Reads an array of bytes from the specified position
    /// </summary>
    public void ReadArray(long position, byte[] array, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (position < 0 || position >= fileSize)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (offset < 0 || count < 0 || offset + count > array.Length)
        {
            throw new ArgumentException("Invalid offset or count");
        }

        if (position + count > fileSize)
        {
            throw new ArgumentException("Read would exceed file bounds");
        }

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ChunkedMemoryMappedAccessor));

            if (!IsPositionInCurrentChunk(position, count))
            {
                LoadChunkForPosition(position);
            }

            var relativePosition = position - _currentChunkStartOffset;
            _currentChunk!.ReadArray(relativePosition, array, offset, count);
        }
    }

    /// <summary>
    /// Checks if the specified position and length are fully contained within the current chunk
    /// </summary>
    private bool IsPositionInCurrentChunk(long position, int count)
    {
        return _currentChunk != null
            && position >= _currentChunkStartOffset
            && position + count <= _currentChunkEndOffset;
    }

    /// <summary>
    /// Loads the appropriate chunk for the specified position
    /// </summary>
    private void LoadChunkForPosition(long position)
    {
        var chunkStartOffset = position / _chunkSize * _chunkSize;
        var chunkEndOffset = Math.Min(chunkStartOffset + _chunkSize, fileSize);
        var chunkLength = chunkEndOffset - chunkStartOffset;

        if (
            _currentChunk != null
            && chunkStartOffset == _currentChunkStartOffset
            && chunkEndOffset == _currentChunkEndOffset
        )
        {
            return;
        }

        _currentChunk?.Dispose();

        _currentChunk = _memoryMappedFile.CreateViewAccessor(
            chunkStartOffset,
            chunkLength,
            MemoryMappedFileAccess.Read
        );

        _currentChunkStartOffset = chunkStartOffset;
        _currentChunkEndOffset = chunkEndOffset;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _currentChunk?.Dispose();
            _currentChunk = null;
            _disposed = true;
        }
    }
}
