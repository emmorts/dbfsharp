namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a point geometry with a single coordinate
/// </summary>
public sealed class Point : Shape
{
    /// <summary>
    /// Gets the coordinate of this point
    /// </summary>
    public Coordinate Coordinate { get; }

    /// <summary>
    /// Initializes a new point with the specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate for this point</param>
    public Point(Coordinate coordinate)
    {
        Coordinate = coordinate;
    }

    /// <summary>
    /// Initializes a new 2D point with X and Y coordinates
    /// </summary>
    /// <param name="x">The X coordinate</param>
    /// <param name="y">The Y coordinate</param>
    public Point(double x, double y)
        : this(new Coordinate(x, y)) { }

    /// <summary>
    /// Initializes a new 3D point with X, Y, and Z coordinates
    /// </summary>
    /// <param name="x">The X coordinate</param>
    /// <param name="y">The Y coordinate</param>
    /// <param name="z">The Z coordinate</param>
    public Point(double x, double y, double z)
        : this(new Coordinate(x, y, z)) { }

    /// <summary>
    /// Initializes a new point with X, Y, Z, and M coordinates
    /// </summary>
    /// <param name="x">The X coordinate</param>
    /// <param name="y">The Y coordinate</param>
    /// <param name="z">The Z coordinate (nullable)</param>
    /// <param name="m">The M coordinate (nullable)</param>
    public Point(double x, double y, double? z, double? m)
        : this(new Coordinate(x, y, z, m)) { }

    /// <inheritdoc />
    public override ShapeType ShapeType
    {
        get
        {
            if (Coordinate is { HasZ: true, HasM: true })
            {
                // Note: Shapefile spec doesn't have a combined ZM type, so we prioritize Z
                return ShapeType.PointZ;
            }

            return Coordinate.HasZ ? ShapeType.PointZ
                : Coordinate.HasM ? ShapeType.PointM
                : ShapeType.Point;
        }
    }

    /// <inheritdoc />
    public override BoundingBox BoundingBox =>
        new(
            Coordinate.X,
            Coordinate.Y,
            Coordinate.X,
            Coordinate.Y,
            Coordinate.Z,
            Coordinate.Z,
            Coordinate.M,
            Coordinate.M
        );

    /// <inheritdoc />
    public override bool IsEmpty => false; // Points are never empty in shapefiles

    /// <summary>
    /// Gets the X coordinate of this point
    /// </summary>
    public double X => Coordinate.X;

    /// <summary>
    /// Gets the Y coordinate of this point
    /// </summary>
    public double Y => Coordinate.Y;

    /// <summary>
    /// Gets the Z coordinate of this point, if present
    /// </summary>
    public double? Z => Coordinate.Z;

    /// <summary>
    /// Gets the M coordinate of this point, if present
    /// </summary>
    public double? M => Coordinate.M;

    /// <inheritdoc />
    public override IEnumerable<Coordinate> GetCoordinates()
    {
        yield return Coordinate;
    }

    /// <inheritdoc />
    public override int CoordinateCount => 1;

    /// <summary>
    /// Calculates the 2D distance to another point
    /// </summary>
    /// <param name="other">The other point</param>
    /// <returns>The Euclidean distance</returns>
    public double DistanceTo(Point other)
    {
        return Coordinate.DistanceTo(other.Coordinate);
    }

    /// <summary>
    /// Calculates the 3D distance to another point (if both have Z values)
    /// </summary>
    /// <param name="other">The other point</param>
    /// <returns>The 3D Euclidean distance, or 2D distance if Z values are missing</returns>
    public double Distance3DTo(Point other)
    {
        return Coordinate.Distance3DTo(other.Coordinate);
    }

    /// <inheritdoc />
    public override Shape Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        return new Point(transform(Coordinate));
    }

    /// <summary>
    /// Creates a new point with the specified offset applied
    /// </summary>
    /// <param name="deltaX">The X offset</param>
    /// <param name="deltaY">The Y offset</param>
    /// <param name="deltaZ">The Z offset (optional)</param>
    /// <returns>A new point with the offset applied</returns>
    public Point Offset(double deltaX, double deltaY, double? deltaZ = null)
    {
        return new Point(
            Coordinate.X + deltaX,
            Coordinate.Y + deltaY,
            Coordinate.HasZ ? Coordinate.Z + (deltaZ ?? 0) : null,
            Coordinate.M
        );
    }

    /// <inheritdoc />
    public override bool IsValid() => true; // Points are always valid

    /// <inheritdoc />
    public override IEnumerable<string> GetValidationErrors()
    {
        if (double.IsNaN(Coordinate.X) || double.IsInfinity(Coordinate.X))
        {
            yield return "Point X coordinate is not a valid number";
        }

        if (double.IsNaN(Coordinate.Y) || double.IsInfinity(Coordinate.Y))
        {
            yield return "Point Y coordinate is not a valid number";
        }

        if (
            Coordinate.HasZ
            && (double.IsNaN(Coordinate.Z!.Value) || double.IsInfinity(Coordinate.Z!.Value))
        )
        {
            yield return "Point Z coordinate is not a valid number";
        }

        if (
            Coordinate.HasM
            && (double.IsNaN(Coordinate.M!.Value) || double.IsInfinity(Coordinate.M!.Value))
        )
        {
            yield return "Point M coordinate is not a valid number";
        }
    }

    /// <summary>
    /// Returns a string representation of the Point
    /// </summary>
    /// <returns>A string that represents the current Point</returns>
    public override string ToString()
    {
        return $"POINT {Coordinate}";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Point
    /// </summary>
    /// <param name="obj">The object to compare with the current Point</param>
    /// <returns>true if the specified object is equal to the current Point; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return obj is Point other && Coordinate.Equals(other.Coordinate);
    }

    /// <summary>
    /// Serves as the default hash function for Point objects
    /// </summary>
    /// <returns>A hash code for the current Point</returns>
    public override int GetHashCode()
    {
        return Coordinate.GetHashCode();
    }
}
