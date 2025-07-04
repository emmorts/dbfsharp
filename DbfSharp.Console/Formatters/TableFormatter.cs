using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;
using Spectre.Console;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Formats DBF records as a console table using Spectre.Console for rich display
/// This formatter is optimized for interactive console display rather than file output
/// </summary>
public sealed class TableFormatter : IConsoleFormatter
{
    private readonly FormatterOptions _options;
    private const int DefaultMaxDisplayRecords = 1000;

    /// <summary>
    /// Initializes a new table formatter with the specified options
    /// </summary>
    /// <param name="options">Formatting options, particularly MaxDisplayRecords for console overflow prevention</param>
    public TableFormatter(FormatterOptions? options = null)
    {
        _options = options ?? new FormatterOptions
        {
            MaxDisplayRecords = DefaultMaxDisplayRecords,
            IncludeTypeInfo = true
        };
    }

    /// <summary>
    /// Displays formatted records directly to console using Spectre.Console table rendering
    /// This is the preferred method for interactive console display
    /// </summary>
    public void DisplayToConsole(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        CancellationToken cancellationToken = default)
    {
        var table = CreateSpectreTable(fields, reader);
        var recordCount = PopulateTableRows(table, records, fields, cancellationToken);

        if (recordCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No records to display[/]");
            return;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Showing {recordCount:N0} record(s)[/]");
    }

    /// <summary>
    /// Fallback implementation for file output - renders table as plain text
    /// Note: For file output, CSV or JSON formatters are typically more appropriate
    /// </summary>
    public async Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default)
    {
        await WriteTextTableAsync(records, fields, reader, writer, cancellationToken);
    }

    /// <summary>
    /// Creates and configures the Spectre.Console table with appropriate styling
    /// </summary>
    private Table CreateSpectreTable(string[] fields, DbfReader reader)
    {
        var table = new Table()
            .Border(TableBorder.Rounded);

        foreach (var fieldName in fields)
        {
            var columnHeader = BuildColumnHeader(fieldName, reader);
            table.AddColumn(new TableColumn(columnHeader).Centered());
        }

        return table;
    }

    /// <summary>
    /// Builds a rich column header with optional type information and proper markup escaping
    /// </summary>
    private string BuildColumnHeader(string fieldName, DbfReader reader)
    {
        var header = EscapeMarkup(fieldName);

        if (!_options.IncludeTypeInfo) return header;

        var field = reader.FindField(fieldName);
        if (!field.HasValue) return header;

        var typeDescription = field.Value.Type.GetDescription();
        header += $"\n[dim]({EscapeMarkup(typeDescription)})[/]";

        return header;
    }

    /// <summary>
    /// Populates table rows with data while respecting display limits and handling cancellation
    /// </summary>
    private int PopulateTableRows(
        Table table,
        IEnumerable<DbfRecord> records,
        string[] fields,
        CancellationToken cancellationToken)
    {
        var recordCount = 0;
        var maxRecords = _options.MaxDisplayRecords ?? DefaultMaxDisplayRecords;

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (recordCount >= maxRecords)
            {
                // Add overflow indicator row
                var overflowRow = Enumerable.Repeat("[dim]...[/]", fields.Length).ToArray();
                table.AddRow(overflowRow);

                if (_options.ShowWarnings)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Note:[/] Only first {maxRecords:N0} records shown for table format. Use --format csv or --limit for more control.");
                }

                break;
            }

            var rowValues = BuildTableRow(record, fields);
            table.AddRow(rowValues);
            recordCount++;
        }

        return recordCount;
    }

    /// <summary>
    /// Builds a single table row with properly formatted and escaped cell values
    /// </summary>
    private static string[] BuildTableRow(DbfRecord record, string[] fields)
    {
        var rowValues = new string[fields.Length];

        for (var i = 0; i < fields.Length; i++)
        {
            var value = record[fields[i]];
            rowValues[i] = FormatValueForTable(value);
        }

        return rowValues;
    }

    /// <summary>
    /// Formats individual cell values with appropriate visual indicators and markup safety
    /// </summary>
    private static string FormatValueForTable(object? value)
    {
        var formatted = value switch
        {
            null => "[dim]NULL[/]",
            string s when string.IsNullOrWhiteSpace(s) => "[dim]<empty>[/]",
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("N2"),
            double d when double.IsFinite(d) => d.ToString("N2"),
            double d when double.IsNaN(d) => "[yellow]NaN[/]",
            double d when double.IsPositiveInfinity(d) => "[yellow]∞[/]",
            double d when double.IsNegativeInfinity(d) => "[yellow]-∞[/]",
            float f when float.IsFinite(f) => f.ToString("N2"),
            float f when float.IsNaN(f) => "[yellow]NaN[/]",
            float f when float.IsPositiveInfinity(f) => "[yellow]∞[/]",
            float f when float.IsNegativeInfinity(f) => "[yellow]-∞[/]",
            bool b => b ? "[green]✓[/]" : "[red]✗[/]",
            InvalidValue => "[red]Error[/]",
            byte[] bytes => $"[dim]<{bytes.Length} bytes>[/]",
            _ => value.ToString() ?? "[dim]NULL[/]",
        };

        // Ensure the formatted value is safe for markup rendering
        return EscapeMarkup(formatted);
    }

    /// <summary>
    /// Escapes Spectre.Console markup characters to prevent rendering issues
    /// Critical for user data that might contain square brackets
    /// </summary>
    private static string EscapeMarkup(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Contains("[/]") || input.StartsWith('[') && input.Contains(']'))
            return input;

        return input.Replace("[", "[[").Replace("]", "]]");
    }

    /// <summary>
    /// Fallback method for writing table format to text files
    /// Creates a simple ASCII table without rich formatting
    /// </summary>
    private async Task WriteTextTableAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken)
    {
        var columnWidths = CalculateColumnWidths(records, fields, reader);

        await WriteTextTableHeader(fields, reader, columnWidths, writer, cancellationToken);
        await WriteTextTableSeparator(columnWidths, writer, cancellationToken);
        await WriteTextTableData(records, fields, columnWidths, writer, cancellationToken);
    }

    /// <summary>
    /// Calculates optimal column widths for text table layout
    /// </summary>
    private Dictionary<string, int> CalculateColumnWidths(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        int sampleSize = 50)
    {
        var widths = new Dictionary<string, int>();

        foreach (var field in fields)
        {
            var headerText = _options.IncludeTypeInfo
                ? $"{field} ({reader.FindField(field)?.Type.GetDescription() ?? "Unknown"})"
                : field;
            widths[field] = headerText.Length;
        }

        // sample first `sampleSize` records to estimate column widths
        var sampleCount = 0;
        foreach (var record in records)
        {
            if (sampleCount++ > sampleSize) break;

            foreach (var field in fields)
            {
                var value = record[field];
                var displayValue = FormatValueForTextTable(value);
                var cleanValue = StripMarkup(displayValue);
                widths[field] = Math.Max(widths[field], cleanValue.Length);
            }
        }

        return widths;
    }

    /// <summary>
    /// Formats values for plain text table output (without markup)
    /// </summary>
    private static string FormatValueForTextTable(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s when string.IsNullOrWhiteSpace(s) => "<empty>",
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            float f => f.ToString("N2"),
            bool b => b ? "✓" : "✗",
            InvalidValue => "Error",
            byte[] bytes => $"<{bytes.Length} bytes>",
            _ => value.ToString() ?? "NULL",
        };
    }

    /// <summary>
    /// Strips Spectre.Console markup tags from text for plain text output
    /// </summary>
    private static string StripMarkup(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // simple markup removal - could be enhanced with regex for more complex cases
        var result = input;
        while (result.Contains('[') && result.Contains(']'))
        {
            var start = result.IndexOf('[');
            var end = result.IndexOf(']', start);
            if (end > start)
            {
                result = result.Remove(start, end - start + 1);
            }
            else
            {
                break;
            }
        }

        return result.Replace("[[", "[").Replace("]]", "]");
    }

    private async Task WriteTextTableHeader(
        string[] fields,
        DbfReader reader,
        Dictionary<string, int> columnWidths,
        TextWriter writer,
        CancellationToken cancellationToken)
    {
        var headerParts = fields.Select(field =>
        {
            var headerText = _options.IncludeTypeInfo
                ? $"{field} ({reader.FindField(field)?.Type.GetDescription() ?? "Unknown"})"
                : field;
            return headerText.PadRight(columnWidths[field]);
        });

        var headerLine = string.Join(" | ", headerParts);
        await writer.WriteLineAsync(headerLine.AsMemory(), cancellationToken);
    }

    private static async Task WriteTextTableSeparator(
        Dictionary<string, int> columnWidths,
        TextWriter writer,
        CancellationToken cancellationToken)
    {
        var separatorParts = columnWidths.Values.Select(width => new string('-', width));
        var separatorLine = string.Join("-+-", separatorParts);
        await writer.WriteLineAsync(separatorLine.AsMemory(), cancellationToken);
    }

    private async Task WriteTextTableData(
        IEnumerable<DbfRecord> records,
        string[] fields,
        Dictionary<string, int> columnWidths,
        TextWriter writer,
        CancellationToken cancellationToken)
    {
        var maxRecords = _options.MaxDisplayRecords ?? DefaultMaxDisplayRecords;
        var recordCount = 0;

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (recordCount >= maxRecords)
            {
                var overflowParts = fields.Select(field => "...".PadRight(columnWidths[field]));
                var overflowLine = string.Join(" | ", overflowParts);
                await writer.WriteLineAsync(overflowLine.AsMemory(), cancellationToken);
                break;
            }

            var dataParts = fields.Select(field =>
            {
                var value = record[field];
                var displayValue = FormatValueForTextTable(value);
                return displayValue.PadRight(columnWidths[field]);
            });

            var dataLine = string.Join(" | ", dataParts);
            await writer.WriteLineAsync(dataLine.AsMemory(), cancellationToken);
            recordCount++;
        }
    }
}