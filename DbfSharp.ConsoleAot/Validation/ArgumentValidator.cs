using DbfSharp.ConsoleAot.Text;

namespace DbfSharp.ConsoleAot.Validation;

/// <summary>
/// Validates command-line arguments with detailed error reporting and helpful suggestions
/// </summary>
public static class ArgumentValidator
{
    /// <summary>
    /// Validates arguments for the read command
    /// </summary>
    /// <param name="filePath">The input file path</param>
    /// <param name="outputPath">The output file path</param>
    /// <param name="limit">Maximum number of records to process</param>
    /// <param name="skip">Number of records to skip</param>
    /// <returns>Validation result with error details if invalid</returns>
    public static ValidationResult ValidateReadArguments(
        string? filePath,
        string? outputPath,
        int? limit,
        int skip
    )
    {
        var inputValidation = ValidateInputSource(filePath);
        if (!inputValidation.IsValid)
        {
            return inputValidation;
        }

        var outputValidation = ValidateOutputPath(outputPath);
        if (!outputValidation.IsValid)
        {
            return outputValidation;
        }

        var limitValidation = ValidateLimit(limit);
        if (!limitValidation.IsValid)
        {
            return limitValidation;
        }

        var skipValidation = ValidateSkip(skip);
        if (!skipValidation.IsValid)
        {
            return skipValidation;
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates arguments for the info command
    /// </summary>
    /// <param name="filePath">The input file path</param>
    /// <returns>Validation result with error details if invalid</returns>
    public static ValidationResult ValidateInfoArguments(string? filePath)
    {
        return ValidateInputSource(filePath);
    }

    /// <summary>
    /// Validates the input source (file path or stdin availability)
    /// </summary>
    private static ValidationResult ValidateInputSource(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            return ValidateInputFile(filePath);
        }

        if (!Console.IsInputRedirected)
        {
            return ValidationResult.Failure(
                "No input file specified and no data available from stdin.",
                "Provide a file path or pipe data to stdin (e.g., 'cat file.dbf | dbfsharp read')."
            );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a specific input file path
    /// </summary>
    private static ValidationResult ValidateInputFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return ValidationResult.Failure(
                    $"File '{filePath}' does not exist.",
                    "Verify the file path and ensure the file exists."
                );
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return ValidationResult.Failure(
                    $"File '{filePath}' is empty.",
                    "Ensure the file contains valid DBF data."
                );
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!IsKnownDbfExtension(extension))
            {
                return ValidationResult.Failure(
                    $"File '{filePath}' does not have a recognized DBF extension.",
                    "DBF files typically have extensions like .dbf, .dbt, .ndx, or .mdx."
                );
            }

            return ValidationResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Failure(
                $"Access denied to file '{filePath}'.",
                "Check file permissions or run with appropriate privileges."
            );
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException)
        {
            return ValidationResult.Failure(
                $"Cannot access file '{filePath}': {ex.Message}",
                "Verify the file path is valid and accessible."
            );
        }
    }

    /// <summary>
    /// Validates the output file path
    /// </summary>
    private static ValidationResult ValidateOutputPath(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return ValidationResult.Success();
        }

        try
        {
            var fullPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                return ValidationResult.Failure(
                    $"Output directory '{directory}' does not exist.",
                    "Create the directory or specify a different output path."
                );
            }

            if (File.Exists(fullPath))
            {
                return ValidateExistingOutputFile(fullPath);
            }

            return ValidateNewOutputFile(directory);
        }
        catch (ArgumentException)
        {
            return ValidationResult.Failure(
                $"Invalid output path '{outputPath}'.",
                "Ensure the path contains only valid characters and is not too long."
            );
        }
        catch (Exception ex) when (ex is NotSupportedException or PathTooLongException)
        {
            return ValidationResult.Failure(
                $"Invalid output path '{outputPath}': {ex.Message}",
                "Use a shorter path or avoid special characters."
            );
        }
    }

    /// <summary>
    /// Validates that we can write to an existing output file
    /// </summary>
    private static ValidationResult ValidateExistingOutputFile(string fullPath)
    {
        try
        {
            using var testStream = File.OpenWrite(fullPath);
            return ValidationResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Failure(
                $"Cannot write to output file '{fullPath}': Access denied.",
                "Check file permissions or choose a different output location."
            );
        }
        catch (IOException ex)
        {
            return ValidationResult.Failure(
                $"Cannot write to output file '{fullPath}': {ex.Message}",
                "Ensure the file is not in use by another process."
            );
        }
    }

    /// <summary>
    /// Validates that we can create a new file in the specified directory
    /// </summary>
    private static ValidationResult ValidateNewOutputFile(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return ValidationResult.Success();
        }

        try
        {
            var testFile = Path.Combine(directory, Path.GetRandomFileName());
            using (File.Create(testFile)) { }
            File.Delete(testFile);
            return ValidationResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Failure(
                $"Cannot create files in directory '{directory}': Access denied.",
                "Check directory permissions or choose a different location."
            );
        }
        catch (IOException ex)
        {
            return ValidationResult.Failure(
                $"Cannot create files in directory '{directory}': {ex.Message}",
                "Ensure the directory is writable and has sufficient space."
            );
        }
    }

    /// <summary>
    /// Validates the limit parameter
    /// </summary>
    private static ValidationResult ValidateLimit(int? limit)
    {
        if (limit is < 0)
        {
            return ValidationResult.Failure(
                "Limit must be a non-negative number.",
                "Use 0 or a positive number, or omit --limit to show all records."
            );
        }

        if (limit == 0)
        {
            return ValidationResult.Failure(
                "Limit cannot be zero.",
                "Use a positive number or omit --limit to show all records."
            );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates the skip parameter
    /// </summary>
    private static ValidationResult ValidateSkip(int skip)
    {
        if (skip < 0)
        {
            return ValidationResult.Failure(
                "Skip must be a non-negative number.",
                "Use 0 or a positive number to skip records from the beginning."
            );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Checks if a file extension is commonly associated with DBF files
    /// </summary>
    private static bool IsKnownDbfExtension(string extension)
    {
        var knownExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dbf",
            ".dbt",
            ".ndx",
            ".mdx",
            ".fpt",
            ".cdx",
        };

        return knownExtensions.Contains(extension);
    }

    /// <summary>
    /// Validates encoding parameter and provides helpful feedback
    /// </summary>
    /// <param name="encodingName">The encoding name to validate</param>
    /// <returns>Validation result with encoding-specific guidance</returns>
    public static ValidationResult ValidateEncoding(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            return ValidationResult.Success();
        }

        var encoding = EncodingResolver.TryResolve(encodingName);
        if (encoding is null)
        {
            var message = $"Encoding '{encodingName}' is not supported or the name is misspelled. ";

            return ValidationResult.Failure(
                message,
                "Common examples include: UTF-8, Windows-1252 (ANSI), IBM437 (DOS)."
            );
        }

        return ValidationResult.Success();
    }
}
