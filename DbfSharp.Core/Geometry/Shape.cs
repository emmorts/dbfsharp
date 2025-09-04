using DbfSharp.Core.Projection;

namespace DbfSharp.Core.Geometry;

/// <summary>
/// Abstract base class for all shapefile geometry types
/// </summary>
public abstract class Shape
{
    /// <summary>
    /// Gets the shape type identifier
    /// </summary>
    public abstract ShapeType ShapeType { get; }

    /// <summary>
    /// Gets the bounding box that encompasses this shape
    /// </summary>
    public abstract BoundingBox BoundingBox { get; }

    /// <summary>
    /// Gets a value indicating whether this shape is empty (has no geometric content)
    /// </summary>
    public abstract bool IsEmpty { get; }

    /// <summary>
    /// Gets a value indicating whether this shape has Z (elevation) coordinates
    /// </summary>
    public virtual bool HasZ => ShapeType.HasZ();

    /// <summary>
    /// Gets a value indicating whether this shape has M (measure) coordinates
    /// </summary>
    public virtual bool HasM => ShapeType.HasM();

    /// <summary>
    /// Gets a value indicating whether this shape is 2D (no Z or M coordinates)
    /// </summary>
    public virtual bool Is2D => ShapeType.Is2D();

    /// <summary>
    /// Gets all coordinates that make up this shape
    /// </summary>
    /// <returns>An enumerable of all coordinates in this shape</returns>
    public abstract IEnumerable<Coordinate> GetCoordinates();

    /// <summary>
    /// Gets the number of coordinate points in this shape
    /// </summary>
    public virtual int CoordinateCount => GetCoordinates().Count();

    /// <summary>
    /// Creates a copy of this shape with the specified coordinate transformation applied
    /// </summary>
    /// <param name="transform">The transformation function to apply to each coordinate</param>
    /// <returns>A new shape with transformed coordinates</returns>
    public abstract Shape Transform(Func<Coordinate, Coordinate> transform);

    /// <summary>
    /// Transforms this shape from one coordinate system to another
    /// </summary>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>A new shape with transformed coordinates</returns>
    public Shape Transform(
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        return TransformationEngine.Transform(this, sourceCoordinateSystem, targetCoordinateSystem);
    }

    /// <summary>
    /// Transforms this shape using EPSG codes
    /// </summary>
    /// <param name="sourceEpsgCode">The source EPSG code</param>
    /// <param name="targetEpsgCode">The target EPSG code</param>
    /// <returns>A new shape with transformed coordinates</returns>
    public Shape Transform(int sourceEpsgCode, int targetEpsgCode)
    {
        return TransformationEngine.Transform(this, sourceEpsgCode, targetEpsgCode);
    }

    /// <summary>
    /// Validates that the shape's geometry is well-formed
    /// </summary>
    /// <returns>True if the shape is geometrically valid</returns>
    public virtual bool IsValid()
    {
        return !IsEmpty && GetValidationErrors().Count() == 0;
    }

    /// <summary>
    /// Gets validation errors for this shape, if any
    /// </summary>
    /// <returns>A collection of validation error messages</returns>
    public virtual IEnumerable<string> GetValidationErrors()
    {
        if (IsEmpty)
        {
            yield return "Shape is empty";
        }

        // Check for duplicate consecutive coordinates
        var coordinates = GetCoordinates().ToList();
        for (var i = 1; i < coordinates.Count; i++)
        {
            if (coordinates[i].Equals(coordinates[i - 1]))
            {
                yield return $"Duplicate consecutive coordinates at positions {i - 1} and {i}";
            }
        }

        // Check for invalid coordinate values
        for (var i = 0; i < coordinates.Count; i++)
        {
            var coord = coordinates[i];
            if (double.IsNaN(coord.X) || double.IsInfinity(coord.X))
            {
                yield return $"Invalid X coordinate at position {i}: {coord.X}";
            }
            if (double.IsNaN(coord.Y) || double.IsInfinity(coord.Y))
            {
                yield return $"Invalid Y coordinate at position {i}: {coord.Y}";
            }
            if (coord.HasZ && (double.IsNaN(coord.Z!.Value) || double.IsInfinity(coord.Z!.Value)))
            {
                yield return $"Invalid Z coordinate at position {i}: {coord.Z}";
            }
            if (coord.HasM && (double.IsNaN(coord.M!.Value) || double.IsInfinity(coord.M!.Value)))
            {
                yield return $"Invalid M coordinate at position {i}: {coord.M}";
            }
        }
    }

    /// <summary>
    /// Creates a repaired version of this shape that fixes common geometric issues
    /// </summary>
    /// <returns>A new shape with geometric issues repaired, or the original shape if no repairs are needed</returns>
    public virtual Shape Repair()
    {
        var errors = GetValidationErrors().ToList();
        if (errors.Count == 0)
        {
            return this; // No repairs needed
        }

        // Remove duplicate consecutive coordinates and fix invalid values
        return Transform(coord =>
        {
            var x = double.IsNaN(coord.X) || double.IsInfinity(coord.X) ? 0.0 : coord.X;
            var y = double.IsNaN(coord.Y) || double.IsInfinity(coord.Y) ? 0.0 : coord.Y;
            var z =
                coord.HasZ && (double.IsNaN(coord.Z!.Value) || double.IsInfinity(coord.Z!.Value))
                    ? null
                    : coord.Z;
            var m =
                coord.HasM && (double.IsNaN(coord.M!.Value) || double.IsInfinity(coord.M!.Value))
                    ? null
                    : coord.M;
            return new Coordinate(x, y, z, m);
        });
    }
}
