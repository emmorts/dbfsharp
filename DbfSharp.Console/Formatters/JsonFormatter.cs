using System.Text.Json;
using DbfSharp.Core;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Formats DBF records as JSON using System.Text.Json for optimal performance and correctness
/// </summary>
public sealed class JsonFormatter : IDbfFormatter
{
    private readonly FormatterOptions _options;
    private readonly JsonWriterOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JsonFormatter
    /// </summary>
    /// <param name="options">Formatting options</param>
    public JsonFormatter(FormatterOptions? options = null)
    {
        _options = options ?? new FormatterOptions();
        _jsonOptions = new JsonWriterOptions
        {
            Indented = _options.PrettyPrint,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }

    /// <summary>
    /// Writes DBF records as formatted JSON to the specified TextWriter
    /// </summary>
    public async Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        // for extremely large datasets, this could be converted to streaming approach
        using var memoryStream = new MemoryStream();

        await using (var jsonWriter = new Utf8JsonWriter(memoryStream, _jsonOptions))
        {
            await WriteJsonContent(jsonWriter, records, fields, reader, cancellationToken);
        }

        memoryStream.Position = 0;
        using var streamReader = new StreamReader(memoryStream, System.Text.Encoding.UTF8);
        var jsonContent = await streamReader.ReadToEndAsync(cancellationToken);
        await writer.WriteAsync(jsonContent.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Writes the core JSON structure with proper error handling and type conversion
    /// </summary>
    private async Task WriteJsonContent(
        Utf8JsonWriter jsonWriter,
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        CancellationToken cancellationToken
    )
    {
        jsonWriter.WriteStartArray();

        await foreach (var record in ConvertToAsyncEnumerable(records, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            jsonWriter.WriteStartObject();

            foreach (var fieldName in fields)
            {
                var value = record[fieldName];
                WriteJsonProperty(jsonWriter, fieldName, value, reader);
            }

            jsonWriter.WriteEndObject();
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
                var dateFormat = _options.DateFormat ?? "yyyy-MM-ddTHH:mm:ss";
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
                // For invalid values, we could optionally include error information
                // For now, represent as null to maintain JSON validity
                writer.WriteNull(propertyName);
                break;

            case byte[] byteArray:
                // Standard approach: encode binary data as base64
                writer.WriteString(propertyName, Convert.ToBase64String(byteArray));
                break;

            // Integer types - explicitly handle to avoid boxing/unboxing overhead
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
                // Fallback for any unexpected types - convert to string safely
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

    /// <summary>
    /// Converts IEnumerable to IAsyncEnumerable for cancellation support
    /// This allows long-running operations to be cancelled gracefully
    /// </summary>
    private static async IAsyncEnumerable<T> ConvertToAsyncEnumerable<T>(
        IEnumerable<T> source,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default
    )
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;

            // Yield control periodically to allow other operations
            if (Random.Shared.Next(100) == 0) // Every ~100 records on average
            {
                await Task.Yield();
            }
        }
    }
}
