using System.ComponentModel;
using DbfSharp.Console.Formatters;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DbfSharp.Console.Commands;

/// <summary>
/// Output format options
/// </summary>
public enum OutputFormat
{
    Table,
    Csv,
    Tsv,
    Json,
}

/// <summary>
/// Settings for the read command
/// </summary>
public sealed class ReadSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Path to the DBF file to read")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("-f|--format")]
    [Description("Output format (table, csv, tsv, json)")]
    [DefaultValue(OutputFormat.Table)]
    public OutputFormat Format { get; set; } = OutputFormat.Table;

    [CommandOption("-o|--output")]
    [Description("Output file path (default: stdout)")]
    public string? OutputPath { get; set; }

    [CommandOption("-l|--limit")]
    [Description("Maximum number of records to display")]
    public int? Limit { get; set; }

    [CommandOption("-s|--skip")]
    [Description("Number of records to skip")]
    [DefaultValue(0)]
    public int Skip { get; set; } = 0;

    [CommandOption("--show-deleted")]
    [Description("Include deleted records in output")]
    [DefaultValue(false)]
    public bool ShowDeleted { get; set; } = false;

    [CommandOption("--fields")]
    [Description("Comma-separated list of fields to include (default: all)")]
    public string? Fields { get; set; }

    [CommandOption("-v|--verbose")]
    [Description("Enable verbose output")]
    [DefaultValue(false)]
    public bool Verbose { get; set; } = false;

    [CommandOption("-q|--quiet")]
    [Description("Suppress all output except errors")]
    [DefaultValue(false)]
    public bool Quiet { get; set; } = false;

    [CommandOption("--encoding")]
    [Description("Override character encoding (e.g., UTF-8, Windows-1252)")]
    public string? Encoding { get; set; }

    [CommandOption("--ignore-case")]
    [Description("Case-insensitive field names")]
    [DefaultValue(true)]
    public bool IgnoreCase { get; set; } = true;

    [CommandOption("--trim-strings")]
    [Description("Trim whitespace from string fields")]
    [DefaultValue(true)]
    public bool TrimStrings { get; set; } = true;

    [CommandOption("--ignore-missing-memo")]
    [Description("Don't fail if memo file is missing")]
    [DefaultValue(true)]
    public bool IgnoreMissingMemo { get; set; } = true;
}

/// <summary>
/// Command for reading and displaying DBF file contents using pluggable formatters
/// </summary>
public sealed class ReadCommand : AsyncCommand<ReadSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ReadSettings settings)
    {
        try
        {
            if (!settings.Quiet)
            {
                AnsiConsole.MarkupLine($"[blue]Reading DBF file:[/] {settings.FilePath}");
            }

            // Create DBF reader with optimized settings
            using var reader = await CreateDbfReaderAsync(settings);

            if (settings is { Verbose: true, Quiet: false })
            {
                DisplayFileInfo(reader);
            }

            // Get records and fields to process
            var records = GetRecordsToDisplay(reader, settings);
            var fieldsToDisplay = GetFieldsToDisplay(settings, reader);

            // Create appropriate formatter and output data
            await FormatAndOutputAsync(records, fieldsToDisplay, reader, settings);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Creates and configures the DBF reader with optimal settings
    /// </summary>
    private static async Task<DbfReader> CreateDbfReaderAsync(ReadSettings settings)
    {
        var readerOptions = new DbfReaderOptions
        {
            IgnoreCase = settings.IgnoreCase,
            TrimStrings = settings.TrimStrings,
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = false, // optimize for performance
            CharacterDecodeFallback = null 
        };

        if (settings.Limit.HasValue)
        {
            readerOptions = readerOptions with { MaxRecords = settings.Limit.Value + settings.Skip };
        }

        if (!string.IsNullOrEmpty(settings.Encoding))
        {
            var encoding = TryGetEncoding(settings.Encoding);
            if (encoding != null)
            {
                readerOptions = readerOptions with { Encoding = encoding };
            }
            else
            {
                if (!settings.Quiet)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Unknown encoding '{settings.Encoding}'");
                }
            }
        }

        return await DbfReader.OpenAsync(settings.FilePath, readerOptions);
    }

    /// <summary>
    /// Tries to get an encoding by name, with special handling for Japanese encodings
    /// </summary>
    private static System.Text.Encoding? TryGetEncoding(string encodingName)
    {
        try
        {
            return System.Text.Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the records to display based on user settings
    /// </summary>
    private static IEnumerable<DbfRecord> GetRecordsToDisplay(DbfReader reader, ReadSettings settings)
    {
        var records = settings.ShowDeleted 
            ? reader.Records.Concat(reader.DeletedRecords)
            : reader.Records;

        if (settings.Skip > 0)
        {
            records = records.Skip(settings.Skip);
        }

        if (settings.Limit.HasValue)
        {
            records = records.Take(settings.Limit.Value);
        }

        return records;
    }

    /// <summary>
    /// Determines which fields to display based on user settings and validates field names
    /// </summary>
    private static string[] GetFieldsToDisplay(ReadSettings settings, DbfReader reader)
    {
        if (string.IsNullOrWhiteSpace(settings.Fields))
        {
            return reader.FieldNames.ToArray();
        }

        var selectedFields = settings.Fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToArray();

        // Validate field names and warn about missing fields
        var validFields = new List<string>();
        foreach (var fieldName in selectedFields)
        {
            if (reader.HasField(fieldName))
            {
                validFields.Add(fieldName);
            }
            else if (!settings.Quiet)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Field '{fieldName}' not found in DBF file");
            }
        }

        if (validFields.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No valid fields specified");
            Environment.Exit(1);
        }

        return validFields.ToArray();
    }

    /// <summary>
    /// Formats and outputs the data using the appropriate formatter
    /// </summary>
    private static async Task FormatAndOutputAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        ReadSettings settings)
    {
        var formatter = FormatterFactory.CreateFormatter(settings.Format, settings);

        if (settings.OutputPath != null)
        {
            // Output to file
            await WriteToFileAsync(records, fields, reader, formatter, settings);
        }
        else
        {
            // Output to console - use console formatter if available for better experience
            await WriteToConsoleAsync(records, fields, reader, formatter, settings);
        }
    }

    /// <summary>
    /// Writes formatted output to a file
    /// </summary>
    private static async Task WriteToFileAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        IDbfFormatter formatter,
        ReadSettings settings)
    {
        await using var fileWriter = new StreamWriter(settings.OutputPath!, false, System.Text.Encoding.UTF8);
        await formatter.WriteAsync(records, fields, reader, fileWriter);

        if (!settings.Quiet)
        {
            AnsiConsole.MarkupLine($"[green]Output written to:[/] {settings.OutputPath}");
        }
    }

    /// <summary>
    /// Writes formatted output to console, using console formatter if available
    /// </summary>
    private static async Task WriteToConsoleAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        IDbfFormatter formatter,
        ReadSettings settings)
    {
        // Use console formatter for better interactive experience if available
        if (formatter is IConsoleFormatter consoleFormatter && !settings.Quiet)
        {
            consoleFormatter.DisplayToConsole(records, fields, reader);
        }
        else
        {
            // Fallback to standard TextWriter output
            await formatter.WriteAsync(records, fields, reader, System.Console.Out);
        }
    }

    /// <summary>
    /// Displays comprehensive file information for verbose mode
    /// </summary>
    private static void DisplayFileInfo(DbfReader reader)
    {
        var stats = reader.GetStatistics();
        
        var infoTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]File Information[/]")
            .AddColumn("Property", column => column.Width(20))
            .AddColumn("Value");

        // Add file statistics rows
        infoTable.AddRow("File Name", stats.TableName);
        infoTable.AddRow("DBF Version", stats.DbfVersion.GetDescription());
        infoTable.AddRow("Last Updated", stats.LastUpdateDate?.ToString("yyyy-MM-dd") ?? "Unknown");
        infoTable.AddRow("Total Records", stats.TotalRecords.ToString("N0"));
        infoTable.AddRow("Active Records", stats.ActiveRecords.ToString("N0"));
        infoTable.AddRow("Deleted Records", stats.DeletedRecords.ToString("N0"));
        infoTable.AddRow("Field Count", stats.FieldCount.ToString());
        infoTable.AddRow("Record Length", $"{stats.RecordLength} bytes");
        infoTable.AddRow("Encoding", stats.Encoding);
        infoTable.AddRow("Memo File", stats.HasMemoFile ? (stats.MemoFilePath ?? "Yes") : "No");

        AnsiConsole.Write(infoTable);
        AnsiConsole.WriteLine();
    }
}