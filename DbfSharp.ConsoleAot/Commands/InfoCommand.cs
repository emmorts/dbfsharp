using System.Text;
using ConsoleAppFramework;
using DbfSharp.ConsoleAot.Commands.Configuration;
using DbfSharp.ConsoleAot.Diagnostics;
using DbfSharp.ConsoleAot.Input;
using DbfSharp.ConsoleAot.Output.Table;
using DbfSharp.ConsoleAot.Text;
using DbfSharp.ConsoleAot.Validation;
using DbfSharp.Core;
using DbfSharp.Core.Enums;

namespace DbfSharp.ConsoleAot.Commands;

/// <summary>
/// Handles the info command for displaying DBF file metadata and structure information
/// </summary>
public static class InfoCommand
{
    /// <summary>
    /// Displays detailed DBF file metadata and structure information.
    /// </summary>
    /// <param name="filePath">Path to the DBF file to analyze (omit to read from stdin).</param>
    /// <param name="fields">Show field definitions table.</param>
    /// <param name="header">Show header information table.</param>
    /// <param name="stats">Show record statistics table.</param>
    /// <param name="memo">Show memo file information.</param>
    /// <param name="verbose">-v, Show additional detailed information, including sample data.</param>
    /// <param name="quiet">-q, Suppress all informational output except for errors.</param>
    /// <param name="encoding">Override character encoding (e.g., "UTF-8", "Windows-1252").</param>
    /// <param name="ignoreMissingMemo">Don't fail if the memo file is missing.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    [Command("info")]
    public static async Task<int> ExecuteAsync(
        [Argument] string? filePath = null,
        bool fields = true,
        bool header = true,
        bool stats = true,
        bool memo = true,
        bool verbose = false,
        bool quiet = false,
        string? encoding = null,
        bool ignoreMissingMemo = true,
        CancellationToken cancellationToken = default)
    {
        var settings = new InfoConfiguration
        {
            FilePath = filePath,
            ShowFields = fields,
            ShowHeader = header,
            ShowStats = stats,
            ShowMemo = memo,
            Verbose = verbose,
            Quiet = quiet,
            Encoding = encoding,
            IgnoreMissingMemo = ignoreMissingMemo
        };

        var validationResult = ArgumentValidator.ValidateInfoArguments(settings.FilePath);
        if (!validationResult.IsValid)
        {
            await Console.Error.WriteLineAsync(validationResult.ErrorMessage);

            if (string.IsNullOrEmpty(validationResult.Suggestion))
            {
                await Console.Error.WriteLineAsync("Use --help for more information.");
            }
            else
            {
                await Console.Error.WriteLineAsync($"Suggestion: {validationResult.Suggestion}");
            }

            return ExceptionMapper.ExitCodes.InvalidArgument;
        }

        using var performanceMonitor = verbose ? new PerformanceProfiler("Analyze DBF File") : null;

        try
        {
            using var inputSource = await InputResolver.ResolveAsync(settings.FilePath);

            if (!settings.Quiet)
            {
                Console.WriteLine($"Analyzing DBF file: {inputSource.GetDisplayName()}\n");
            }

            var readerOptions = CreateDbfReaderOptions(settings);
            var tableName = inputSource.IsStdin ? "stdin" : Path.GetFileNameWithoutExtension(inputSource.OriginalPath);

            await using var reader =
                await DbfReader.CreateAsync(inputSource.Stream, readerOptions, cancellationToken);

            // Subscribe to warnings if not in quiet mode
            if (!settings.Quiet)
            {
                reader.Warning += (sender, e) => Console.WriteLine($"Warning: {e.Message}");
            }

            var dbfStats = reader.GetStatistics();

            var fileSize = inputSource.GetFileSize();
            var expectedSize = FileSize.CalculateExpectedSize(reader);

            if (settings.ShowHeader)
            {
                DisplayHeaderInformation(reader, fileSize, expectedSize);
            }

            if (settings.ShowFields)
            {
                DisplayFieldInformation(reader, settings.Verbose);
            }

            if (settings is { ShowStats: true, Verbose: true })
            {
                await DisplaySampleDataAsync(reader, cancellationToken);
            }

            if (settings.ShowMemo && dbfStats.HasMemoFile)
            {
                DisplayMemoFileInformation(reader);
            }

            if (verbose && fileSize.HasValue)
            {
                DisplayPerformanceInformation(fileSize.Value, expectedSize);
            }

            return ExceptionMapper.ExitCodes.Success;
        }
        catch (Exception ex)
        {
            return await ExceptionMapper.HandleExceptionAsync(ex, "analyzing DBF file", settings.Verbose);
        }
    }

    private static DbfReaderOptions CreateDbfReaderOptions(InfoConfiguration settings)
    {
        var options = new DbfReaderOptions
        {
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = true
        };

        var validationResult = ArgumentValidator.ValidateEncoding(settings.Encoding);
        if (validationResult.IsValid && !string.IsNullOrEmpty(settings.Encoding))
        {
            options = options with { Encoding = EncodingResolver.Resolve(settings.Encoding) };
        }
        else if (!validationResult.IsValid)
        {
            if (settings.Quiet)
            {
                return options;
            }

            Console.WriteLine($"Warning: {validationResult.ErrorMessage}");

            if (!string.IsNullOrEmpty(validationResult.Suggestion))
            {
                Console.WriteLine(validationResult.Suggestion);
            }
        }

        return options;
    }

    private static void DisplayHeaderInformation(DbfReader reader, long? actualFileSize, long expectedFileSize)
    {
        var header = reader.Header;
        var table = new ConsoleTableWriter("Header Information", "Property", "Value", "Details");
        table.AddRow("DBF Version", header.DbfVersion.GetDescription(), $"Byte: 0x{header.DbVersionByte:X2}");
        table.AddRow("Last Update", header.LastUpdateDate?.ToString("yyyy-MM-dd") ?? "Unknown",
            $"Raw: {header.Year:00}/{header.Month:00}/{header.Day:00}");
        table.AddRow("Header Length", $"{header.HeaderLength} bytes",
            $"Fields area: {header.HeaderLength - DbfHeader.Size - 1} bytes");
        table.AddRow("Record Length", $"{header.RecordLength} bytes", "Including deletion flag");
        table.AddRow("Total Records", header.NumberOfRecords.ToString("N0"), "Including deleted records");
        table.AddRow("MDX Index", header.MdxFlag != 0 ? "Present" : "None", $"Flag: 0x{header.MdxFlag:X2}");
        table.AddRow("Encryption", header.EncryptionFlag != 0 ? "Encrypted" : "None",
            $"Flag: 0x{header.EncryptionFlag:X2}");
        table.AddRow("Language Driver", header.EncodingDescription, $"Code: 0x{header.LanguageDriver:X2}");

        if (actualFileSize.HasValue)
        {
            table.AddRow("File Size", FileSize.Format(actualFileSize.Value), $"{actualFileSize.Value:N0} bytes");
        }
        else
        {
            table.AddRow("Expected File Size", FileSize.Format(expectedFileSize), $"{expectedFileSize:N0} bytes");
        }

        table.Print(TableBorderStyles.Rounded);
    }

    private static void DisplayFieldInformation(DbfReader reader, bool verbose)
    {
        var headers = verbose
            ? new[] { "#", "Name", "Type", "Length", "Decimals", "Flags" }
            : new[] { "#", "Name", "Type", "Length", "Decimals" };
        var table = new ConsoleTableWriter("\nField Definitions", headers);

        var fieldIndex = 1;
        foreach (var field in reader.Fields)
        {
            var row = new List<string>
            {
                fieldIndex.ToString(),
                field.Name,
                $"{field.Type} ({(char)field.Type})",
                field.ActualLength.ToString(),
                field.Type is FieldType.Numeric or FieldType.Float ? field.ActualDecimalCount.ToString() : "-"
            };

            if (verbose)
            {
                var flags = new List<string>();
                if (field.UsesMemoFile)
                {
                    flags.Add("Memo");
                }

                if (field.IndexFieldFlag != 0)
                {
                    flags.Add("Index");
                }

                if (field.SetFieldsFlag != 0)
                {
                    flags.Add("Set");
                }

                row.Add(string.Join(", ", flags));
            }

            table.AddRow(row.ToArray());
            fieldIndex++;
        }

        table.Print(TableBorderStyles.Rounded);
    }


    private static async Task DisplaySampleDataAsync(DbfReader reader, CancellationToken cancellationToken)
    {
        try
        {
            var sampleRecords = new List<DbfRecord>();
            var count = 0;
            await foreach (var record in reader.ReadRecordsAsync(cancellationToken))
            {
                if (count >= 3)
                {
                    break;
                }

                sampleRecords.Add(record);
                count++;
            }

            if (sampleRecords.Count == 0)
            {
                Console.WriteLine("\nNo records to display.");
                return;
            }

            var fieldsToShow = reader.FieldNames.Take(5).ToArray();
            var tableHeaders = new List<string>(fieldsToShow);
            if (reader.FieldNames.Count > 5)
            {
                tableHeaders.Add("...");
            }

            var table = new ConsoleTableWriter("\nSample Data (first 3 records)", tableHeaders.ToArray());

            foreach (var record in sampleRecords)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = new List<string>();
                foreach (var fieldName in fieldsToShow)
                {
                    values.Add(FormatSampleValue(record[fieldName]));
                }

                if (reader.FieldNames.Count > 5)
                {
                    values.Add($"+{reader.FieldNames.Count - 5} more");
                }

                table.AddRow(values.ToArray());
            }

            table.Print(TableBorderStyles.Rounded);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nWarning: Could not display sample data: {ex.Message}");
        }
    }

    private static void DisplayMemoFileInformation(DbfReader reader)
    {
        var stats = reader.GetStatistics();
        var table = new ConsoleTableWriter("\nMemo File Information", "Property", "Value");
        table.AddRow("Memo File", stats.MemoFilePath ?? "Present");
        table.AddRow("Status", stats.HasMemoFile ? "Available" : "Missing");
        table.Print(TableBorderStyles.Rounded);
    }

    private static void DisplayPerformanceInformation(long actualFileSize, long expectedFileSize)
    {
        var table = new ConsoleTableWriter("\nPerformance Information", "Metric", "Value", "Details");

        var memoryEstimate = FileSize.EstimateMemoryUsage(actualFileSize);
        table.AddRow("Estimated Memory Usage", FileSize.Format(memoryEstimate), "Approximate processing requirement");

        var percentageDiff = FileSize.CalculatePercentageDifference(actualFileSize, expectedFileSize);
        table.AddRow("Size Difference", $"{percentageDiff:F1}%",
            percentageDiff == 0 ? "Perfect match" : "May indicate corruption");

        var isWithinBounds = FileSize.IsWithinProcessingBounds(actualFileSize);
        table.AddRow("Processing Recommendation", isWithinBounds ? "Optimal" : "Large file",
            isWithinBounds ? "Good performance expected" : "Consider using --limit for faster processing");

        table.Print(TableBorderStyles.Rounded);

        var performanceWarning = FileSize.CreatePerformanceWarning(actualFileSize);
        if (performanceWarning != null)
        {
            Console.WriteLine($"\n⚠️  {performanceWarning}");
        }
    }

    private static string FormatSampleValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s when string.IsNullOrWhiteSpace(s) => "<empty>",
            string { Length: > 20 } s => s[..17] + "...",
            string s => s,
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            bool b => b ? "True" : "False",
            Core.Parsing.InvalidValue => "<invalid>",
            byte[] bytes => $"<{bytes.Length} bytes>",
            _ => value.ToString() ?? "NULL"
        };
    }
}
