namespace DbfSharp.Console.Utils;

/// <summary>
/// Utility class for handling stdin input detection and reading
/// </summary>
public static class StdinHelper
{
    /// <summary>
    /// Cleans up a temporary file created by CreateTemporaryFileFromStdinAsync
    /// </summary>
    /// <param name="tempFilePath">Path to the temporary file to delete</param>
    public static void CleanupTemporaryFile(string tempFilePath)
    {
        if (string.IsNullOrWhiteSpace(tempFilePath))
        {
            return;
        }

        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch
        {
            // ignore cleanup errors - temp files will be cleaned up eventually by OS
        }
    }

    /// <summary>
    /// Determines the appropriate input source to use, either from file path or stdin
    /// </summary>
    /// <param name="filePath">The file path argument (may be null or empty)</param>
    /// <returns>A result containing the input stream and cleanup information</returns>
    public static async Task<InputSourceResult> ResolveInputSourceAsync(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 65536, useAsync: true);
            return new InputSourceResult(fileStream, filePath, false, null);
        }

        if (!IsStdinAvailable())
        {
            throw new InvalidOperationException(
                "No input file specified and no data available from stdin. "
                + "Provide a file path or pipe data to stdin (e.g., 'cat file.dbf | dbfsharp read').");
        }

        // For stdin, we have two strategies depending on the use case:
        // 1. Direct stream processing (most efficient)
        // 2. Buffered temporary file (for scenarios requiring random access)

        return await CreateStdinInputSourceAsync();
    }

    /// <summary>
    /// Legacy method maintained for backward compatibility
    /// </summary>
    /// <param name="filePath">The file path argument (may be null or empty)</param>
    /// <returns>A tuple containing the file path to use and whether it's a temporary file</returns>
    [Obsolete("Use ResolveInputSourceAsync for better performance. This method will be removed in a future version.")]
    public static async Task<(string FilePath, bool IsTemporary)> ResolveFilePathAsync(string? filePath)
    {
        var inputSource = await ResolveInputSourceAsync(filePath);

        if (inputSource is { IsStdin: true, TempFilePath: null })
        {
            // Need to create temp file for legacy compatibility
            var tempFile = await CreateTemporaryFileFromStreamAsync(inputSource.Stream);
            await inputSource.Stream.DisposeAsync();
            return (tempFile, true);
        }

        return (inputSource.TempFilePath ?? inputSource.OriginalPath!, inputSource.IsTemporary);
    }

    /// <summary>
    /// Checks if stdin has data available (i.e., data is being piped in)
    /// </summary>
    /// <returns>True if stdin has data available</returns>
    private static bool IsStdinAvailable()
    {
        try
        {
            return System.Console.IsInputRedirected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an input source from stdin with optimal strategy based on requirements
    /// </summary>
    /// <param name="requireRandomAccess">Whether random access to the data is required</param>
    /// <returns>InputSourceResult with appropriate stream configuration</returns>
    private static async Task<InputSourceResult> CreateStdinInputSourceAsync(bool requireRandomAccess = false)
    {
        var stdin = System.Console.OpenStandardInput();

        if (!requireRandomAccess)
        {
            // Direct stream processing - most efficient for sequential access
            var bufferedStream = new BufferedStream(stdin, bufferSize: 65536);
            return new InputSourceResult(bufferedStream, "stdin", true, null);
        }
        else
        {
            // Create memory-mapped temporary file for random access scenarios
            var tempFile = await CreateTemporaryFileFromStreamAsync(stdin, useMemoryMapping: true);
            await stdin.DisposeAsync();

            var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 65536, useAsync: true);
            return new InputSourceResult(fileStream, "stdin", true, tempFile);
        }
    }

    /// <summary>
    /// Creates a temporary file from a stream using efficient streaming copy
    /// </summary>
    /// <param name="inputStream">The stream to copy from</param>
    /// <param name="extension">File extension to use for the temporary file</param>
    /// <param name="useMemoryMapping">Whether to use memory mapping for better performance</param>
    /// <returns>Path to the temporary file</returns>
    private static async Task<string> CreateTemporaryFileFromStreamAsync(
        Stream inputStream,
        string extension = ".dbf",
        bool useMemoryMapping = false)
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

            // Use FileStream with optimal settings for streaming copy
            await using var outputStream = new FileStream(
                tempDbfFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 65536,
                useAsync: true);

            // Stream copy without loading entire content into memory
            await inputStream.CopyToAsync(outputStream, bufferSize: 65536);
            await outputStream.FlushAsync();

            // Verify we actually wrote some data
            if (outputStream.Length == 0)
            {
                throw new InvalidOperationException("No data received from input stream");
            }

            return tempDbfFile;
        }
        catch
        {
            // Cleanup on error
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(tempDbfFile))
                {
                    File.Delete(tempDbfFile);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
            throw;
        }
    }

    /// <summary>
    /// Legacy method maintained for backward compatibility
    /// </summary>
    [Obsolete("Use CreateTemporaryFileFromStreamAsync for better performance")]
    private static async Task<string> CreateTemporaryFileFromStdinAsync(string extension = ".dbf")
    {
        await using var stdin = System.Console.OpenStandardInput();

        return await CreateTemporaryFileFromStreamAsync(stdin, extension);
    }

    /// <summary>
    /// Legacy method maintained for backward compatibility
    /// </summary>
    [Obsolete("Direct stream processing is more efficient than reading entire stdin into memory")]
    private static async Task<byte[]> ReadStdinAsBytesAsync()
    {
        await using var stdin = System.Console.OpenStandardInput();
        using var memoryStream = new MemoryStream();
        await stdin.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
