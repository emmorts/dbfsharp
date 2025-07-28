using System.ComponentModel;
using System.Text;
using DbfSharp.Console.Utils;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DbfSharp.Console.Commands;

/// <summary>
/// Settings for the info command
/// </summary>
public sealed class InfoSettings : CommandSettings
{
    [CommandArgument(0, "[FILE]")]
    [Description("Path to the DBF file to analyze (omit to read from stdin)")]
    public string? FilePath { get; set; }

    [CommandOption("--fields")]
    [Description("Show field definitions")]
    [DefaultValue(true)]
    public bool ShowFields { get; set; } = true;

    [CommandOption("--header")]
    [Description("Show header information")]
    [DefaultValue(true)]
    public bool ShowHeader { get; set; } = true;

    [CommandOption("--stats")]
    [Description("Show record statistics")]
    [DefaultValue(true)]
    public bool ShowStats { get; set; } = true;

    [CommandOption("--memo")]
    [Description("Show memo file information")]
    [DefaultValue(true)]
    public bool ShowMemo { get; set; } = true;

    [CommandOption("-v|--verbose")]
    [Description("Show additional detailed information")]
    [DefaultValue(false)]
    public bool Verbose { get; set; } = false;

    [CommandOption("-q|--quiet")]
    [Description("Suppress all output except errors")]
    [DefaultValue(false)]
    public bool Quiet { get; set; } = false;

    [CommandOption("--encoding")]
    [Description("Override character encoding (e.g., UTF-8, Windows-1252)")]
    public string? Encoding { get; set; }

    [CommandOption("--ignore-missing-memo")]
    [Description("Don't fail if memo file is missing")]
    [DefaultValue(true)]
    public bool IgnoreMissingMemo { get; set; } = true;
}

/// <summary>
/// Command for displaying DBF file information and metadata
/// </summary>
public sealed class InfoCommand : AsyncCommand<InfoSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InfoSettings settings)
    {
        string? tempFilePath = null;
        try
        {
            // Resolve file path (from argument or stdin)
            var (filePath, isTemporary) = await StdinHelper.ResolveFilePathAsync(settings.FilePath);
            if (isTemporary)
            {
                tempFilePath = filePath;
            }
            
            if (!settings.Quiet)
            {
                var source = isTemporary ? "stdin" : filePath;
                AnsiConsole.MarkupLine($"[blue]Analyzing DBF file:[/] {source}");
                AnsiConsole.WriteLine();
            }

            var readerOptions = CreateDbfReaderOptions(settings);

            using var reader = await DbfReader.OpenAsync(filePath, readerOptions);
            var stats = reader.GetStatistics();

            DisplayFileOverview(reader, stats);

            if (settings.ShowHeader)
            {
                AnsiConsole.WriteLine();
                DisplayHeaderInformation(reader);
            }

            if (settings.ShowFields)
            {
                AnsiConsole.WriteLine();
                DisplayFieldInformation(reader, settings.Verbose);
            }

            if (settings.ShowStats)
            {
                AnsiConsole.WriteLine();
                DisplayRecordStatistics(reader, settings.Verbose);
            }

            if (settings.ShowMemo && stats.HasMemoFile)
            {
                AnsiConsole.WriteLine();
                DisplayMemoFileInformation(reader);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
        finally
        {
            // Clean up temporary file if created
            if (tempFilePath != null)
            {
                StdinHelper.CleanupTemporaryFile(tempFilePath);
            }
        }
    }

    /// <summary>
    /// Creates DBF reader options from command settings
    /// </summary>
    private static DbfReaderOptions CreateDbfReaderOptions(InfoSettings settings)
    {
        var options = new DbfReaderOptions
        {
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = true // For info command, we want validation
        };

        // Set encoding if specified
        if (!string.IsNullOrEmpty(settings.Encoding))
        {
            var encoding = TryGetEncoding(settings.Encoding);
            if (encoding != null)
            {
                options = options with { Encoding = encoding };
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning:[/] Unknown encoding '{settings.Encoding}', using auto-detection");
            }
        }

        return options;
    }

    /// <summary>
    /// Tries to get an encoding by name, with special handling for Japanese encodings
    /// </summary>
    private static Encoding? TryGetEncoding(string encodingName)
    {
        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Displays a high-level overview of the file
    /// </summary>
    private static void DisplayFileOverview(DbfReader reader, DbfStatistics stats)
    {
        var panel = new Panel(
            Align.Left(new Markup(
                $"[bold]File:[/] {stats.TableName}\n" +
                $"[bold]Version:[/] {stats.DbfVersion.GetDescription()}\n" +
                $"[bold]Records:[/] {stats.ActiveRecords:N0} active, {stats.DeletedRecords:N0} deleted\n" +
                $"[bold]Fields:[/] {stats.FieldCount}\n" +
                $"[bold]Encoding:[/] {stats.Encoding}\n" +
                $"[bold]Last Updated:[/] {stats.LastUpdateDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown"}")))
        {
            Header = new PanelHeader("[bold blue]DBF File Overview[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }
    
    /// <summary>
    /// Displays detailed header information
    /// </summary>
    private static void DisplayHeaderInformation(DbfReader reader)
    {
        var header = reader.Header;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Header Information[/]");

        table.AddColumn("Property", col => col.Width(20));
        table.AddColumn("Value", col => col.Width(40));
        table.AddColumn("Details");

        // Version information
        table.AddRow(
            "DBF Version",
            header.DbfVersion.GetDescription(),
            $"Byte: 0x{header.DbVersionByte:X2}");

        // Date information
        if (header.LastUpdateDate.HasValue)
        {
            table.AddRow(
                "Last Update",
                header.LastUpdateDate.Value.ToString("yyyy-MM-dd"),
                $"Raw: {header.Year:00}/{header.Month:00}/{header.Day:00}");
        }
        else
        {
            table.AddRow(
                "Last Update",
                "Unknown",
                $"Raw: {header.Year:00}/{header.Month:00}/{header.Day:00}");
        }

        // Structure information
        table.AddRow(
            "Header Length",
            $"{header.HeaderLength} bytes",
            $"Fields area: {header.HeaderLength - DbfHeader.Size - 1} bytes");

        table.AddRow(
            "Record Length",
            $"{header.RecordLength} bytes",
            "Including deletion flag");

        table.AddRow(
            "Total Records",
            header.NumberOfRecords.ToString("N0"),
            "Including deleted records");

        // Flags and features
        table.AddRow(
            "MDX Index",
            header.MdxFlag != 0 ? "Present" : "None",
            $"Flag: 0x{header.MdxFlag:X2}");

        table.AddRow(
            "Encryption",
            header.EncryptionFlag != 0 ? "Encrypted" : "None",
            $"Flag: 0x{header.EncryptionFlag:X2}");

        table.AddRow(
            "Language Driver",
            header.EncodingDescription,
            $"Code: 0x{header.LanguageDriver:X2}");

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Displays field definitions and information
    /// </summary>
    private static void DisplayFieldInformation(DbfReader reader, bool verbose)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Field Definitions[/]");

        table.AddColumn("#", col => col.Width(3).RightAligned());
        table.AddColumn("Name", col => col.Width(15));
        table.AddColumn("Type", col => col.Width(12));
        table.AddColumn("Length", col => col.Width(8).RightAligned());
        table.AddColumn("Decimals", col => col.Width(8).RightAligned());
        table.AddColumn(".NET Type", col => col.Width(15));

        if (verbose)
        {
            table.AddColumn("Address", col => col.Width(10).RightAligned());
            table.AddColumn("Flags", col => col.Width(8));
        }

        var fieldIndex = 1;
        foreach (var field in reader.Fields)
        {
            var netType = GetFriendlyTypeName(field.ExpectedNetType, field.SupportsNull);

            var row = new List<string>
            {
                fieldIndex.ToString(),
                field.Name,
                $"{field.Type} ({(char)field.Type})",
                field.ActualLength.ToString(),
                field.Type is FieldType.Numeric or FieldType.Float
                    ? field.ActualDecimalCount.ToString()
                    : "-",
                netType
            };

            if (verbose)
            {
                row.Add($"0x{field.Address:X8}");

                var flags = new List<string>();
                if (field.UsesMemoFile) flags.Add("Memo");
                if (field.IndexFieldFlag != 0) flags.Add("Index");
                if (field.SetFieldsFlag != 0) flags.Add("Set");
                row.Add(string.Join(", ", flags));
            }

            table.AddRow(row.ToArray());
            fieldIndex++;
        }

        AnsiConsole.Write(table);

        // Show field type summary
        var typeGroups = reader.Fields
            .GroupBy(f => f.Type)
            .OrderBy(g => g.Key.ToString())
            .ToList();

        if (typeGroups.Count > 1)
        {
            AnsiConsole.WriteLine();
            var typePanel = new Panel(
                string.Join(" • ", typeGroups.Select(g =>
                    $"[bold]{g.Key}[/]: {g.Count()}")))
            {
                Header = new PanelHeader("[dim]Field Type Summary[/]"),
                Border = BoxBorder.None
            };
            AnsiConsole.Write(typePanel);
        }
    }

    /// <summary>
    /// Displays record statistics
    /// </summary>
    private static void DisplayRecordStatistics(DbfReader reader, bool verbose)
    {
        var stats = reader.GetStatistics();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Record Statistics[/]");

        table.AddColumn("Metric");
        table.AddColumn("Count", col => col.RightAligned());
        table.AddColumn("Percentage", col => col.RightAligned());

        table.AddRow("Total Records", stats.TotalRecords.ToString("N0"), "100.0%");
        table.AddRow("Active Records", stats.ActiveRecords.ToString("N0"),
            $"{(stats.TotalRecords > 0 ? stats.ActiveRecords * 100.0 / stats.TotalRecords : 0):F1}%");
        table.AddRow("Deleted Records", stats.DeletedRecords.ToString("N0"),
            $"{(stats.TotalRecords > 0 ? stats.DeletedRecords * 100.0 / stats.TotalRecords : 0):F1}%");

        AnsiConsole.Write(table);

        if (verbose && stats.ActiveRecords > 0)
        {
            AnsiConsole.WriteLine();

            DisplaySampleData(reader);
        }
    }

    /// <summary>
    /// Displays sample data from the file
    /// </summary>
    private static void DisplaySampleData(DbfReader reader)
    {
        AnsiConsole.MarkupLine("[bold blue]Sample Data (first 3 records):[/]");

        var sampleRecords = reader.Records.Take(3).ToList();
        if (sampleRecords.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No records to display[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded);

        // Add columns (limit to first 5 fields for readability)
        var fieldsToShow = reader.FieldNames.Take(5).ToArray();
        foreach (var fieldName in fieldsToShow)
        {
            table.AddColumn(fieldName);
        }

        if (reader.FieldNames.Count > 5)
        {
            table.AddColumn("[dim]...[/]");
        }

        // Add sample rows
        foreach (var record in sampleRecords)
        {
            var values = new List<string>();

            foreach (var fieldName in fieldsToShow)
            {
                var value = record[fieldName];
                values.Add(FormatSampleValue(value));
            }

            if (reader.FieldNames.Count > 5)
            {
                values.Add($"[dim]+{reader.FieldNames.Count - 5} more[/]");
            }

            table.AddRow(values.ToArray());
        }

        AnsiConsole.Write(table);

        if (reader.Count > 3)
        {
            AnsiConsole.MarkupLine($"[dim]... and {reader.Count - 3:N0} more records[/]");
        }
    }

    /// <summary>
    /// Displays memo file information
    /// </summary>
    private static void DisplayMemoFileInformation(DbfReader reader)
    {
        var stats = reader.GetStatistics();

        var panel = new Panel(
            Align.Left(new Markup(
                $"[bold]Memo File:[/] {stats.MemoFilePath ?? "Present"}\n" +
                $"[bold]Status:[/] {(stats.HasMemoFile ? "Available" : "Missing")}")))
        {
            Header = new PanelHeader("[bold blue]Memo File Information[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Gets a friendly display name for a .NET type, handling nullable types properly
    /// </summary>
    private static string GetFriendlyTypeName(Type type, bool supportsNull)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return GetSimpleTypeName(underlyingType) + "?";
            }
        }
        
        if (supportsNull && !type.IsValueType)
        {
            return GetSimpleTypeName(type) + "?";
        }
        
        return GetSimpleTypeName(type);
    }
    
    /// <summary>
    /// Gets a simple, friendly name for common .NET types
    /// </summary>
    private static string GetSimpleTypeName(Type type)
    {
        return type.Name switch
        {
            "String" => "String",
            "Int32" => "Int32",
            "Decimal" => "Decimal", 
            "DateTime" => "DateTime",
            "Boolean" => "Boolean",
            "Single" => "Single",
            "Double" => "Double",
            "Int64" => "Int64",
            "Byte[]" => "Byte[]",
            _ => type.Name
        };
    }

    /// <summary>
    /// Formats a value for sample data display
    /// </summary>
    private static string FormatSampleValue(object? value)
    {
        return value switch
        {
            null => "[dim]NULL[/]",
            string s when string.IsNullOrWhiteSpace(s) => "[dim]<empty>[/]",
            string { Length: > 20 } s => string.Concat(s.AsSpan(0, 17), "..."),
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            float f => f.ToString("N2"),
            bool b => b ? "True" : "False",
            InvalidValue => "[red]<invalid>[/]",
            byte[] bytes => $"[dim]<{bytes.Length} bytes>[/]",
            _ => value.ToString()?.Replace("[", "[[").Replace("]", "]]") ?? "[dim]NULL[/]"
        };
    }
}