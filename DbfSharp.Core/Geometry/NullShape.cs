namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a null shape with no geometric content
/// </summary>
public sealed class NullShape : Shape
{
    /// <summary>
    /// The singleton instance of NullShape
    /// </summary>
    public static readonly NullShape Instance = new();

    private NullShape() { }

    /// <inheritdoc />
    public override ShapeType ShapeType => ShapeType.NullShape;

    /// <inheritdoc />
    public override BoundingBox BoundingBox => new(0, 0, 0, 0);

    /// <inheritdoc />
    public override bool IsEmpty => true;

    /// <inheritdoc />
    public override IEnumerable<Coordinate> GetCoordinates()
    {
        return [];
    }

    /// <inheritdoc />
    public override int CoordinateCount => 0;

    /// <inheritdoc />
    public override Shape Transform(Func<Coordinate, Coordinate> transform)
    {
        return this; // Null shape is immutable and transformations don't affect it
    }

    /// <inheritdoc />
    public override bool IsValid() => true; // Null shapes are always valid

    /// <inheritdoc />
    public override IEnumerable<string> GetValidationErrors()
    {
        return []; // No validation errors for null shapes
    }

    /// <summary>
    /// Returns a string representation of the NullShape
    /// </summary>
    /// <returns>A string that represents the current NullShape</returns>
    public override string ToString() => "NULL";
}
