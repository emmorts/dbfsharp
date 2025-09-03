using DbfSharp.Core.Geometry;

namespace DbfSharp.Core;

/// <summary>
/// Represents a single geometric record from a shapefile
/// </summary>
public readonly struct ShapefileRecord
{
    /// <summary>
    /// Gets the record number (1-based as per shapefile specification)
    /// </summary>
    public int RecordNumber { get; }

    /// <summary>
    /// Gets the geometry for this record
    /// </summary>
    public Shape Geometry { get; }

    /// <summary>
    /// Gets the byte offset of this record in the shapefile
    /// </summary>
    public long ByteOffset { get; }

    /// <summary>
    /// Gets the total length of this record in bytes (including the record header)
    /// </summary>
    public int TotalLengthInBytes { get; }

    /// <summary>
    /// Initializes a new shapefile record
    /// </summary>
    /// <param name="recordNumber">The record number (1-based)</param>
    /// <param name="geometry">The geometry for this record</param>
    /// <param name="byteOffset">The byte offset in the shapefile</param>
    /// <param name="totalLengthInBytes">The total record length in bytes</param>
    public ShapefileRecord(
        int recordNumber,
        Shape geometry,
        long byteOffset,
        int totalLengthInBytes
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

        if (byteOffset < 0)
        {
            throw new ArgumentException("Byte offset must be non-negative", nameof(byteOffset));
        }

        if (totalLengthInBytes < 8)
        {
            throw new ArgumentException(
                "Total length must be at least 8 bytes (record header)",
                nameof(totalLengthInBytes)
            );
        }

        RecordNumber = recordNumber;
        Geometry = geometry;
        ByteOffset = byteOffset;
        TotalLengthInBytes = totalLengthInBytes;
    }

    /// <summary>
    /// Gets the zero-based index of this record (RecordNumber - 1)
    /// </summary>
    public int RecordIndex => RecordNumber - 1;

    /// <summary>
    /// Gets the content length in bytes (excluding the 8-byte record header)
    /// </summary>
    public int ContentLengthInBytes => TotalLengthInBytes - 8;

    /// <summary>
    /// Gets the shape type of the geometry in this record
    /// </summary>
    public ShapeType ShapeType => Geometry.ShapeType;

    /// <summary>
    /// Gets the bounding box of the geometry in this record
    /// </summary>
    public BoundingBox BoundingBox => Geometry.BoundingBox;

    /// <summary>
    /// Gets a value indicating whether this record has geometric content
    /// </summary>
    public bool HasGeometry => !Geometry.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether the geometry in this record is valid
    /// </summary>
    public bool IsGeometryValid => Geometry.IsValid();

    /// <summary>
    /// Gets validation errors for the geometry in this record, if any
    /// </summary>
    /// <returns>A collection of validation error messages</returns>
    public IEnumerable<string> GetGeometryValidationErrors()
    {
        foreach (var error in Geometry.GetValidationErrors())
        {
            yield return $"Record {RecordNumber}: {error}";
        }
    }

    /// <summary>
    /// Creates a new record with a transformed geometry
    /// </summary>
    /// <param name="transform">The transformation function to apply to the geometry</param>
    /// <returns>A new record with the transformed geometry</returns>
    /// <exception cref="ArgumentNullException">Thrown when transform is null</exception>
    public ShapefileRecord Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        var transformedGeometry = Geometry.Transform(transform);
        return new ShapefileRecord(
            RecordNumber,
            transformedGeometry,
            ByteOffset,
            TotalLengthInBytes
        );
    }

    public override string ToString()
    {
        return $"Record {RecordNumber}: {Geometry} at offset {ByteOffset:N0}";
    }

    public override bool Equals(object? obj)
    {
        return obj is ShapefileRecord other
            && RecordNumber == other.RecordNumber
            && Geometry.Equals(other.Geometry)
            && ByteOffset == other.ByteOffset
            && TotalLengthInBytes == other.TotalLengthInBytes;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RecordNumber, Geometry, ByteOffset, TotalLengthInBytes);
    }

    public static bool operator ==(ShapefileRecord left, ShapefileRecord right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ShapefileRecord left, ShapefileRecord right)
    {
        return !left.Equals(right);
    }
}
