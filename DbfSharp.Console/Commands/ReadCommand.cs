using System.ComponentModel;
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
/// Command for reading and displaying DBF file contents
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

            // Create DBF reader options
            var readerOptions = CreateDbfReaderOptions(settings);

            // Apply limit to reader options if specified
            if (settings.Limit.HasValue)
            {
                readerOptions = readerOptions with { MaxRecords = settings.Limit.Value + settings.Skip };
            }

            using var reader = DbfReader.Open(settings.FilePath, readerOptions);

            if (settings is { Verbose: true, Quiet: false })
            {
                DisplayFileInfo(reader);
            }

            // Get records to display
            var records = GetRecordsToDisplay(reader, settings);

            // Filter fields if specified
            var selectedFields = ParseSelectedFields(settings.Fields);
            if (selectedFields != null)
            {
                records = FilterFields(records, selectedFields, reader);
            }

            // Format and output
            if (settings.OutputPath != null)
            {
                // Write to file
                await WriteToFile(records, reader, settings, selectedFields);
                
                if (!settings.Quiet)
                {
                    AnsiConsole.MarkupLine($"[green]Output written to:[/] {settings.OutputPath}");
                }
            }
            else
            {
                // Write to console
                if (settings is { Format: OutputFormat.Table, Quiet: false })
                {
                    DisplayAsTable(records, reader, selectedFields);
                }
                else
                {
                    await WriteToConsole(records, reader, settings);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Creates DBF reader options from command settings
    /// </summary>
    private static DbfReaderOptions CreateDbfReaderOptions(ReadSettings settings)
    {
        var options = new DbfReaderOptions
        {
            IgnoreCase = settings.IgnoreCase,
            TrimStrings = settings.TrimStrings,
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = false, // For performance
        };

        // Set encoding if specified
        if (!string.IsNullOrEmpty(settings.Encoding))
        {
            try
            {
                options = options with { Encoding = System.Text.Encoding.GetEncoding(settings.Encoding) };
            }
            catch (ArgumentException)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Unknown encoding '{settings.Encoding}', using auto-detection");
            }
        }

        return options;
    }

    /// <summary>
    /// Parses the comma-separated field list
    /// </summary>
    private static string[]? ParseSelectedFields(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
            return null;

        return fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToArray();
    }

    /// <summary>
    /// Gets the records to display based on settings
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
    /// Filters records to only include selected fields
    /// </summary>
    private static IEnumerable<DbfRecord> FilterFields(
        IEnumerable<DbfRecord> records,
        string[] selectedFields,
        DbfReader reader)
    {
        // Validate field names
        var validFields = new List<string>();
        foreach (var fieldName in selectedFields)
        {
            if (reader.HasField(fieldName))
            {
                validFields.Add(fieldName);
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Field '{fieldName}' not found in DBF file");
            }
        }

        if (validFields.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No valid fields specified");
            Environment.Exit(1);
        }

        // For now, just return the original records and filter during display
        // TODO: Implement proper field filtering when DbfRecord constructor is accessible
        return records;
    }

    /// <summary>
    /// Writes output to a file
    /// </summary>
    private static async Task WriteToFile(
        IEnumerable<DbfRecord> records,
        DbfReader reader,
        ReadSettings settings,
        string[]? selectedFields)
    {
        var fieldsToWrite = selectedFields ?? reader.FieldNames.ToArray();
        
        await using var writer = new StreamWriter(settings.OutputPath!, false, System.Text.Encoding.UTF8);
        
        switch (settings.Format)
        {
            case OutputFormat.Csv:
                await WriteCsv(records, fieldsToWrite, writer, ',');
                break;
            case OutputFormat.Tsv:
                await WriteCsv(records, fieldsToWrite, writer, '\t');
                break;
            case OutputFormat.Json:
                await WriteJson(records, fieldsToWrite, writer);
                break;
            default:
                throw new ArgumentException($"Format {settings.Format} not supported for file output");
        }
    }

    /// <summary>
    /// Writes output to console
    /// </summary>
    private static async Task WriteToConsole(
        IEnumerable<DbfRecord> records,
        DbfReader reader,
        ReadSettings settings)
    {
        var fieldsToWrite = ParseSelectedFields(settings.Fields) ?? reader.FieldNames.ToArray();
        
        switch (settings.Format)
        {
            case OutputFormat.Csv:
                await WriteCsv(records, fieldsToWrite, System.Console.Out, ',');
                break;
            case OutputFormat.Tsv:
                await WriteCsv(records, fieldsToWrite, System.Console.Out, '\t');
                break;
            case OutputFormat.Json:
                await WriteJson(records, fieldsToWrite, System.Console.Out);
                break;
        }
    }

    /// <summary>
    /// Writes records in CSV/TSV format
    /// </summary>
    private static async Task WriteCsv(
        IEnumerable<DbfRecord> records,
        string[] fields,
        TextWriter writer,
        char delimiter)
    {
        // Write header
        await writer.WriteLineAsync(string.Join(delimiter, fields.Select(EscapeCsvField)));

        // Write records
        foreach (var record in records)
        {
            var values = fields.Select(field => 
            {
                var value = record[field]; // Use indexer instead of GetValue
                return EscapeCsvField(FormatCsvValue(value));
            });
            
            await writer.WriteLineAsync(string.Join(delimiter, values));
        }
    }

    /// <summary>
    /// Writes records in JSON format
    /// </summary>
    private static async Task WriteJson(
        IEnumerable<DbfRecord> records,
        string[] fields,
        TextWriter writer)
    {
        await writer.WriteLineAsync("[");
        
        var isFirst = true;
        foreach (var record in records)
        {
            if (!isFirst)
                await writer.WriteLineAsync(",");
            
            await writer.WriteAsync("  {");
            
            var fieldValues = fields.Select(field =>
            {
                var value = record[field]; // Use indexer instead of GetValue
                var jsonValue = FormatJsonValue(value);
                return $"\"{EscapeJsonString(field)}\": {jsonValue}";
            });
            
            await writer.WriteAsync(string.Join(", ", fieldValues));
            await writer.WriteAsync("}");
            
            isFirst = false;
        }
        
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("]");
    }

    /// <summary>
    /// Displays file information
    /// </summary>
    private static void DisplayFileInfo(DbfReader reader)
    {
        var stats = reader.GetStatistics();
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]File Information[/]");

        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("File Name", stats.TableName);
        table.AddRow("DBF Version", stats.DbfVersion.GetDescription());
        table.AddRow("Last Updated", stats.LastUpdateDate?.ToString("yyyy-MM-dd") ?? "Unknown");
        table.AddRow("Total Records", stats.TotalRecords.ToString("N0"));
        table.AddRow("Active Records", stats.ActiveRecords.ToString("N0"));
        table.AddRow("Deleted Records", stats.DeletedRecords.ToString("N0"));
        table.AddRow("Field Count", stats.FieldCount.ToString());
        table.AddRow("Record Length", $"{stats.RecordLength} bytes");
        table.AddRow("Encoding", stats.Encoding);
        table.AddRow("Memo File", stats.HasMemoFile ? (stats.MemoFilePath ?? "Yes") : "No");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays records as a formatted table using Spectre.Console
    /// </summary>
    private static void DisplayAsTable(
        IEnumerable<DbfRecord> records,
        DbfReader reader,
        string[]? selectedFields)
    {
        var fieldsToShow = selectedFields ?? reader.FieldNames.ToArray();
        
        var table = new Table()
            .Border(TableBorder.Rounded);

        // Add columns
        foreach (var fieldName in fieldsToShow)
        {
            var field = reader.FindField(fieldName);
            var header = fieldName;
            
            if (field.HasValue)
            {
                header += $"\n[dim]({field.Value.Type.GetDescription()})[/]";
            }
            
            table.AddColumn(new TableColumn(header).Centered());
        }

        // Add rows
        var recordCount = 0;
        foreach (var record in records)
        {
            var rowValues = new string[fieldsToShow.Length];
            
            for (int i = 0; i < fieldsToShow.Length; i++)
            {
                var value = record[fieldsToShow[i]]; // Use indexer instead of GetValue
                rowValues[i] = FormatValueForTable(value);
            }

            table.AddRow(rowValues);
            recordCount++;

            // Prevent console overflow with too many records
            if (recordCount >= 1000)
            {
                table.AddRow(Enumerable.Repeat("[dim]...[/]", fieldsToShow.Length).ToArray());
                AnsiConsole.MarkupLine("[yellow]Note:[/] Only first 1000 records shown for table format. Use --format csv or --limit for more control.");
                break;
            }
        }

        if (recordCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No records to display[/]");
            return;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Showing {recordCount:N0} record(s)[/]");
    }

    /// <summary>
    /// Formats a value for display in a table
    /// </summary>
    private static string FormatValueForTable(object? value)
    {
        return value switch
        {
            null => "[dim]NULL[/]",
            string s when string.IsNullOrWhiteSpace(s) => "[dim]<empty>[/]",
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            float f => f.ToString("N2"),
            bool b => b ? "✓" : "✗",
            InvalidValue => "[red]Error[/]",
            byte[] bytes => $"[dim]<{bytes.Length} bytes>[/]",
            _ => value.ToString()?.Replace("[", "[[").Replace("]", "]]") ?? "[dim]NULL[/]",
        };
    }

    /// <summary>
    /// Formats a value for CSV output
    /// </summary>
    private static string FormatCsvValue(object? value)
    {
        return value switch
        {
            null => "",
            string s => s,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            decimal d => d.ToString("F"),
            double d => d.ToString("F"),
            float f => f.ToString("F"),
            bool b => b ? "True" : "False",
            InvalidValue => "<INVALID>",
            byte[] bytes => $"<{bytes.Length} bytes>",
            _ => value.ToString() ?? "",
        };
    }

    /// <summary>
    /// Formats a value for JSON output
    /// </summary>
    private static string FormatJsonValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{EscapeJsonString(s)}\"",
            DateTime dt => $"\"{dt:yyyy-MM-ddTHH:mm:ss}\"",
            decimal d => d.ToString("F"),
            double d => d.ToString("F"),
            float f => f.ToString("F"),
            bool b => b ? "true" : "false",
            InvalidValue => "null",
            byte[] => "null",
            _ => $"\"{EscapeJsonString(value.ToString() ?? "")}\"",
        };
    }

    /// <summary>
    /// Escapes a string for CSV output
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    /// <summary>
    /// Escapes a string for JSON output
    /// </summary>
    private static string EscapeJsonString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }
}