using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace DbfSharp.ConsoleAot.Input;

/// <summary>
/// High-performance utilities for stream operations and data transfer
/// </summary>
public static class StreamUtilities
{
    private const int DefaultBufferSize = 65536;
    private const int MinimumDataThreshold = 32;
    private const long MemoryMappingThreshold = 256 * 1024 * 1024;

    /// <summary>
    /// Creates an optimized stream for the given file, using memory mapping for large files
    /// </summary>
    public static Stream CreateOptimizedFileStream(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MemoryMappingThreshold)
        {
            return (Stream?)TryCreateMemoryMappedStream(filePath)
                ?? CreateStandardFileStream(filePath);
        }

        return CreateStandardFileStream(filePath);
    }

    /// <summary>
    /// Copies data from source to destination stream with high-performance buffering
    /// </summary>
    public static async Task<long> CopyWithProgressAsync(
        Stream source,
        Stream destination,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        var totalBytesWritten = 0L;

        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesWritten += bytesRead;
                progress?.Report(totalBytesWritten);
            }

            return totalBytesWritten;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Creates a temporary file from stream data with progress reporting
    /// </summary>
    public static async Task<string> CreateTemporaryFileAsync(
        Stream inputStream,
        string extension = ".dbf",
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var tempFile = Path.GetTempFileName();
        var tempDir = Path.GetDirectoryName(tempFile) ?? Path.GetTempPath();
        var tempFileName = Path.GetFileNameWithoutExtension(tempFile) + extension;
        var tempDbfFile = Path.Combine(tempDir, tempFileName);

        try
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            await using var outputStream = new FileStream(
                tempDbfFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                DefaultBufferSize,
                FileOptions.WriteThrough | FileOptions.RandomAccess
            );

            var totalBytes = await CopyWithProgressAsync(
                inputStream,
                outputStream,
                progress,
                cancellationToken
            );
            await outputStream.FlushAsync(cancellationToken);

            if (totalBytes < MinimumDataThreshold)
            {
                throw new InvalidOperationException(
                    $"Insufficient data received ({totalBytes} bytes). Minimum required: {MinimumDataThreshold} bytes."
                );
            }

            return tempDbfFile;
        }
        catch (Exception)
        {
            CleanupFile(tempFile);
            CleanupFile(tempDbfFile);
            throw;
        }
    }

    /// <summary>
    /// Estimates the size of a stream if possible without consuming it
    /// </summary>
    public static long? EstimateSize(Stream stream)
    {
        try
        {
            return stream.CanSeek ? stream.Length : null;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Validates that a stream contains sufficient data for processing
    /// </summary>
    public static void ValidateStreamData(Stream stream, long bytesRead)
    {
        if (bytesRead < MinimumDataThreshold)
        {
            throw new InvalidOperationException(
                $"Insufficient data in stream ({bytesRead} bytes). Minimum required: {MinimumDataThreshold} bytes."
            );
        }
    }

    /// <summary>
    /// Safely cleans up a temporary file, ignoring common cleanup errors
    /// </summary>
    public static void CleanupTemporaryFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        CleanupFile(filePath);
    }

    private static MemoryMappedViewStream? TryCreateMemoryMappedStream(string filePath)
    {
        try
        {
            var mmf = MemoryMappedFile.CreateFromFile(
                path: filePath,
                mode: FileMode.Open,
                mapName: "dbf_read",
                capacity: 0,
                access: MemoryMappedFileAccess.Read
            );

            return mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static FileStream CreateStandardFileStream(string filePath)
    {
        return new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            DefaultBufferSize,
            FileOptions.SequentialScan
        );
    }

    private static void CleanupFile(string filePath)
    {
        try
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // temp files will be cleaned up by OS eventually
        }
    }
}
