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
    /// Determines the appropriate file path to use, either from argument or stdin
    /// </summary>
    /// <param name="filePath">The file path argument (may be null or empty)</param>
    /// <returns>A tuple containing the file path to use and whether it's a temporary file</returns>
    public static async Task<(string FilePath, bool IsTemporary)> ResolveFilePathAsync(
        string? filePath
    )
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            return (filePath, false);
        }

        if (!IsStdinAvailable())
        {
            throw new InvalidOperationException(
                "No input file specified and no data available from stdin. "
                + "Provide a file path or pipe data to stdin (e.g., 'cat file.dbf | dbfsharp read')."
            );
        }

        var tempFile = await CreateTemporaryFileFromStdinAsync();

        return (tempFile, true);
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
    /// Reads all data from stdin as a byte array
    /// </summary>
    /// <returns>Byte array containing stdin data</returns>
    private static async Task<byte[]> ReadStdinAsBytesAsync()
    {
        await using var stdin = System.Console.OpenStandardInput();
        using var memoryStream = new MemoryStream();

        await stdin.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Creates a temporary file with the data from stdin and returns the path
    /// </summary>
    /// <param name="extension">File extension to use for the temporary file (default: .dbf)</param>
    /// <returns>Path to the temporary file</returns>
    private static async Task<string> CreateTemporaryFileFromStdinAsync(string extension = ".dbf")
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

            var stdinData = await ReadStdinAsBytesAsync();
            if (stdinData.Length == 0)
            {
                throw new InvalidOperationException("No data received from stdin");
            }

            await File.WriteAllBytesAsync(tempDbfFile, stdinData);
            return tempDbfFile;
        }
        catch
        {
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
}
