using DbfSharp.Core.Geometry;

namespace DbfSharp.Core;

/// <summary>
/// Represents a complete feature combining geometry from a shapefile with attributes from a DBF file
/// </summary>
public readonly struct ShapefileFeature
{
    /// <summary>
    /// Gets the record number (1-based as per shapefile specification)
    /// </summary>
    public int RecordNumber { get; }

    /// <summary>
    /// Gets the geometry for this feature
    /// </summary>
    public Shape Geometry { get; }

    /// <summary>
    /// Gets the attribute data for this feature, or null if no attributes are available
    /// </summary>
    public DbfRecord? Attributes { get; }

    /// <summary>
    /// Gets the byte offset of the geometry record in the shapefile
    /// </summary>
    public long GeometryByteOffset { get; }

    /// <summary>
    /// Gets the total length of the geometry record in bytes
    /// </summary>
    public int GeometryLengthInBytes { get; }

    /// <summary>
    /// Initializes a new shapefile feature
    /// </summary>
    /// <param name="recordNumber">The record number (1-based)</param>
    /// <param name="geometry">The geometry for this feature</param>
    /// <param name="attributes">The attribute data, or null if not available</param>
    /// <param name="geometryByteOffset">The byte offset of the geometry in the shapefile</param>
    /// <param name="geometryLengthInBytes">The total length of the geometry record in bytes</param>
    public ShapefileFeature(
        int recordNumber,
        Shape geometry,
        DbfRecord? attributes,
        long geometryByteOffset,
        int geometryLengthInBytes
    )
    {
        if (recordNumber <= 0)
        {
            throw new ArgumentException("Record number must be positive", nameof(recordNumber));
        }

        if (geometry == null)
        {
            throw new ArgumentNullException(nameof(geometry));
        }

        if (geometryByteOffset < 0)
        {
            throw new ArgumentException(
                "Geometry byte offset must be non-negative",
                nameof(geometryByteOffset)
            );
        }

        if (geometryLengthInBytes < 8)
        {
            throw new ArgumentException(
                "Geometry length must be at least 8 bytes",
                nameof(geometryLengthInBytes)
            );
        }

        RecordNumber = recordNumber;
        Geometry = geometry;
        Attributes = attributes;
        GeometryByteOffset = geometryByteOffset;
        GeometryLengthInBytes = geometryLengthInBytes;
    }

    /// <summary>
    /// Initializes a new shapefile feature from a shapefile record
    /// </summary>
    /// <param name="record">The shapefile record containing the geometry</param>
    /// <param name="attributes">The attribute data, or null if not available</param>
    public ShapefileFeature(ShapefileRecord record, DbfRecord? attributes)
        : this(
            record.RecordNumber,
            record.Geometry,
            attributes,
            record.ByteOffset,
            record.TotalLengthInBytes
        ) { }

    /// <summary>
    /// Gets the zero-based index of this feature (RecordNumber - 1)
    /// </summary>
    public int RecordIndex => RecordNumber - 1;

    /// <summary>
    /// Gets the shape type of the geometry in this feature
    /// </summary>
    public ShapeType ShapeType => Geometry.ShapeType;

    /// <summary>
    /// Gets the bounding box of the geometry in this feature
    /// </summary>
    public BoundingBox BoundingBox => Geometry.BoundingBox;

    /// <summary>
    /// Gets a value indicating whether this feature has geometric content
    /// </summary>
    public bool HasGeometry => !Geometry.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether this feature has attribute data
    /// </summary>
    public bool HasAttributes => Attributes.HasValue;

    /// <summary>
    /// Gets a value indicating whether the geometry in this feature is valid
    /// </summary>
    public bool IsGeometryValid => Geometry.IsValid();

    /// <summary>
    /// Gets the number of attribute fields, or 0 if no attributes are available
    /// </summary>
    public int AttributeFieldCount => Attributes?.FieldCount ?? 0;

    /// <summary>
    /// Gets the attribute field names, or an empty collection if no attributes are available
    /// </summary>
    public IReadOnlyList<string> AttributeFieldNames =>
        Attributes?.FieldNames ?? Array.Empty<string>();

    /// <summary>
    /// Gets an attribute value by field name
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The attribute value, or null if the field doesn't exist or no attributes are available</returns>
    public object? GetAttribute(string fieldName)
    {
        if (!HasAttributes || string.IsNullOrEmpty(fieldName))
        {
            return null;
        }

        try
        {
            return Attributes!.Value[fieldName];
        }
        catch (ArgumentException)
        {
            return null; // Field not found
        }
    }

    /// <summary>
    /// Gets an attribute value by field index
    /// </summary>
    /// <param name="fieldIndex">The zero-based index of the field</param>
    /// <returns>The attribute value, or null if the index is out of range or no attributes are available</returns>
    public object? GetAttribute(int fieldIndex)
    {
        if (!HasAttributes || fieldIndex < 0 || fieldIndex >= AttributeFieldCount)
        {
            return null;
        }

        try
        {
            return Attributes!.Value[fieldIndex];
        }
        catch (ArgumentOutOfRangeException)
        {
            return null; // Index out of range
        }
    }

    /// <summary>
    /// Gets an attribute value as a string by field name
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The attribute value as a string, or null if not available</returns>
    public string? GetAttributeAsString(string fieldName)
    {
        if (!HasAttributes)
        {
            return null;
        }

        try
        {
            return Attributes!.Value.GetString(fieldName);
        }
        catch (ArgumentException)
        {
            return null; // Field not found
        }
    }

    /// <summary>
    /// Gets an attribute value as a string by field index
    /// </summary>
    /// <param name="fieldIndex">The zero-based index of the field</param>
    /// <returns>The attribute value as a string, or null if not available</returns>
    public string? GetAttributeAsString(int fieldIndex)
    {
        if (!HasAttributes || fieldIndex < 0 || fieldIndex >= AttributeFieldCount)
        {
            return null;
        }

        try
        {
            return Attributes!.Value.GetString(fieldIndex);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null; // Index out of range
        }
    }

    /// <summary>
    /// Checks if the feature has a specific attribute field
    /// </summary>
    /// <param name="fieldName">The name of the field to check</param>
    /// <returns>True if the field exists</returns>
    public bool HasAttributeField(string fieldName)
    {
        return HasAttributes
            && !string.IsNullOrEmpty(fieldName)
            && AttributeFieldNames.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all attribute values as key-value pairs
    /// </summary>
    /// <returns>A dictionary of field names to values, or empty if no attributes are available</returns>
    public Dictionary<string, object?> GetAllAttributes()
    {
        var result = new Dictionary<string, object?>();

        if (!HasAttributes)
        {
            return result;
        }

        var attributes = Attributes!.Value;
        for (int i = 0; i < attributes.FieldCount; i++)
        {
            var fieldName = attributes.FieldNames[i];
            var value = attributes[i];
            result[fieldName] = value;
        }

        return result;
    }

    /// <summary>
    /// Creates a new feature with a transformed geometry
    /// </summary>
    /// <param name="transform">The transformation function to apply to the geometry</param>
    /// <returns>A new feature with the transformed geometry</returns>
    /// <exception cref="ArgumentNullException">Thrown when transform is null</exception>
    public ShapefileFeature Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        var transformedGeometry = Geometry.Transform(transform);
        return new ShapefileFeature(
            RecordNumber,
            transformedGeometry,
            Attributes,
            GeometryByteOffset,
            GeometryLengthInBytes
        );
    }

    /// <summary>
    /// Gets the underlying shapefile record (geometry only)
    /// </summary>
    /// <returns>The shapefile record containing just the geometry</returns>
    public ShapefileRecord ToShapefileRecord()
    {
        return new ShapefileRecord(
            RecordNumber,
            Geometry,
            GeometryByteOffset,
            GeometryLengthInBytes
        );
    }

    /// <summary>
    /// Gets validation errors for this feature, if any
    /// </summary>
    /// <returns>A collection of validation error messages</returns>
    public IEnumerable<string> GetValidationErrors()
    {
        // Validate geometry
        foreach (var error in Geometry.GetValidationErrors())
        {
            yield return $"Feature {RecordNumber} geometry: {error}";
        }

        // Note: We don't validate attributes here as they're handled by DbfRecord
        // and validation rules may vary by application
    }

    public override string ToString()
    {
        var attributeInfo = HasAttributes
            ? $", {AttributeFieldCount} attributes"
            : ", no attributes";
        return $"Feature {RecordNumber}: {Geometry}{attributeInfo}";
    }

    public override bool Equals(object? obj)
    {
        return obj is ShapefileFeature other
            && RecordNumber == other.RecordNumber
            && Geometry.Equals(other.Geometry)
            && EqualityComparer<DbfRecord?>.Default.Equals(Attributes, other.Attributes)
            && GeometryByteOffset == other.GeometryByteOffset
            && GeometryLengthInBytes == other.GeometryLengthInBytes;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            RecordNumber,
            Geometry,
            Attributes,
            GeometryByteOffset,
            GeometryLengthInBytes
        );
    }

    public static bool operator ==(ShapefileFeature left, ShapefileFeature right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ShapefileFeature left, ShapefileFeature right)
    {
        return !left.Equals(right);
    }
}
