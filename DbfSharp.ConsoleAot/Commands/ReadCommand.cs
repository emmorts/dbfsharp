using System.Text;
using ConsoleAppFramework;
using DbfSharp.ConsoleAot.Commands.Configuration;
using DbfSharp.ConsoleAot.Diagnostics;
using DbfSharp.ConsoleAot.Formatters;
using DbfSharp.ConsoleAot.Input;
using DbfSharp.ConsoleAot.Output.Table;
using DbfSharp.ConsoleAot.Text;
using DbfSharp.ConsoleAot.Validation;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Geometry;
using DbfSharp.Core.Utils;

namespace DbfSharp.ConsoleAot.Commands;

/// <summary>
/// Handles the read command for displaying DBF file contents
/// </summary>
public static class ReadCommand
{
    /// <summary>
    /// Reads and displays DBF file contents.
    /// </summary>
    /// <param name="filePath">Path to the DBF file to read (omit to read from stdin).</param>
    /// <param name="format">-f, The output format (table, csv, tsv, json).</param>
    /// <param name="output">-o, The output file path (default: stdout).</param>
    /// <param name="limit">-l, Maximum number of records to display.</param>
    /// <param name="skip">-s, Number of records to skip before reading.</param>
    /// <param name="showDeleted">Includes records marked as deleted in the output.</param>
    /// <param name="fields">A comma-separated list of fields to include (e.g., "NAME,AGE").</param>
    /// <param name="verbose">-v, Enables verbose output, including file information.</param>
    /// <param name="quiet">-q, Suppresses all informational output except for data and errors.</param>
    /// <param name="encoding">Overrides the character encoding for reading text fields (e.g., "UTF-8", "Windows-1252").</param>
    /// <param name="ignoreCase">Treats field names as case-insensitive.</param>
    /// <param name="trimStrings">Trims leading and trailing whitespace from string fields.</param>
    /// <param name="ignoreMissingMemo">Prevents failure if a required memo file (.dbt) is missing.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>Returns 0 on success, 1 on error, 2 if the file is not found, or 130 if cancelled.</returns>
    [Command("read")]
    public static async Task<int> ExecuteAsync(
        [Argument] string? filePath = null,
        OutputFormat format = OutputFormat.Table,
        string? output = null,
        int? limit = null,
        int skip = 0,
        bool showDeleted = false,
        string? fields = null,
        bool verbose = false,
        bool quiet = false,
        string? encoding = null,
        bool ignoreCase = true,
        bool trimStrings = true,
        bool ignoreMissingMemo = true,
        CancellationToken cancellationToken = default
    )
    {
        var settings = new ReadConfiguration
        {
            FilePath = filePath,
            Format = format,
            OutputPath = output,
            Limit = limit,
            Skip = skip,
            ShowDeleted = showDeleted,
            Fields = fields,
            Verbose = verbose,
            Quiet = quiet,
            Encoding = encoding,
            IgnoreCase = ignoreCase,
            TrimStrings = trimStrings,
            IgnoreMissingMemo = ignoreMissingMemo,
        };

        var validationResult = ArgumentValidator.ValidateReadArguments(
            settings.FilePath,
            settings.OutputPath,
            settings.Limit,
            settings.Skip
        );
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

        using var performanceMonitor = verbose ? new PerformanceProfiler("Read File") : null;

        try
        {
            // Determine input strategy
            var inputStrategy = InputResolver.DetermineStrategy(settings.FilePath);

            switch (inputStrategy)
            {
                case InputResolutionStrategy.Shapefile:
                    return await ProcessShapefileAsync(settings, cancellationToken);
                case InputResolutionStrategy.DbfOnly:
                case InputResolutionStrategy.Stdin:
                default:
                    return await ProcessDbfOnlyAsync(settings, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return await ExceptionMapper.HandleExceptionAsync(
                ex,
                "reading DBF file",
                settings.Verbose
            );
        }
    }

    /// <summary>
    /// Processes a shapefile input (includes geometry)
    /// </summary>
    private static async Task<int> ProcessShapefileAsync(
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        using var shapefileSource = await InputResolver.ResolveShapefileAsync(settings.FilePath);

        if (!settings.Quiet)
        {
            Console.WriteLine($"Reading shapefile: {shapefileSource.DisplayName}");

            // Display shapefile diagnostics
            var diagnostics = ShapefileDetector.GetDiagnostics(shapefileSource.Components);
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"Info: {diagnostic}");
            }
        }

        // If GeoJSON format is requested or shapefile has geometry, use shapefile processing
        if (settings.Format == OutputFormat.GeoJson || shapefileSource.Components.HasGeometry)
        {
            return await ProcessShapefileWithGeometryAsync(
                shapefileSource,
                settings,
                cancellationToken
            );
        }
        else
        {
            // Fall back to DBF-only processing if no geometry or non-geo format
            return await ProcessDbfOnlyFromShapefileAsync(
                shapefileSource,
                settings,
                cancellationToken
            );
        }
    }

    /// <summary>
    /// Processes a shapefile with geometry data
    /// </summary>
    private static async Task<int> ProcessShapefileWithGeometryAsync(
        ShapefileInputSource shapefileSource,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        if (!shapefileSource.Components.HasGeometry)
        {
            await Console.Error.WriteLineAsync(
                "Error: Shapefile geometry files (.shp/.shx) not found."
            );
            return ExceptionMapper.ExitCodes.FileNotFound;
        }

        try
        {
            using var shapefileReader = shapefileSource.CreateShapefileReader();

            if (settings is { Verbose: true, Quiet: false })
            {
                DisplayShapefileInfo(shapefileReader, shapefileSource);
            }

            var fieldsToDisplay = GetShapefileFieldsToDisplay(settings, shapefileReader);

            // Create features enumerable
            var features = GetShapefileFeaturesToDisplay(
                shapefileReader,
                settings,
                cancellationToken
            );

            await FormatAndOutputShapefileAsync(
                features,
                fieldsToDisplay,
                shapefileReader,
                settings,
                cancellationToken
            );

            return ExceptionMapper.ExitCodes.Success;
        }
        catch (Exception ex)
        {
            return await ExceptionMapper.HandleExceptionAsync(
                ex,
                "reading shapefile",
                settings.Verbose
            );
        }
    }

    /// <summary>
    /// Processes DBF-only data from a shapefile source
    /// </summary>
    private static async Task<int> ProcessDbfOnlyFromShapefileAsync(
        ShapefileInputSource shapefileSource,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        if (shapefileSource.DbfSource == null)
        {
            await Console.Error.WriteLineAsync("Error: DBF file not found in shapefile dataset.");
            return ExceptionMapper.ExitCodes.FileNotFound;
        }

        using var reader = CreateDbfReader(shapefileSource.DbfSource.Stream, settings);

        if (settings is { Verbose: true, Quiet: false })
        {
            DisplayFileInfo(reader, shapefileSource.DbfSource);
        }

        var fieldsToDisplay = GetFieldsToDisplay(settings, reader);
        if (fieldsToDisplay.Length == 0)
        {
            await Console.Error.WriteLineAsync("Error: No valid fields to display.");
            return ExceptionMapper.ExitCodes.InvalidArgument;
        }

        if (settings.Format == OutputFormat.Table)
        {
            var records = GetRecordsToDisplay(reader, settings);
            await FormatAndOutputAsync(
                records,
                fieldsToDisplay,
                reader,
                settings,
                cancellationToken
            );
        }
        else
        {
            await FormatAndOutputStreamingAsync(
                reader,
                fieldsToDisplay,
                settings,
                cancellationToken
            );
        }

        return ExceptionMapper.ExitCodes.Success;
    }

    /// <summary>
    /// Processes DBF-only input (no associated geometry)
    /// </summary>
    private static async Task<int> ProcessDbfOnlyAsync(
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        using var inputSource = await InputResolver.ResolveAsync(settings.FilePath);

        if (!settings.Quiet)
        {
            Console.WriteLine($"Reading DBF file: {inputSource.GetDisplayName()}");

            var fileSize = inputSource.GetFileSize();
            if (fileSize.HasValue)
            {
                var warning = FileSize.CreatePerformanceWarning(fileSize.Value);
                if (warning != null)
                {
                    Console.WriteLine($"⚠️  {warning}");
                }
            }
        }

        using var reader = CreateDbfReader(inputSource.Stream, settings);

        if (settings is { Verbose: true, Quiet: false })
        {
            DisplayFileInfo(reader, inputSource);
        }

        var fieldsToDisplay = GetFieldsToDisplay(settings, reader);
        if (fieldsToDisplay.Length == 0)
        {
            await Console.Error.WriteLineAsync("Error: No valid fields to display.");
            return ExceptionMapper.ExitCodes.InvalidArgument;
        }

        if (settings.Format == OutputFormat.Table)
        {
            var records = GetRecordsToDisplay(reader, settings);
            await FormatAndOutputAsync(
                records,
                fieldsToDisplay,
                reader,
                settings,
                cancellationToken
            );
        }
        else
        {
            await FormatAndOutputStreamingAsync(
                reader,
                fieldsToDisplay,
                settings,
                cancellationToken
            );
        }

        return ExceptionMapper.ExitCodes.Success;
    }

    /// <summary>
    /// Gets fields to display for shapefile features
    /// </summary>
    private static string[] GetShapefileFieldsToDisplay(
        ReadConfiguration settings,
        ShapefileReader shapefileReader
    )
    {
        if (!shapefileReader.HasAttributes || shapefileReader.DbfReader == null)
        {
            return Array.Empty<string>();
        }

        var dbfReader = shapefileReader.DbfReader;

        if (string.IsNullOrWhiteSpace(settings.Fields))
        {
            return dbfReader.FieldNames.ToArray();
        }

        var selectedFields = settings
            .Fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToArray();

        var validFields = new List<string>();
        var invalidFields = new List<string>();

        foreach (var fieldName in selectedFields)
        {
            if (dbfReader.HasField(fieldName))
            {
                validFields.Add(fieldName);
            }
            else
            {
                invalidFields.Add(fieldName);
            }
        }

        if (invalidFields.Count > 0 && !settings.Quiet)
        {
            var fieldList = string.Join(", ", invalidFields.Select(f => $"'{f}'"));
            Console.WriteLine($"Warning: Fields not found: {fieldList}");

            if (validFields.Count == 0)
            {
                Console.WriteLine("Available fields: " + string.Join(", ", dbfReader.FieldNames));
            }
        }

        return validFields.ToArray();
    }

    /// <summary>
    /// Gets shapefile features to display based on configuration
    /// </summary>
    private static IEnumerable<ShapefileFeature> GetShapefileFeaturesToDisplay(
        ShapefileReader shapefileReader,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        var count = 0;
        var skipped = 0;

        foreach (var feature in shapefileReader.Features)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (skipped < settings.Skip)
            {
                skipped++;
                continue;
            }

            if (settings.Limit.HasValue && count >= settings.Limit.Value)
            {
                break;
            }

            yield return feature;
            count++;
        }
    }

    /// <summary>
    /// Formats and outputs shapefile features
    /// </summary>
    private static async Task FormatAndOutputShapefileAsync(
        IEnumerable<ShapefileFeature> features,
        string[] fields,
        ShapefileReader shapefileReader,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        var formatter = FormatterFactory.CreateFormatter(settings.Format, settings);

        if (settings.OutputPath != null)
        {
            var directory = Path.GetDirectoryName(settings.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileWriter = new StreamWriter(
                settings.OutputPath,
                false,
                Encoding.UTF8,
                bufferSize: 65536
            );

            if (formatter is GeoJsonFormatter geoJsonFormatter)
            {
                await geoJsonFormatter.WriteAsync(
                    features,
                    fields,
                    shapefileReader,
                    fileWriter,
                    cancellationToken
                );
            }
            else
            {
                // Convert features to DBF records for non-GeoJSON formats
                var records = features
                    .Where(f => f.HasAttributes)
                    .Select(f => f.Attributes!.Value)
                    .ToList();

                // For non-GeoJSON formats, we need to create a mock DbfReader
                // This is a limitation - we'll require GeoJSON for full shapefile output
                await Console.Error.WriteLineAsync(
                    "Error: Non-GeoJSON output for shapefiles not yet supported."
                );
                return;
            }

            if (!settings.Quiet)
            {
                var outputFileInfo = new FileInfo(settings.OutputPath);
                Console.WriteLine(
                    $"Output written to: {settings.OutputPath} ({FileSize.Format(outputFileInfo.Length)})"
                );
            }
        }
        else
        {
            if (formatter is GeoJsonFormatter geoJsonFormatter)
            {
                await geoJsonFormatter.WriteAsync(
                    features,
                    fields,
                    shapefileReader,
                    Console.Out,
                    cancellationToken
                );
            }
            else
            {
                await Console.Error.WriteLineAsync(
                    "Error: Non-GeoJSON output for shapefiles requires file output."
                );
            }
        }
    }

    /// <summary>
    /// Displays shapefile information
    /// </summary>
    private static void DisplayShapefileInfo(
        ShapefileReader shapefileReader,
        ShapefileInputSource inputSource
    )
    {
        var table = new ConsoleTableWriter("\nShapefile Information", "Property", "Value");

        // Basic shapefile info
        table.AddRow("Dataset Name", inputSource.DisplayName);
        table.AddRow("Shape Type", shapefileReader.ShapeType.GetDescription());
        table.AddRow("Total Features", shapefileReader.RecordCount.ToString("N0"));
        table.AddRow("Bounding Box", shapefileReader.BoundingBox.ToString());

        // Component file info
        var components = inputSource.Components;
        table.AddRow("Components", $"{components.ComponentCount} files detected");

        if (components.HasGeometry)
        {
            table.AddRow("Geometry Files", "SHP + SHX");
        }

        if (components.HasAttributes && shapefileReader.DbfReader != null)
        {
            table.AddRow(
                "Attributes",
                $"DBF ({shapefileReader.DbfReader.FieldNames.Count} fields)"
            );
        }

        if (components.HasProjection)
        {
            table.AddRow("Projection", "PRJ file available");
        }

        if (components.HasEncoding)
        {
            table.AddRow("Encoding", "CPG file available");
        }

        table.Print(TableBorderStyles.Rounded);
        Console.WriteLine();
    }

    private static DbfReader CreateDbfReader(Stream stream, ReadConfiguration settings)
    {
        var readerOptions = new DbfReaderOptions
        {
            IgnoreCase = settings.IgnoreCase,
            TrimStrings = settings.TrimStrings,
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = false,
            CharacterDecodeFallback = null,
            SkipDeletedRecords = !settings.ShowDeleted,
        };

        var validationResult = ArgumentValidator.ValidateEncoding(settings.Encoding);
        if (validationResult.IsValid && !string.IsNullOrEmpty(settings.Encoding))
        {
            readerOptions = readerOptions with
            {
                Encoding = EncodingResolver.Resolve(settings.Encoding),
            };
        }
        else if (!validationResult.IsValid && !settings.Quiet)
        {
            Console.WriteLine($"Warning: {validationResult.ErrorMessage}");

            if (!string.IsNullOrEmpty(validationResult.Suggestion))
            {
                Console.WriteLine(validationResult.Suggestion);
            }
        }

        var reader = DbfReader.Create(stream, readerOptions);

        // Subscribe to warnings if not in quiet mode
        if (!settings.Quiet)
        {
            reader.Warning += (_, e) => Console.WriteLine($"Warning: {e.Message}");
        }

        return reader;
    }

    private static List<DbfRecord> GetRecordsToDisplay(DbfReader reader, ReadConfiguration settings)
    {
        var records = new List<DbfRecord>();
        var recordsToEnumerate = reader.Records;

        var count = 0;
        var skipped = 0;
        foreach (var record in recordsToEnumerate)
        {
            if (skipped < settings.Skip)
            {
                skipped++;
                continue;
            }

            if (settings.Limit.HasValue && count >= settings.Limit.Value)
            {
                break;
            }

            records.Add(record);
            count++;
        }

        return records;
    }

    private static string[] GetFieldsToDisplay(ReadConfiguration settings, DbfReader reader)
    {
        if (string.IsNullOrWhiteSpace(settings.Fields))
        {
            return reader.FieldNames.ToArray();
        }

        var selectedFields = settings
            .Fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToArray();

        var validFields = new List<string>();
        var invalidFields = new List<string>();

        foreach (var fieldName in selectedFields)
        {
            if (reader.HasField(fieldName))
            {
                validFields.Add(fieldName);
            }
            else
            {
                invalidFields.Add(fieldName);
            }
        }

        if (invalidFields.Count > 0 && !settings.Quiet)
        {
            var fieldList = string.Join(", ", invalidFields.Select(f => $"'{f}'"));
            Console.WriteLine($"Warning: Fields not found: {fieldList}");

            if (validFields.Count == 0)
            {
                Console.WriteLine("Available fields: " + string.Join(", ", reader.FieldNames));
            }
        }

        if (validFields.Count == 0 && selectedFields.Length > 0)
        {
            return [];
        }

        return validFields.ToArray();
    }

    private static async Task FormatAndOutputAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        if (fields.Length == 0)
        {
            return;
        }

        var formatter = FormatterFactory.CreateFormatter(settings.Format, settings);

        if (settings.OutputPath != null)
        {
            var directory = Path.GetDirectoryName(settings.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileWriter = new StreamWriter(
                settings.OutputPath,
                false,
                Encoding.UTF8,
                bufferSize: 65536
            );
            await formatter.WriteAsync(records, fields, reader, fileWriter, cancellationToken);

            if (!settings.Quiet)
            {
                var outputFileInfo = new FileInfo(settings.OutputPath);
                Console.WriteLine(
                    $"Output written to: {settings.OutputPath} ({FileSize.Format(outputFileInfo.Length)})"
                );
            }
        }
        else
        {
            await formatter.WriteAsync(records, fields, reader, Console.Out, cancellationToken);
        }
    }

    private static async Task FormatAndOutputStreamingAsync(
        DbfReader reader,
        string[] fields,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        var formatter = FormatterFactory.CreateFormatter(settings.Format, settings);

        if (settings.OutputPath != null)
        {
            var directory = Path.GetDirectoryName(settings.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileWriter = new StreamWriter(
                settings.OutputPath,
                false,
                Encoding.UTF8,
                bufferSize: 65536
            );

            var records = new List<DbfRecord>();
            foreach (var record in CreateFilteredRecordStream(reader, settings, cancellationToken))
            {
                records.Add(record);
            }

            await formatter.WriteAsync(records, fields, reader, fileWriter, cancellationToken);

            if (!settings.Quiet)
            {
                var outputFileInfo = new FileInfo(settings.OutputPath);
                Console.WriteLine(
                    $"Output written to: {settings.OutputPath} ({FileSize.Format(outputFileInfo.Length)})"
                );
            }
        }
        else
        {
            var records = new List<DbfRecord>();
            foreach (var record in CreateFilteredRecordStream(reader, settings, cancellationToken))
            {
                records.Add(record);
            }

            await formatter.WriteAsync(records, fields, reader, Console.Out, cancellationToken);
        }
    }

    private static IEnumerable<DbfRecord> CreateFilteredRecordStream(
        DbfReader reader,
        ReadConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        var recordsEnumerable = reader.Records;
        var count = 0;
        var skipped = 0;

        foreach (var record in recordsEnumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (skipped < settings.Skip)
            {
                skipped++;
                continue;
            }

            if (settings.Limit.HasValue && count >= settings.Limit.Value)
            {
                break;
            }

            yield return record;
            count++;
        }
    }

    private static void DisplayFileInfo(DbfReader reader, InputSource inputSource)
    {
        var stats = reader.GetStatistics();
        var table = new ConsoleTableWriter("\nFile Information", "Property", "Value");
        table.AddRow("File Name", stats.TableName);
        table.AddRow("DBF Version", stats.DbfVersion.GetDescription());
        table.AddRow("Last Updated", stats.LastUpdateDate?.ToString("yyyy-MM-dd") ?? "Unknown");
        table.AddRow("Total Records", stats.TotalRecords.ToString("N0"));
        table.AddRow("Field Count", stats.FieldCount.ToString());
        table.AddRow("Encoding", stats.Encoding);
        table.AddRow("Memo File", stats.HasMemoFile ? stats.MemoFilePath ?? "Yes" : "No");

        var fileSize = inputSource.GetFileSize();
        if (fileSize.HasValue)
        {
            var expectedSize = FileSize.CalculateExpectedSize(reader);
            table.AddRow("File Size", FileSize.Format(fileSize.Value));
            table.AddRow(
                "Size Status",
                FileSize.FormatSizeDifference(fileSize.Value, expectedSize)
            );
        }

        table.Print(TableBorderStyles.Rounded);
        Console.WriteLine();
    }
}
