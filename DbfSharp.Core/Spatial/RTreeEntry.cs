using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Spatial;

/// <summary>
/// Represents an entry in an R-tree spatial index
/// </summary>
public class RTreeEntry
{
    /// <summary>
    /// The bounding box of the spatial object
    /// </summary>
    public BoundingBox BoundingBox { get; }

    /// <summary>
    /// The record number in the shapefile
    /// </summary>
    public int RecordNumber { get; }

    /// <summary>
    /// Optional reference to the actual shape geometry
    /// </summary>
    public Shape? Shape { get; }

    /// <summary>
    /// Optional user data associated with this entry
    /// </summary>
    public object? UserData { get; }

    /// <summary>
    /// Initializes a new R-tree entry
    /// </summary>
    /// <param name="boundingBox">The bounding box of the spatial object</param>
    /// <param name="recordNumber">The record number in the shapefile</param>
    /// <param name="shape">Optional reference to the actual shape geometry</param>
    /// <param name="userData">Optional user data</param>
    public RTreeEntry(
        BoundingBox boundingBox,
        int recordNumber,
        Shape? shape = null,
        object? userData = null
    )
    {
        BoundingBox = boundingBox;
        RecordNumber = recordNumber;
        Shape = shape;
        UserData = userData;
    }

    /// <summary>
    /// Creates an R-tree entry from a shape and record number
    /// </summary>
    /// <param name="shape">The shape geometry</param>
    /// <param name="recordNumber">The record number</param>
    /// <param name="userData">Optional user data</param>
    /// <returns>A new R-tree entry</returns>
    public static RTreeEntry FromShape(Shape shape, int recordNumber, object? userData = null)
    {
        if (shape == null)
            throw new ArgumentNullException(nameof(shape));

        return new RTreeEntry(shape.BoundingBox, recordNumber, shape, userData);
    }

    /// <summary>
    /// Gets a value indicating whether this entry has an associated shape
    /// </summary>
    public bool HasShape => Shape != null;

    /// <summary>
    /// Returns a string representation of the RTreeEntry
    /// </summary>
    /// <returns>A string that represents the current RTreeEntry</returns>
    public override string ToString()
    {
        return $"RTreeEntry: Record {RecordNumber}, BBox: {BoundingBox}"
            + (HasShape ? $", ShapeType: {Shape!.ShapeType}" : "");
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current RTreeEntry
    /// </summary>
    /// <param name="obj">The object to compare with the current RTreeEntry</param>
    /// <returns>true if the specified object is equal to the current RTreeEntry; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return obj is RTreeEntry other
            && RecordNumber == other.RecordNumber
            && BoundingBox.Equals(other.BoundingBox);
    }

    /// <summary>
    /// Serves as the default hash function for RTreeEntry objects
    /// </summary>
    /// <returns>A hash code for the current RTreeEntry</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(RecordNumber, BoundingBox);
    }
}
