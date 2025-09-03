using System.Globalization;
using System.Text.Json;
using DbfSharp.Core;
using DbfSharp.Core.Geometry;

namespace DbfSharp.ConsoleAot.Formatters;

/// <summary>
/// Formatter for outputting shapefile features as GeoJSON
/// </summary>
public sealed class GeoJsonFormatter : IDbfFormatter, IShapefileFormatter
{
    private readonly FormatterConfiguration _configuration;

    /// <summary>
    /// Initializes a new GeoJSON formatter
    /// </summary>
    /// <param name="configuration">The formatter configuration</param>
    public GeoJsonFormatter(FormatterConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Writes DBF records as GeoJSON (for DBF-only scenarios)
    /// </summary>
    /// <param name="records">The records to format</param>
    /// <param name="fields">The field names to include</param>
    /// <param name="reader">The DBF reader</param>
    /// <param name="writer">The output writer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        // For DBF-only data, create features with null geometries
        var features = records.Select(
            (record, index) => new ShapefileFeature(index + 1, NullShape.Instance, record, 0, 0)
        );

        await WriteShapefileFeaturesAsync(features, fields, writer, cancellationToken);
    }

    /// <summary>
    /// Writes shapefile features as GeoJSON
    /// </summary>
    /// <param name="features">The features to format</param>
    /// <param name="fields">The field names to include</param>
    /// <param name="reader">The shapefile reader</param>
    /// <param name="writer">The output writer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WriteAsync(
        IEnumerable<ShapefileFeature> features,
        string[] fields,
        ShapefileReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        await WriteShapefileFeaturesAsync(features, fields, writer, cancellationToken);
    }

    /// <summary>
    /// Core method to write shapefile features as GeoJSON
    /// </summary>
    private async Task WriteShapefileFeaturesAsync(
        IEnumerable<ShapefileFeature> features,
        string[] fields,
        TextWriter writer,
        CancellationToken cancellationToken
    )
    {
        var options = new JsonWriterOptions { Indented = _configuration.PrettyPrint };

        using var memoryStream = new MemoryStream();
        using var utf8Writer = new Utf8JsonWriter(memoryStream, options);

        // Start FeatureCollection
        utf8Writer.WriteStartObject();
        utf8Writer.WriteString("type", "FeatureCollection");

        // Optional metadata
        if (_configuration.IncludeTypeInfo)
        {
            utf8Writer.WriteStartObject("metadata");
            utf8Writer.WriteString("generator", "DbfSharp");
            utf8Writer.WriteString("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            utf8Writer.WriteEndObject();
        }

        // Start features array
        utf8Writer.WriteStartArray("features");

        var processedCount = 0;
        foreach (var feature in features)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (
                _configuration.MaxDisplayRecords.HasValue
                && processedCount >= _configuration.MaxDisplayRecords.Value
            )
            {
                break;
            }

            WriteFeature(utf8Writer, feature, fields);
            processedCount++;

            // Flush periodically for large datasets
            if (processedCount % 1000 == 0)
            {
                await utf8Writer.FlushAsync();
            }
        }

        // End features array and FeatureCollection
        utf8Writer.WriteEndArray();
        utf8Writer.WriteEndObject();

        await utf8Writer.FlushAsync();

        // Write the JSON content to the output writer
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var jsonContent = await reader.ReadToEndAsync();
        await writer.WriteAsync(jsonContent);
    }

    /// <summary>
    /// Writes a single feature to the JSON writer
    /// </summary>
    private void WriteFeature(Utf8JsonWriter writer, ShapefileFeature feature, string[] fields)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "Feature");

        // Write geometry
        writer.WritePropertyName("geometry");
        WriteGeometry(writer, feature.Geometry);

        // Write properties
        writer.WriteStartObject("properties");

        if (feature.HasAttributes)
        {
            var attributes = feature.Attributes!.Value;

            foreach (var fieldName in fields)
            {
                if (feature.HasAttributeField(fieldName))
                {
                    var value = feature.GetAttribute(fieldName);
                    WriteProperty(writer, fieldName, value);
                }
            }
        }

        writer.WriteEndObject(); // properties
        writer.WriteEndObject(); // feature
    }

    /// <summary>
    /// Writes a geometry object to the JSON writer
    /// </summary>
    private void WriteGeometry(Utf8JsonWriter writer, Shape geometry)
    {
        if (geometry.IsEmpty || geometry is NullShape)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        switch (geometry)
        {
            case Point point:
                WritePoint(writer, point);
                break;
            case MultiPoint multiPoint:
                WriteMultiPoint(writer, multiPoint);
                break;
            case PolyLine polyLine:
                WritePolyLine(writer, polyLine);
                break;
            case Polygon polygon:
                WritePolygon(writer, polygon);
                break;
            case MultiPatch multiPatch:
                WriteMultiPatch(writer, multiPatch);
                break;
            default:
                // Fallback for unknown geometry types
                writer.WriteString("type", "GeometryCollection");
                writer.WriteStartArray("geometries");
                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a Point geometry
    /// </summary>
    private void WritePoint(Utf8JsonWriter writer, Point point)
    {
        writer.WriteString("type", "Point");
        writer.WritePropertyName("coordinates");
        WriteCoordinate(writer, point.Coordinate);
    }

    /// <summary>
    /// Writes a MultiPoint geometry
    /// </summary>
    private void WriteMultiPoint(Utf8JsonWriter writer, MultiPoint multiPoint)
    {
        writer.WriteString("type", "MultiPoint");
        writer.WriteStartArray("coordinates");

        foreach (var coordinate in multiPoint.Coordinates)
        {
            WriteCoordinate(writer, coordinate);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a LineString or MultiLineString geometry
    /// </summary>
    private void WritePolyLine(Utf8JsonWriter writer, PolyLine polyLine)
    {
        if (polyLine.PartCount == 1)
        {
            // Single part - write as LineString
            writer.WriteString("type", "LineString");
            writer.WritePropertyName("coordinates");
            WriteCoordinateArray(writer, polyLine.GetPart(0));
        }
        else
        {
            // Multiple parts - write as MultiLineString
            writer.WriteString("type", "MultiLineString");
            writer.WriteStartArray("coordinates");

            for (int i = 0; i < polyLine.PartCount; i++)
            {
                WriteCoordinateArray(writer, polyLine.GetPart(i));
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Writes a Polygon or MultiPolygon geometry
    /// </summary>
    private void WritePolygon(Utf8JsonWriter writer, Polygon polygon)
    {
        // For now, write all polygons as simple Polygon type
        // In the future, we could detect multiple exterior rings and use MultiPolygon
        writer.WriteString("type", "Polygon");
        writer.WriteStartArray("coordinates");

        // Write all rings (first is exterior, rest are holes)
        for (int i = 0; i < polygon.RingCount; i++)
        {
            WriteCoordinateArray(writer, polygon.GetRing(i));
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a MultiPatch geometry as a GeometryCollection of various surface types
    /// </summary>
    private void WriteMultiPatch(Utf8JsonWriter writer, MultiPatch multiPatch)
    {
        // MultiPatch doesn't have a direct GeoJSON equivalent, so we represent it as a GeometryCollection
        // with individual geometries for each patch part
        writer.WriteString("type", "GeometryCollection");
        writer.WriteStartArray("geometries");

        foreach (var part in multiPatch.GetParts())
        {
            writer.WriteStartObject();

            if (part.PatchType.IsRing())
            {
                // Ring-based patches become Polygons
                writer.WriteString("type", "Polygon");
                writer.WriteStartArray("coordinates");
                WriteCoordinateArray(writer, part.Coordinates);
                writer.WriteEndArray();
            }
            else if (part.PatchType.IsTriangle())
            {
                // Triangle-based patches become MultiPoint for simplicity
                // (GeoJSON doesn't have native support for triangle strips/fans)
                writer.WriteString("type", "MultiPoint");
                writer.WriteStartArray("coordinates");
                foreach (var coordinate in part.Coordinates)
                {
                    WriteCoordinate(writer, coordinate);
                }
                writer.WriteEndArray();
            }
            else
            {
                // Fallback to LineString for other patch types
                writer.WriteString("type", "LineString");
                writer.WritePropertyName("coordinates");
                WriteCoordinateArray(writer, part.Coordinates);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes an array of coordinates
    /// </summary>
    private void WriteCoordinateArray(Utf8JsonWriter writer, IReadOnlyList<Coordinate> coordinates)
    {
        writer.WriteStartArray();

        foreach (var coordinate in coordinates)
        {
            WriteCoordinate(writer, coordinate);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a single coordinate as [x, y] or [x, y, z] array
    /// </summary>
    private void WriteCoordinate(Utf8JsonWriter writer, Coordinate coordinate)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(coordinate.X);
        writer.WriteNumberValue(coordinate.Y);

        if (coordinate.HasZ)
        {
            writer.WriteNumberValue(coordinate.Z!.Value);
        }

        // Note: GeoJSON doesn't have a standard for M coordinates,
        // so we omit them to maintain compatibility

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a property value with appropriate JSON type
    /// </summary>
    private void WriteProperty(Utf8JsonWriter writer, string name, object? value)
    {
        writer.WritePropertyName(name);

        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string strValue:
                writer.WriteStringValue(strValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            case byte byteValue:
                writer.WriteNumberValue(byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumberValue(sbyteValue);
                break;
            case short shortValue:
                writer.WriteNumberValue(shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumberValue(ushortValue);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case uint uintValue:
                writer.WriteNumberValue(uintValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumberValue(ulongValue);
                break;
            case float floatValue:
                if (float.IsFinite(floatValue))
                {
                    writer.WriteNumberValue(floatValue);
                }
                else
                {
                    writer.WriteNullValue();
                }

                break;
            case double doubleValue:
                if (double.IsFinite(doubleValue))
                {
                    writer.WriteNumberValue(doubleValue);
                }
                else
                {
                    writer.WriteNullValue();
                }

                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            case DateTime dateTimeValue:
                var dateFormat = _configuration.DateFormat ?? "yyyy-MM-ddTHH:mm:ssZ";
                writer.WriteStringValue(
                    dateTimeValue.ToString(dateFormat, CultureInfo.InvariantCulture)
                );
                break;
            case DateOnly dateOnlyValue:
                writer.WriteStringValue(
                    dateOnlyValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                );
                break;
            case TimeOnly timeOnlyValue:
                writer.WriteStringValue(
                    timeOnlyValue.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                );
                break;
            default:
                // Fallback to string representation
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}

/// <summary>
/// Interface for formatters that can handle shapefile features
/// </summary>
public interface IShapefileFormatter
{
    /// <summary>
    /// Writes shapefile features using the formatter
    /// </summary>
    /// <param name="features">The features to format</param>
    /// <param name="fields">The field names to include</param>
    /// <param name="reader">The shapefile reader</param>
    /// <param name="writer">The output writer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WriteAsync(
        IEnumerable<ShapefileFeature> features,
        string[] fields,
        ShapefileReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    );
}
