using DbfSharp.ConsoleAot.Text;

namespace DbfSharp.ConsoleAot.Input;

/// <summary>
/// Resolves input sources from file paths or stdin with optimized stream handling
/// </summary>
public static class InputResolver
{
    private const int MinimumDataThreshold = 32;
    private const long MaxMemoryStreamSize = 256 * 1024 * 1024;

    /// <summary>
    /// Determines the appropriate input source to use, either from file path or stdin
    /// </summary>
    /// <param name="filePath">The file path argument (may be null or empty)</param>
    /// <returns>A result containing the input stream and cleanup information</returns>
    public static async Task<InputSource> ResolveAsync(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            return CreateFileInputSource(filePath);
        }

        if (!IsStdinAvailable())
        {
            throw new InvalidOperationException(
                "No input file specified and no data available from stdin. "
                    + "Provide a file path or pipe data to stdin (e.g., 'cat file.dbf | dbfsharp read')."
            );
        }

        return await CreateStdinInputSourceAsync();
    }

    /// <summary>
    /// Creates an input source from a file path with validation and optimization
    /// </summary>
    private static FileInputSource CreateFileInputSource(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The specified file does not exist: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            throw new InvalidOperationException($"The specified file is empty: {filePath}");
        }

        var stream = StreamUtilities.CreateOptimizedFileStream(filePath);
        return new FileInputSource(stream, filePath);
    }

    /// <summary>
    /// Checks if stdin has data available with improved detection
    /// </summary>
    /// <returns>True if stdin has data available</returns>
    private static bool IsStdinAvailable()
    {
        try
        {
            if (!Console.IsInputRedirected)
            {
                return false;
            }

            var stdin = Console.OpenStandardInput();
            if (stdin.CanSeek)
            {
                return stdin.Length > 0;
            }

            return true;
        }
        catch (Exception ex)
            when (ex is IOException or InvalidOperationException or ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an input source from stdin with adaptive strategy based on data size
    /// </summary>
    private static async Task<InputSource> CreateStdinInputSourceAsync()
    {
        var stdin = Console.OpenStandardInput();

        try
        {
            var estimatedSize = StreamUtilities.EstimateSize(stdin);
            if (estimatedSize is <= MaxMemoryStreamSize)
            {
                return await CreateMemoryBasedInputSourceAsync(stdin);
            }

            return await CreateFileBasedInputSourceAsync(stdin);
        }
        catch (Exception)
        {
            await stdin.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Creates a memory-based input source for smaller stdin data
    /// </summary>
    private static async Task<InputSource> CreateMemoryBasedInputSourceAsync(Stream stdin)
    {
        var memoryStream = new MemoryStream();

        try
        {
            var bytesRead = await StreamUtilities.CopyWithProgressAsync(stdin, memoryStream);
            StreamUtilities.ValidateStreamData(memoryStream, bytesRead);

            memoryStream.Position = 0;
            return new MemoryInputSource(memoryStream, "stdin");
        }
        catch (Exception)
        {
            await memoryStream.DisposeAsync();
            throw;
        }
        finally
        {
            await stdin.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a file-based input source for larger stdin data
    /// </summary>
    private static async Task<InputSource> CreateFileBasedInputSourceAsync(Stream stdin)
    {
        var progress = FileSize.CreateProgressReporter();
        var tempFile = await StreamUtilities.CreateTemporaryFileAsync(stdin, ".dbf", progress);

        await stdin.DisposeAsync();

        var fileStream = new FileStream(
            tempFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 65536,
            FileOptions.DeleteOnClose | FileOptions.SequentialScan
        );

        return new TemporaryFileInputSource(fileStream, "stdin", tempFile);
    }
}
