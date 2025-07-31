using System.Text;
using DbfSharp.ConsoleAot.Output.Table;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;

namespace DbfSharp.ConsoleAot.Formatters;

/// <summary>
/// High-performance table formatter with streaming support and adaptive column width calculation
/// </summary>
public sealed class TableFormatter : IDbfFormatter
{
    private readonly FormatterConfiguration _configuration;
    private const int DefaultMaxDisplayRecords = 1000;
    private const int MaxColumnWidth = 50;
    private const int MinColumnWidth = 3;
    private const int SampleSize = 100; // Number of records to sample for width calculation

    /// <summary>
    /// Initializes a new table formatter with the specified options.
    /// </summary>
    /// <param name="options">Formatting options, particularly MaxDisplayRecords for console overflow prevention.</param>
    public TableFormatter(FormatterConfiguration? options = null)
    {
        _configuration = options ?? new FormatterConfiguration();
    }

    /// <summary>
    /// Writes the DBF records as a formatted text table with streaming and adaptive width calculation
    /// </summary>
    public async Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        var headers = fields.Select(f => BuildColumnHeader(f, reader)).ToArray();
        var maxRecords = _configuration.MaxDisplayRecords ?? DefaultMaxDisplayRecords;

        // Use two-pass approach for optimal column width calculation
        var (columnWidths, recordsList, totalRecords) = CalculateOptimalColumnWidths(
            records, fields, headers, maxRecords, cancellationToken);

        if (recordsList.Count == 0)
        {
            await writer.WriteLineAsync("No records to display.");
            return;
        }

        // Create and render table using the unified TableRenderer
        await RenderTableAsync(recordsList, headers, columnWidths, totalRecords, maxRecords, writer, cancellationToken);
    }

    /// <summary>
    /// Calculates optimal column widths by sampling data and determines final record set
    /// </summary>
    private static (int[] ColumnWidths, List<string[]> Records, int TotalRecords) CalculateOptimalColumnWidths(
        IEnumerable<DbfRecord> records,
        string[] fields,
        string[] headers,
        int maxRecords,
        CancellationToken cancellationToken)
    {
        var columnWidths = headers.Select(h => Math.Max(h.Length, MinColumnWidth)).ToArray();
        var recordsList = new List<string[]>();
        var sampleRecords = new List<string[]>();
        var totalRecords = 0;

        // First pass: collect records and sample for width calculation
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowValues = BuildTableRow(record, fields);

            // Update column widths based on this row
            for (var i = 0; i < rowValues.Length && i < columnWidths.Length; i++)
            {
                var cellWidth = Math.Min(rowValues[i].Length, MaxColumnWidth);
                columnWidths[i] = Math.Max(columnWidths[i], cellWidth);
            }

            if (totalRecords < maxRecords)
            {
                recordsList.Add(rowValues);
            }
            else if (totalRecords == maxRecords)
            {
                // Add overflow indicator
                recordsList.Add(Enumerable.Repeat("...", fields.Length).ToArray());
            }

            // Keep sampling even after maxRecords for better width calculation
            if (sampleRecords.Count < SampleSize)
            {
                sampleRecords.Add(rowValues);
            }

            totalRecords++;
        }

        // Optimize column widths based on content distribution
        OptimizeColumnWidths(columnWidths, sampleRecords);

        return (columnWidths, recordsList, totalRecords);
    }

    /// <summary>
    /// Optimizes column widths based on content analysis
    /// </summary>
    private static void OptimizeColumnWidths(int[] columnWidths, List<string[]> sampleRecords)
    {
        if (sampleRecords.Count == 0) return;

        for (var colIndex = 0; colIndex < columnWidths.Length; colIndex++)
        {
            var columnValues = sampleRecords.Select(row =>
                colIndex < row.Length ? row[colIndex] : "").ToArray();

            // Calculate statistics for this column
            var avgLength = columnValues.Average(v => v.Length);
            var maxLength = columnValues.Max(v => v.Length);

            // If most values are much shorter than the max, cap the width
            if (avgLength < maxLength * 0.5 && maxLength > 20)
            {
                columnWidths[colIndex] = Math.Min(columnWidths[colIndex], (int)(avgLength * 2));
            }

            // Ensure minimum width
            columnWidths[colIndex] = Math.Max(columnWidths[colIndex], MinColumnWidth);
        }
    }

    /// <summary>
    /// Renders the complete table with optimized console output using unified TableRenderer
    /// </summary>
    private async Task RenderTableAsync(
        List<string[]> records,
        string[] headers,
        int[] columnWidths,
        int totalRecords,
        int maxRecords,
        TextWriter writer,
        CancellationToken cancellationToken)
    {
        var tableRenderer = new TableRenderer(headers, columnWidths);

        // For console output, use direct rendering
        if (writer == Console.Out)
        {
            tableRenderer.RenderToConsole(records);
        }
        else
        {
            // For file output, render to string builder then write
            var output = new StringBuilder();
            tableRenderer.RenderToStringBuilder(records, output);
            await writer.WriteAsync(output.ToString());
        }

        // Add record count and overflow information
        if (records.Count > 0 && records[^1][0] == "...")
        {
            await writer.WriteLineAsync(
                $"\nShowing {records.Count - 1:N0} of {totalRecords:N0} records (limit: {maxRecords:N0}).");
            if (_configuration.ShowWarnings)
            {
                await writer.WriteLineAsync("Use --format csv or --limit to see more records.");
            }
        }
        else
        {
            await writer.WriteLineAsync($"\nShowing {records.Count:N0} record(s).");
        }
    }

    /// <summary>
    /// Builds a column header string with optional type information.
    /// </summary>
    private string BuildColumnHeader(string fieldName, DbfReader reader)
    {
        if (!_configuration.IncludeTypeInfo)
        {
            return fieldName;
        }

        var field = reader.FindField(fieldName);
        if (!field.HasValue)
        {
            return fieldName;
        }

        var typeDescription = field.Value.Type.GetDescription();
        return $"{fieldName} ({typeDescription})";
    }

    /// <summary>
    /// Builds a single table row with properly formatted and truncated cell values.
    /// </summary>
    private static string[] BuildTableRow(DbfRecord record, string[] fields)
    {
        var rowValues = new string[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            var value = record[fields[i]];
            var formatted = FormatValueForTable(value);

            // Truncate long values for table display
            if (formatted.Length > MaxColumnWidth)
            {
                formatted = formatted[..(MaxColumnWidth - 3)] + "...";
            }

            rowValues[i] = formatted;
        }

        return rowValues;
    }

    /// <summary>
    /// Formats individual cell values for plain text table display with performance optimizations.
    /// </summary>
    private static string FormatValueForTable(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s when string.IsNullOrWhiteSpace(s) => "<empty>",
            string s => s,
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("N2"),
            double d when double.IsFinite(d) => d.ToString("N2"),
            double.NaN => "NaN",
            double d when double.IsPositiveInfinity(d) => "∞",
            double d when double.IsNegativeInfinity(d) => "-∞",
            float f when float.IsFinite(f) => f.ToString("N2"),
            float.NaN => "NaN",
            float f when float.IsPositiveInfinity(f) => "∞",
            float f when float.IsNegativeInfinity(f) => "-∞",
            bool b => b ? "✓" : "✗",
            InvalidValue => "⚠ ERROR",
            byte[] bytes => $"📄{bytes.Length}B",
            int i => i.ToString("N0"),
            long l => l.ToString("N0"),
            _ => value.ToString() ?? "NULL",
        };
    }
}
