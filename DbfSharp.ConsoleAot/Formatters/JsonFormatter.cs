using System.Text.Json;
using DbfSharp.ConsoleAot.Output;
using DbfSharp.Core;
using DbfSharp.Core.Parsing;

namespace DbfSharp.ConsoleAot.Formatters;

/// <summary>
/// Formats DBF records as JSON using System.Text.Json with direct streaming to avoid memory buffering
/// </summary>
public sealed class JsonFormatter : IDbfFormatter
{
    private readonly FormatterConfiguration _configuration;
    private readonly JsonWriterOptions _jsonOptions;
    private const int DefaultChunkSize = 1000;

    /// <summary>
    /// Initializes a new instance of the JsonFormatter
    /// </summary>
    /// <param name="options">Formatting options</param>
    public JsonFormatter(FormatterConfiguration? options = null)
    {
        _configuration = options ?? new FormatterConfiguration();
        _jsonOptions = new JsonWriterOptions
        {
            Indented = _configuration.PrettyPrint,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }

    /// <summary>
    /// Writes DBF records as formatted JSON directly to the specified TextWriter's underlying stream
    /// </summary>
    public async Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        var underlyingStream = GetUnderlyingStream(writer);

        if (underlyingStream != null)
        {
            // direct stream writing - most efficient path
            await WriteToStreamDirectlyAsync(
                records,
                fields,
                reader,
                underlyingStream,
                cancellationToken
            );
        }
        else
        {
            // fallback: use a bridge that converts UTF-8 bytes to characters
            await WriteWithTextWriterBridgeAsync(
                records,
                fields,
                reader,
                writer,
                cancellationToken
            );
        }
    }

    /// <summary>
    /// Attempts to extract the underlying stream from various TextWriter implementations
    /// </summary>
    private static Stream? GetUnderlyingStream(TextWriter writer)
    {
        return writer switch
        {
            StreamWriter streamWriter => streamWriter.BaseStream,
            // todo: add other TextWriter types that expose streams
            _ => null,
        };
    }

    /// <summary>
    /// Writes JSON directly to the underlying stream for maximum performance
    /// </summary>
    private async Task WriteToStreamDirectlyAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        Stream stream,
        CancellationToken cancellationToken
    )
    {
        await using var jsonWriter = new Utf8JsonWriter(stream, _jsonOptions);
        await WriteJsonContentAsync(jsonWriter, records, fields, reader, cancellationToken);
        await jsonWriter.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes JSON using a TextWriter bridge when direct stream access is not available
    /// </summary>
    private async Task WriteWithTextWriterBridgeAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken
    )
    {
        await using var textWriterStream = new StreamBridge(writer);
        await using var jsonWriter = new Utf8JsonWriter(textWriterStream, _jsonOptions);
        await WriteJsonContentAsync(jsonWriter, records, fields, reader, cancellationToken);
        await jsonWriter.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes the core JSON structure with chunked processing for memory efficiency
    /// </summary>
    private async Task WriteJsonContentAsync(
        Utf8JsonWriter jsonWriter,
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        CancellationToken cancellationToken
    )
    {
        jsonWriter.WriteStartArray();

        var recordCount = 0;
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            jsonWriter.WriteStartObject();

            foreach (var fieldName in fields)
            {
                var value = record[fieldName];
                WriteJsonProperty(jsonWriter, fieldName, value, reader);
            }

            jsonWriter.WriteEndObject();

            recordCount++;
            if (recordCount % DefaultChunkSize == 0)
            {
                await jsonWriter.FlushAsync(cancellationToken);
            }
        }

        jsonWriter.WriteEndArray();
    }

    /// <summary>
    /// Writes a single property with comprehensive type handling and DBF-specific considerations
    /// </summary>
    private void WriteJsonProperty(
        Utf8JsonWriter writer,
        string propertyName,
        object? value,
        DbfReader reader
    )
    {
        switch (value)
        {
            case null:
                writer.WriteNull(propertyName);
                break;

            case string stringValue:
                writer.WriteString(propertyName, stringValue);
                break;

            case DateTime dateTimeValue:
                var dateFormat = _configuration.DateFormat ?? "yyyy-MM-ddTHH:mm:ss";
                writer.WriteString(propertyName, dateTimeValue.ToString(dateFormat));
                break;

            case decimal decimalValue:
                writer.WriteNumber(propertyName, decimalValue);
                break;

            case double doubleValue:
                WriteFloatingPointNumber(writer, propertyName, doubleValue);
                break;

            case float floatValue:
                WriteFloatingPointNumber(writer, propertyName, floatValue);
                break;

            case bool boolValue:
                writer.WriteBoolean(propertyName, boolValue);
                break;

            case InvalidValue invalidValue:
                // for invalid values, we could optionally include error information
                // for now, represent as null to maintain JSON validity
                writer.WriteNull(propertyName);
                break;

            case byte[] byteArray:
                // encode binary data as base64
                writer.WriteString(propertyName, Convert.ToBase64String(byteArray));
                break;

            case int intValue:
                writer.WriteNumber(propertyName, intValue);
                break;

            case long longValue:
                writer.WriteNumber(propertyName, longValue);
                break;

            case short shortValue:
                writer.WriteNumber(propertyName, shortValue);
                break;

            case byte byteValue:
                writer.WriteNumber(propertyName, byteValue);
                break;

            case sbyte sbyteValue:
                writer.WriteNumber(propertyName, sbyteValue);
                break;

            case uint uintValue:
                writer.WriteNumber(propertyName, uintValue);
                break;

            case ulong ulongValue:
                writer.WriteNumber(propertyName, ulongValue);
                break;

            case ushort ushortValue:
                writer.WriteNumber(propertyName, ushortValue);
                break;

            default:
                // fallback for any unexpected types - convert to string
                var stringRepresentation = value.ToString();
                if (stringRepresentation != null)
                {
                    writer.WriteString(propertyName, stringRepresentation);
                }
                else
                {
                    writer.WriteNull(propertyName);
                }

                break;
        }
    }

    /// <summary>
    /// Handles floating-point numbers with special value considerations (NaN, Infinity)
    /// </summary>
    private static void WriteFloatingPointNumber(
        Utf8JsonWriter writer,
        string propertyName,
        double value
    )
    {
        if (double.IsFinite(value))
        {
            writer.WriteNumber(propertyName, value);
        }
        else
        {
            // JSON doesn't support NaN or Infinity - represent as null
            // Alternative: could write as string "NaN", "Infinity", "-Infinity"
            // but null is more universally parseable
            writer.WriteNull(propertyName);
        }
    }
}
