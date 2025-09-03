using ConsoleAppFramework;
using DbfSharp.ConsoleAot.Commands.Configuration;
using DbfSharp.ConsoleAot.Diagnostics;
using DbfSharp.ConsoleAot.Input;
using DbfSharp.ConsoleAot.Output.Table;
using DbfSharp.ConsoleAot.Text;
using DbfSharp.ConsoleAot.Validation;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Geometry;
using DbfSharp.Core.Projection;
using DbfSharp.Core.Utils;

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
        CancellationToken cancellationToken = default
    )
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
            IgnoreMissingMemo = ignoreMissingMemo,
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

        using var performanceMonitor = verbose ? new PerformanceProfiler("Analyze File") : null;

        try
        {
            // Determine input strategy
            var inputStrategy = InputResolver.DetermineStrategy(settings.FilePath);

            switch (inputStrategy)
            {
                case InputResolutionStrategy.Shapefile:
                    return await ProcessShapefileInfoAsync(settings, cancellationToken);
                case InputResolutionStrategy.DbfOnly:
                case InputResolutionStrategy.Stdin:
                default:
                    return await ProcessDbfOnlyInfoAsync(settings, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return await ExceptionMapper.HandleExceptionAsync(
                ex,
                "analyzing file",
                settings.Verbose
            );
        }
    }

    /// <summary>
    /// Processes shapefile information (includes geometry metadata)
    /// </summary>
    private static async Task<int> ProcessShapefileInfoAsync(
        InfoConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        using var shapefileSource = await InputResolver.ResolveShapefileAsync(settings.FilePath);

        if (!settings.Quiet)
        {
            Console.WriteLine($"Analyzing shapefile: {shapefileSource.DisplayName}\n");
        }

        try
        {
            // Display shapefile component information
            if (settings.ShowHeader)
            {
                DisplayShapefileComponentInformation(shapefileSource);
            }

            // If we have geometry data, show shapefile-specific info
            if (shapefileSource.Components.HasGeometry)
            {
                using var shapefileReader = shapefileSource.CreateShapefileReader();

                if (settings.ShowHeader)
                {
                    DisplayShapefileGeometryInformation(shapefileReader);
                }

                // If we have attributes, show DBF field information
                if (shapefileReader is { HasAttributes: true, DbfReader: not null })
                {
                    if (settings.ShowFields)
                    {
                        DisplayFieldInformation(shapefileReader.DbfReader, settings.Verbose);
                    }

                    if (settings is { ShowStats: true, Verbose: true })
                    {
                        DisplaySampleData(shapefileReader.DbfReader, cancellationToken);
                    }

                    var dbfStats = shapefileReader.DbfReader.GetStatistics();
                    if (settings.ShowMemo && dbfStats.HasMemoFile)
                    {
                        DisplayMemoFileInformation(shapefileReader.DbfReader);
                    }
                }
            }
            else if (shapefileSource.Components.HasAttributes)
            {
                // Fall back to DBF-only processing if no geometry but has attributes
                return await ProcessDbfOnlyFromShapefileAsync(
                    shapefileSource,
                    settings,
                    cancellationToken
                );
            }

            // Display shapefile diagnostics and warnings
            if (settings.Verbose)
            {
                DisplayShapefileDiagnostics(shapefileSource);
            }

            return ExceptionMapper.ExitCodes.Success;
        }
        catch (Exception ex)
        {
            return await ExceptionMapper.HandleExceptionAsync(
                ex,
                "analyzing shapefile",
                settings.Verbose
            );
        }
    }

    /// <summary>
    /// Processes DBF-only data from a shapefile source
    /// </summary>
    private static async Task<int> ProcessDbfOnlyFromShapefileAsync(
        ShapefileInputSource shapefileSource,
        InfoConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        if (shapefileSource.DbfSource == null)
        {
            await Console.Error.WriteLineAsync("Error: DBF file not found in shapefile dataset.");
            return ExceptionMapper.ExitCodes.FileNotFound;
        }

        var readerOptions = CreateDbfReaderOptions(settings);
        using var reader = DbfReader.Create(shapefileSource.DbfSource.Stream, readerOptions);

        if (!settings.Quiet)
        {
            reader.Warning += (_, e) => Console.WriteLine($"Warning: {e.Message}");
        }

        return await ProcessDbfReaderInfo(
            reader,
            shapefileSource.DbfSource,
            settings,
            cancellationToken
        );
    }

    /// <summary>
    /// Processes DBF-only input (no associated geometry)
    /// </summary>
    private static async Task<int> ProcessDbfOnlyInfoAsync(
        InfoConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        using var inputSource = await InputResolver.ResolveAsync(settings.FilePath);

        if (!settings.Quiet)
        {
            Console.WriteLine($"Analyzing DBF file: {inputSource.GetDisplayName()}\n");
        }

        var readerOptions = CreateDbfReaderOptions(settings);
        using var reader = DbfReader.Create(inputSource.Stream, readerOptions);

        if (!settings.Quiet)
        {
            reader.Warning += (_, e) => Console.WriteLine($"Warning: {e.Message}");
        }

        return await ProcessDbfReaderInfo(reader, inputSource, settings, cancellationToken);
    }

    /// <summary>
    /// Common processing for DBF reader information
    /// </summary>
    private static async Task<int> ProcessDbfReaderInfo(
        DbfReader reader,
        InputSource inputSource,
        InfoConfiguration settings,
        CancellationToken cancellationToken
    )
    {
        try
        {
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
                DisplaySampleData(reader, cancellationToken);
            }

            if (settings.ShowMemo && dbfStats.HasMemoFile)
            {
                DisplayMemoFileInformation(reader);
            }

            if (settings.Verbose && fileSize.HasValue)
            {
                DisplayPerformanceInformation(fileSize.Value, expectedSize);
            }

            return ExceptionMapper.ExitCodes.Success;
        }
        catch (Exception ex)
        {
            return await ExceptionMapper.HandleExceptionAsync(
                ex,
                "analyzing DBF file",
                settings.Verbose
            );
        }
    }

    /// <summary>
    /// Displays shapefile component information
    /// </summary>
    private static void DisplayShapefileComponentInformation(ShapefileInputSource shapefileSource)
    {
        var components = shapefileSource.Components;
        var table = new ConsoleTableWriter(
            "Shapefile Components",
            "Component",
            "Status",
            "Details"
        );

        // Geometry files
        table.AddRow(
            "Geometry (.shp)",
            components.ShpPath != null ? "Present" : "Missing",
            components.ShpPath ?? "Required for geometry data"
        );

        table.AddRow(
            "Index (.shx)",
            components.ShxPath != null ? "Present" : "Missing",
            components.ShxPath ?? "Required for random access"
        );

        // Attribute file
        table.AddRow(
            "Attributes (.dbf)",
            components.DbfPath != null ? "Present" : "Missing",
            components.DbfPath ?? "Required for feature attributes"
        );

        // Optional files
        table.AddRow(
            "Projection (.prj)",
            components.PrjPath != null ? "Present" : "Missing",
            components.PrjPath ?? "Coordinate system unknown"
        );

        table.AddRow(
            "Encoding (.cpg)",
            components.CpgPath != null ? "Present" : "Missing",
            components.CpgPath ?? "Uses default encoding"
        );

        // Dataset status
        var status =
            components.IsComplete ? "Complete"
            : components.HasGeometry ? "Geometry only"
            : components.HasAttributes ? "Attributes only"
            : "Incomplete";

        table.AddRow("Dataset Status", status, $"{components.ComponentCount} of 5 files detected");

        table.Print(TableBorderStyles.Rounded);

        // Display detailed projection and encoding information if available
        DisplayProjectionAndEncodingInformation(components);
    }

    /// <summary>
    /// Displays detailed projection and encoding information
    /// </summary>
    private static void DisplayProjectionAndEncodingInformation(
        ShapefileDetector.ShapefileComponents components
    )
    {
        var metadata = ShapefileDetector.GetMetadata(components);

        // Display projection information if available
        if (metadata.Projection != null)
        {
            var table = new ConsoleTableWriter(
                "\nProjection Information",
                "Property",
                "Value",
                "Details"
            );

            table.AddRow(
                "Coordinate System",
                metadata.Projection.CoordinateSystemName ?? "Unknown",
                metadata.Projection.IsValid ? "Valid" : "Invalid WKT"
            );

            table.AddRow(
                "Projection Type",
                metadata.Projection.ProjectionType.GetDescription(),
                $"Type: {metadata.Projection.ProjectionType}"
            );

            if (!string.IsNullOrEmpty(metadata.Projection.Datum))
            {
                table.AddRow("Datum", metadata.Projection.Datum, "Geodetic reference frame");
            }

            if (!string.IsNullOrEmpty(metadata.Projection.Spheroid))
            {
                table.AddRow(
                    "Spheroid/Ellipsoid",
                    metadata.Projection.Spheroid,
                    "Earth model for calculations"
                );
            }

            if (!string.IsNullOrEmpty(metadata.Projection.LinearUnit))
            {
                table.AddRow(
                    "Linear Unit",
                    metadata.Projection.LinearUnit,
                    "Unit for coordinate values"
                );
            }

            if (metadata.Projection.Parameters.Count > 0)
            {
                var paramList = string.Join(
                    ", ",
                    metadata.Projection.Parameters.Select(p => $"{p.Key}={p.Value:F6}")
                );
                table.AddRow(
                    "Parameters",
                    $"{metadata.Projection.Parameters.Count} parameters",
                    paramList
                );
            }

            table.Print(TableBorderStyles.Rounded);
        }

        // Display encoding information if available
        if (metadata.CodePage != null)
        {
            var table = new ConsoleTableWriter(
                "\nEncoding Information",
                "Property",
                "Value",
                "Details"
            );

            table.AddRow(
                "Code Page",
                metadata.CodePage.CodePageIdentifier,
                metadata.CodePage.IsValid ? "Valid" : "Invalid/Unknown"
            );

            if (metadata.CodePage.IsValid)
            {
                table.AddRow(
                    "Encoding Name",
                    metadata.CodePage.Encoding.EncodingName,
                    $"Code Page: {metadata.CodePage.Encoding.CodePage}"
                );

                table.AddRow("Usage", "DBF text fields", "Applied to string field parsing");
            }
            else
            {
                table.AddRow(
                    "Fallback",
                    "UTF-8 (default)",
                    "Used when .cpg encoding cannot be resolved"
                );
            }

            table.Print(TableBorderStyles.Rounded);
        }
    }

    /// <summary>
    /// Displays shapefile geometry metadata
    /// </summary>
    private static void DisplayShapefileGeometryInformation(ShapefileReader shapefileReader)
    {
        var header = shapefileReader.Header;
        var table = new ConsoleTableWriter(
            "\nGeometry Information",
            "Property",
            "Value",
            "Details"
        );

        table.AddRow(
            "Shape Type",
            shapefileReader.ShapeType.GetDescription(),
            $"Code: {(int)shapefileReader.ShapeType}"
        );

        table.AddRow(
            "Total Features",
            shapefileReader.RecordCount.ToString("N0"),
            "Including empty geometries"
        );

        table.AddRow(
            "File Length",
            $"{header.FileLength * 2:N0} bytes",
            $"Raw length: {header.FileLength} words"
        );

        table.AddRow("Shapefile Version", header.Version.ToString(), "ESRI specification version");

        // Bounding box information
        var bbox = shapefileReader.BoundingBox;
        table.AddRow(
            "Bounding Box",
            $"({bbox.MinX:F6}, {bbox.MinY:F6}) to ({bbox.MaxX:F6}, {bbox.MaxY:F6})",
            "Spatial extent of all features"
        );

        if (bbox.HasZ)
        {
            table.AddRow("Z Range", $"{bbox.MinZ:F3} to {bbox.MaxZ:F3}", "Elevation/height range");
        }

        if (bbox.HasM)
        {
            table.AddRow(
                "M Range",
                $"{bbox.MinM:F3} to {bbox.MaxM:F3}",
                "Measure/linear referencing range"
            );
        }

        // Index information
        if (shapefileReader.HasIndex)
        {
            var index = shapefileReader.Index!;
            table.AddRow(
                "File Index (.shx)",
                "Available",
                $"{index.RecordCount} entries for random access"
            );
        }
        else
        {
            table.AddRow("File Index (.shx)", "Missing", "Sequential access only");
        }

        // Spatial index information (R-tree)
        try
        {
            if (shapefileReader.HasSpatialIndex)
            {
                var spatialStats = shapefileReader.GetSpatialIndexStatistics();
                table.AddRow(
                    "R-Tree Spatial Index",
                    "Built",
                    $"{spatialStats.TotalEntries} geometries in {spatialStats.LeafCount + spatialStats.InternalCount} nodes"
                );
            }
            else
            {
                table.AddRow(
                    "R-Tree Spatial Index",
                    "Not built",
                    "Build with spatial queries for better performance"
                );
            }
        }
        catch
        {
            table.AddRow(
                "R-Tree Spatial Index",
                "Not available",
                "Spatial operations not supported for this geometry type"
            );
        }

        table.Print(TableBorderStyles.Rounded);
    }

    /// <summary>
    /// Displays shapefile diagnostics and warnings
    /// </summary>
    private static void DisplayShapefileDiagnostics(ShapefileInputSource shapefileSource)
    {
        var diagnostics = ShapefileDetector.GetDiagnostics(shapefileSource.Components).ToList();
        var warnings = shapefileSource.GetWarnings().ToList();
        var missing = shapefileSource.GetMissingComponents().ToList();

        if (diagnostics.Count > 0)
        {
            var table = new ConsoleTableWriter("\nDiagnostic Information", "Category", "Details");

            foreach (var diagnostic in diagnostics)
            {
                table.AddRow("Info", diagnostic);
            }

            foreach (var warning in warnings)
            {
                table.AddRow("Warning", warning);
            }

            foreach (var missingComponent in missing)
            {
                table.AddRow("Missing", missingComponent);
            }

            table.Print(TableBorderStyles.Rounded);
        }
    }

    private static DbfReaderOptions CreateDbfReaderOptions(InfoConfiguration settings)
    {
        var options = new DbfReaderOptions
        {
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = true,
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

    private static void DisplayHeaderInformation(
        DbfReader reader,
        long? actualFileSize,
        long expectedFileSize
    )
    {
        var header = reader.Header;
        var table = new ConsoleTableWriter("Header Information", "Property", "Value", "Details");
        table.AddRow(
            "DBF Version",
            header.DbfVersion.GetDescription(),
            $"Byte: 0x{header.DbVersionByte:X2}"
        );
        table.AddRow(
            "Last Update",
            header.LastUpdateDate?.ToString("yyyy-MM-dd") ?? "Unknown",
            $"Raw: {header.Year:00}/{header.Month:00}/{header.Day:00}"
        );
        table.AddRow(
            "Header Length",
            $"{header.HeaderLength} bytes",
            $"Fields area: {header.HeaderLength - DbfHeader.Size - 1} bytes"
        );
        table.AddRow("Record Length", $"{header.RecordLength} bytes", "Including deletion flag");
        table.AddRow(
            "Total Records",
            header.NumberOfRecords.ToString("N0"),
            "Including deleted records"
        );
        table.AddRow(
            "MDX Index",
            header.MdxFlag != 0 ? "Present" : "None",
            $"Flag: 0x{header.MdxFlag:X2}"
        );
        table.AddRow(
            "Encryption",
            header.EncryptionFlag != 0 ? "Encrypted" : "None",
            $"Flag: 0x{header.EncryptionFlag:X2}"
        );
        table.AddRow(
            "Language Driver",
            header.EncodingDescription,
            $"Code: 0x{header.LanguageDriver:X2}"
        );

        if (actualFileSize.HasValue)
        {
            table.AddRow(
                "File Size",
                FileSize.Format(actualFileSize.Value),
                $"{actualFileSize.Value:N0} bytes"
            );
        }
        else
        {
            table.AddRow(
                "Expected File Size",
                FileSize.Format(expectedFileSize),
                $"{expectedFileSize:N0} bytes"
            );
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
                field.Type is FieldType.Numeric or FieldType.Float
                    ? field.ActualDecimalCount.ToString()
                    : "-",
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

    private static void DisplaySampleData(DbfReader reader, CancellationToken cancellationToken)
    {
        try
        {
            var sampleRecords = new List<DbfRecord>();
            var count = 0;
            foreach (var record in reader.Records)
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

            var table = new ConsoleTableWriter(
                "\nSample Data (first 3 records)",
                tableHeaders.ToArray()
            );

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
        var table = new ConsoleTableWriter(
            "\nPerformance Information",
            "Metric",
            "Value",
            "Details"
        );

        var memoryEstimate = FileSize.EstimateMemoryUsage(actualFileSize);
        table.AddRow(
            "Estimated Memory Usage",
            FileSize.Format(memoryEstimate),
            "Approximate processing requirement"
        );

        var percentageDiff = FileSize.CalculatePercentageDifference(
            actualFileSize,
            expectedFileSize
        );
        table.AddRow(
            "Size Difference",
            $"{percentageDiff:F1}%",
            percentageDiff == 0 ? "Perfect match" : "May indicate corruption"
        );

        var isWithinBounds = FileSize.IsWithinProcessingBounds(actualFileSize);
        table.AddRow(
            "Processing Recommendation",
            isWithinBounds ? "Optimal" : "Large file",
            isWithinBounds
                ? "Good performance expected"
                : "Consider using --limit for faster processing"
        );

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
            _ => value.ToString() ?? "NULL",
        };
    }
}
