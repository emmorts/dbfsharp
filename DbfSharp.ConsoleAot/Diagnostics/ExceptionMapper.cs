namespace DbfSharp.ConsoleAot.Diagnostics;

/// <summary>
/// Standardized error handling and exit codes for the CLI application
/// </summary>
public static class ExceptionMapper
{
    /// <summary>
    /// Standard CLI exit codes following Unix conventions
    /// </summary>
    public static class ExitCodes
    {
        public const int Success = 0;
        public const int GeneralError = 1;
        public const int FileNotFound = 2;
        public const int InvalidArgument = 22;
        public const int AccessDenied = 13;
        public const int OperationCancelled = 130;
        public const int OutOfMemory = 137;
        public const int InvalidFileFormat = 65;
        public const int CorruptedData = 66;
    }

    /// <summary>
    /// Handles exceptions in a consistent manner with appropriate exit codes and error messages
    /// </summary>
    /// <param name="ex">The exception to handle</param>
    /// <param name="context">Additional context about where the error occurred</param>
    /// <param name="verbose">Whether to include detailed error information</param>
    /// <returns>The appropriate exit code</returns>
    public static async Task<int> HandleExceptionAsync(Exception ex, string context, bool verbose = false)
    {
        var (exitCode, message, suggestion) = MapExceptionToErrorInfo(ex, context);

        await Console.Error.WriteLineAsync($"Error: {message}");

        if (!string.IsNullOrEmpty(suggestion))
        {
            await Console.Error.WriteLineAsync($"Suggestion: {suggestion}");
        }

        if (verbose && ex.StackTrace != null)
        {
            await Console.Error.WriteLineAsync("\nStack trace:");
            await Console.Error.WriteLineAsync(ex.StackTrace);
        }

        if (verbose)
        {
            await Console.Error.WriteLineAsync("\nDiagnostic Information:");
            await Console.Error.WriteLineAsync($"  Exception Type: {ex.GetType().Name}");
            await Console.Error.WriteLineAsync($"  Context: {context}");
            await Console.Error.WriteLineAsync($"  Process ID: {Environment.ProcessId}");
            await Console.Error.WriteLineAsync($"  Working Directory: {Environment.CurrentDirectory}");
            await Console.Error.WriteLineAsync($"  Runtime Version: {Environment.Version}");

            if (ex.InnerException != null)
            {
                await Console.Error.WriteLineAsync($"  Inner Exception: {ex.InnerException.GetType().Name}");
                await Console.Error.WriteLineAsync($"  Inner Message: {ex.InnerException.Message}");
            }
        }

        return exitCode;
    }

    /// <summary>
    /// Maps exceptions to appropriate exit codes, user-friendly error messages, and helpful suggestions
    /// </summary>
    private static (int exitCode, string message, string? suggestion) MapExceptionToErrorInfo(Exception ex,
        string context)
    {
        return ex switch
        {
            OperationCanceledException =>
                (ExitCodes.OperationCancelled, "Operation was cancelled by user request.", null),

            FileNotFoundException fileEx =>
                (ExitCodes.FileNotFound,
                    $"File not found: {fileEx.FileName ?? "Unknown file"}",
                    "Verify the file path and ensure the file exists."),

            DirectoryNotFoundException dirEx =>
                (ExitCodes.FileNotFound,
                    $"Directory not found: {dirEx.Message}",
                    "Check the directory path and ensure it exists."),

            UnauthorizedAccessException =>
                (ExitCodes.AccessDenied,
                    "Access denied. Insufficient permissions to access the file or directory.",
                    "Check file permissions or run with appropriate privileges."),

            ArgumentNullException nullEx =>
                (ExitCodes.InvalidArgument,
                    $"Required argument is missing: {nullEx.ParamName}",
                    "Provide all required arguments or use --help for usage information."),

            ArgumentException argEx =>
                (ExitCodes.InvalidArgument,
                    $"Invalid argument: {argEx.Message}",
                    "Check argument format and try again."),

            OutOfMemoryException =>
                (ExitCodes.OutOfMemory,
                    "Insufficient memory to complete the operation.",
                    "Try processing smaller files, use --limit to reduce records, or increase available memory."),

            InvalidDataException =>
                (ExitCodes.InvalidFileFormat,
                    "Invalid or corrupted DBF file format.",
                    "Verify this is a valid DBF file and not corrupted."),

            EndOfStreamException =>
                (ExitCodes.CorruptedData,
                    "Unexpected end of file while reading DBF data.",
                    "The file may be truncated or corrupted. Try with a backup copy."),

            IOException ioEx when ioEx.Message.Contains("disk", StringComparison.OrdinalIgnoreCase) =>
                (ExitCodes.GeneralError,
                    "Disk space or I/O error occurred.",
                    "Check available disk space and file system health."),

            IOException ioEx when ioEx.Message.Contains("network", StringComparison.OrdinalIgnoreCase) =>
                (ExitCodes.GeneralError,
                    "Network I/O error occurred.",
                    "Check network connection and try again."),

            IOException ioEx =>
                (ExitCodes.GeneralError,
                    $"I/O error: {ioEx.Message}",
                    "Verify file accessibility and system resources."),

            NotSupportedException notSupportedEx =>
                (ExitCodes.InvalidFileFormat,
                    $"Unsupported feature or file format: {notSupportedEx.Message}",
                    "This DBF variant or feature is not currently supported."),

            TimeoutException =>
                (ExitCodes.GeneralError,
                    "Operation timed out.",
                    "Try with a smaller file or increase timeout settings."),

            _ => (ExitCodes.GeneralError,
                $"An unexpected error occurred while {context}: {ex.Message}",
                "Use --verbose for more details or report this issue if it persists.")
        };
    }
}
