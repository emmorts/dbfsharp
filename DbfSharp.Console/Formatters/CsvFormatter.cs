using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Formats DBF records as CSV or TSV with proper RFC 4180 compliance and customizable delimiters
/// </summary>
public sealed class CsvFormatter : IDbfFormatter
{
    private readonly char _delimiter;
    private readonly FormatterOptions _options;

    /// <summary>
    /// Initializes a new CSV formatter with the specified delimiter
    /// </summary>
    /// <param name="delimiter">The field delimiter character (comma for CSV, tab for TSV)</param>
    /// <param name="options">Formatting options</param>
    public CsvFormatter(char delimiter = ',', FormatterOptions? options = null)
    {
        _delimiter = delimiter;
        _options = options ?? new FormatterOptions();
    }

    /// <summary>
    /// Creates a CSV formatter instance
    /// </summary>
    public static CsvFormatter Csv(FormatterOptions? options = null)
    {
        return new CsvFormatter(',', options);
    }

    /// <summary>
    /// Creates a TSV formatter instance
    /// </summary>
    public static CsvFormatter Tsv(FormatterOptions? options = null)
    {
        return new CsvFormatter('\t', options);
    }

    /// <summary>
    /// Writes DBF records as delimiter-separated values to the specified TextWriter
    /// </summary>
    public async Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        await WriteHeaderAsync(fields, reader, writer, cancellationToken);
        await WriteDataRowsAsync(records, fields, writer, cancellationToken);
    }

    /// <summary>
    /// Writes the CSV header row with optional type information
    /// </summary>
    private async Task WriteHeaderAsync(
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken
    )
    {
        var headerFields = fields.AsEnumerable();

        if (_options.IncludeTypeInfo)
        {
            headerFields = fields.Select(fieldName =>
            {
                var field = reader.FindField(fieldName);
                return field.HasValue
                    ? $"{fieldName} ({field.Value.Type.GetDescription()})"
                    : fieldName;
            });
        }

        var headerLine = string.Join(_delimiter, headerFields.Select(EscapeField));
        await writer.WriteLineAsync(headerLine.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Writes data rows with streaming approach for memory efficiency
    /// </summary>
    private async Task WriteDataRowsAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        TextWriter writer,
        CancellationToken cancellationToken
    )
    {
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fieldValues = fields.Select(fieldName =>
            {
                var value = record[fieldName];
                var formattedValue = FormatFieldValue(value);
                return EscapeField(formattedValue);
            });

            var dataLine = string.Join(_delimiter, fieldValues);
            await writer.WriteLineAsync(dataLine.AsMemory(), cancellationToken);
        }
    }

    /// <summary>
    /// Formats a field value according to CSV conventions and DBF data types
    /// </summary>
    private string FormatFieldValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string stringValue => stringValue,
            DateTime dateTimeValue => FormatDateTime(dateTimeValue),
            decimal decimalValue => decimalValue.ToString(
                "F",
                System.Globalization.CultureInfo.InvariantCulture
            ),
            double doubleValue => FormatFloatingPoint(doubleValue),
            float floatValue => FormatFloatingPoint(floatValue),
            bool boolValue => boolValue ? "True" : "False",
            InvalidValue => "<INVALID>", // Clear indication of data issues
            byte[] byteArray => $"<{byteArray.Length} bytes>", // Human-readable representation
            _ => value.ToString() ?? string.Empty,
        };
    }

    /// <summary>
    /// Formats DateTime values with configurable format string
    /// </summary>
    private string FormatDateTime(DateTime dateTime)
    {
        var format = _options.DateFormat ?? "yyyy-MM-dd HH:mm:ss";

        return dateTime.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats floating-point numbers with proper handling of special values
    /// </summary>
    private static string FormatFloatingPoint(double value)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "Infinity";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-Infinity";
        }

        return value.ToString("F", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Escapes field values according to RFC 4180 specifications
    /// Handles quotes, delimiters, and line breaks properly
    /// </summary>
    private string EscapeField(string field)
    {
        // Check if escaping is needed - RFC 4180 requires quoting if field contains:
        // - The delimiter character
        // - Double quote character
        // - Line break characters (CR or LF)
        var needsEscaping =
            field.Contains(_delimiter)
            || field.Contains('"')
            || field.Contains('\n')
            || field.Contains('\r');

        if (!needsEscaping)
        {
            return field;
        }

        // RFC 4180: Escape quotes by doubling them, then wrap entire field in quotes
        var escapedField = field.Replace("\"", "\"\"");
        return $"\"{escapedField}\"";
    }
}
