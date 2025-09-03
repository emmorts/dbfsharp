using System.Globalization;

namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a 2D bounding box with optional Z and M bounds
/// </summary>
public readonly struct BoundingBox : IEquatable<BoundingBox>
{
    /// <summary>
    /// The minimum X coordinate
    /// </summary>
    public double MinX { get; }

    /// <summary>
    /// The maximum X coordinate
    /// </summary>
    public double MaxX { get; }

    /// <summary>
    /// The minimum Y coordinate
    /// </summary>
    public double MinY { get; }

    /// <summary>
    /// The maximum Y coordinate
    /// </summary>
    public double MaxY { get; }

    /// <summary>
    /// The minimum Z coordinate, if present
    /// </summary>
    public double? MinZ { get; }

    /// <summary>
    /// The maximum Z coordinate, if present
    /// </summary>
    public double? MaxZ { get; }

    /// <summary>
    /// The minimum M coordinate, if present
    /// </summary>
    public double? MinM { get; }

    /// <summary>
    /// The maximum M coordinate, if present
    /// </summary>
    public double? MaxM { get; }

    /// <summary>
    /// Initializes a new 2D bounding box
    /// </summary>
    /// <param name="minX">The minimum X coordinate</param>
    /// <param name="minY">The minimum Y coordinate</param>
    /// <param name="maxX">The maximum X coordinate</param>
    /// <param name="maxY">The maximum Y coordinate</param>
    public BoundingBox(double minX, double minY, double maxX, double maxY)
    {
        MinX = Math.Min(minX, maxX);
        MaxX = Math.Max(minX, maxX);
        MinY = Math.Min(minY, maxY);
        MaxY = Math.Max(minY, maxY);
        MinZ = null;
        MaxZ = null;
        MinM = null;
        MaxM = null;
    }

    /// <summary>
    /// Initializes a new 3D bounding box with Z bounds
    /// </summary>
    /// <param name="minX">The minimum X coordinate</param>
    /// <param name="minY">The minimum Y coordinate</param>
    /// <param name="maxX">The maximum X coordinate</param>
    /// <param name="maxY">The maximum Y coordinate</param>
    /// <param name="minZ">The minimum Z coordinate</param>
    /// <param name="maxZ">The maximum Z coordinate</param>
    public BoundingBox(double minX, double minY, double maxX, double maxY, double minZ, double maxZ)
    {
        MinX = Math.Min(minX, maxX);
        MaxX = Math.Max(minX, maxX);
        MinY = Math.Min(minY, maxY);
        MaxY = Math.Max(minY, maxY);
        MinZ = Math.Min(minZ, maxZ);
        MaxZ = Math.Max(minZ, maxZ);
        MinM = null;
        MaxM = null;
    }

    /// <summary>
    /// Initializes a new bounding box with complete bounds including M values
    /// </summary>
    /// <param name="minX">The minimum X coordinate</param>
    /// <param name="minY">The minimum Y coordinate</param>
    /// <param name="maxX">The maximum X coordinate</param>
    /// <param name="maxY">The maximum Y coordinate</param>
    /// <param name="minZ">The minimum Z coordinate</param>
    /// <param name="maxZ">The maximum Z coordinate</param>
    /// <param name="minM">The minimum M coordinate</param>
    /// <param name="maxM">The maximum M coordinate</param>
    public BoundingBox(
        double minX,
        double minY,
        double maxX,
        double maxY,
        double? minZ,
        double? maxZ,
        double? minM,
        double? maxM
    )
    {
        MinX = Math.Min(minX, maxX);
        MaxX = Math.Max(minX, maxX);
        MinY = Math.Min(minY, maxY);
        MaxY = Math.Max(minY, maxY);
        MinZ = minZ.HasValue && maxZ.HasValue ? Math.Min(minZ.Value, maxZ.Value) : minZ;
        MaxZ = minZ.HasValue && maxZ.HasValue ? Math.Max(minZ.Value, maxZ.Value) : maxZ;
        MinM = minM.HasValue && maxM.HasValue ? Math.Min(minM.Value, maxM.Value) : minM;
        MaxM = minM.HasValue && maxM.HasValue ? Math.Max(minM.Value, maxM.Value) : maxM;
    }

    /// <summary>
    /// Gets the width of the bounding box (MaxX - MinX)
    /// </summary>
    public double Width => MaxX - MinX;

    /// <summary>
    /// Gets the height of the bounding box (MaxY - MinY)
    /// </summary>
    public double Height => MaxY - MinY;

    /// <summary>
    /// Gets the depth of the bounding box (MaxZ - MinZ), if Z bounds are present
    /// </summary>
    public double? Depth => HasZ ? MaxZ!.Value - MinZ!.Value : null;

    /// <summary>
    /// Gets the center point of the bounding box
    /// </summary>
    public Coordinate Center =>
        new(
            (MinX + MaxX) / 2,
            (MinY + MaxY) / 2,
            HasZ ? (MinZ!.Value + MaxZ!.Value) / 2 : null,
            HasM ? (MinM!.Value + MaxM!.Value) / 2 : null
        );

    /// <summary>
    /// Gets a value indicating whether this bounding box has Z bounds
    /// </summary>
    public bool HasZ => MinZ.HasValue && MaxZ.HasValue;

    /// <summary>
    /// Gets a value indicating whether this bounding box has M bounds
    /// </summary>
    public bool HasM => MinM.HasValue && MaxM.HasValue;

    /// <summary>
    /// Gets a value indicating whether this bounding box is empty (has no area)
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Gets the area of the bounding box
    /// </summary>
    public double Area => Math.Max(0, Width * Height);

    /// <summary>
    /// Determines whether this bounding box contains the specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to test</param>
    /// <returns>True if the coordinate is within this bounding box</returns>
    public bool Contains(Coordinate coordinate)
    {
        return coordinate.X >= MinX
            && coordinate.X <= MaxX
            && coordinate.Y >= MinY
            && coordinate.Y <= MaxY
            && (!HasZ || !coordinate.HasZ || (coordinate.Z >= MinZ && coordinate.Z <= MaxZ))
            && (!HasM || !coordinate.HasM || (coordinate.M >= MinM && coordinate.M <= MaxM));
    }

    /// <summary>
    /// Determines whether this bounding box intersects with another bounding box
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>True if the bounding boxes intersect</returns>
    public bool Intersects(BoundingBox other)
    {
        return MinX <= other.MaxX
            && MaxX >= other.MinX
            && MinY <= other.MaxY
            && MaxY >= other.MinY
            && (!HasZ || !other.HasZ || (MinZ <= other.MaxZ && MaxZ >= other.MinZ));
    }

    /// <summary>
    /// Creates a new bounding box that is the union of this box and another
    /// </summary>
    /// <param name="other">The other bounding box</param>
    /// <returns>A new bounding box containing both boxes</returns>
    public BoundingBox Union(BoundingBox other)
    {
        return new BoundingBox(
            Math.Min(MinX, other.MinX),
            Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX),
            Math.Max(MaxY, other.MaxY),
            HasZ && other.HasZ ? Math.Min(MinZ!.Value, other.MinZ!.Value) : null,
            HasZ && other.HasZ ? Math.Max(MaxZ!.Value, other.MaxZ!.Value) : null,
            HasM && other.HasM ? Math.Min(MinM!.Value, other.MinM!.Value) : null,
            HasM && other.HasM ? Math.Max(MaxM!.Value, other.MaxM!.Value) : null
        );
    }

    /// <summary>
    /// Creates a new bounding box expanded by the specified distance in all directions
    /// </summary>
    /// <param name="distance">The distance to expand</param>
    /// <returns>A new expanded bounding box</returns>
    public BoundingBox Expand(double distance)
    {
        return new BoundingBox(
            MinX - distance,
            MinY - distance,
            MaxX + distance,
            MaxY + distance,
            MinZ - distance,
            MaxZ + distance,
            MinM,
            MaxM
        );
    }

    /// <summary>
    /// Creates a bounding box from a collection of coordinates
    /// </summary>
    /// <param name="coordinates">The coordinates to create bounds for</param>
    /// <returns>A bounding box containing all coordinates</returns>
    /// <exception cref="ArgumentException">Thrown if no coordinates are provided</exception>
    public static BoundingBox FromCoordinates(IEnumerable<Coordinate> coordinates)
    {
        using var enumerator = coordinates.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            throw new ArgumentException(
                "Cannot create bounding box from empty coordinate collection",
                nameof(coordinates)
            );
        }

        var first = enumerator.Current;
        var minX = first.X;
        var maxX = first.X;
        var minY = first.Y;
        var maxY = first.Y;
        double? minZ = first.Z;
        double? maxZ = first.Z;
        double? minM = first.M;
        double? maxM = first.M;

        var hasZ = first.HasZ;
        var hasM = first.HasM;

        while (enumerator.MoveNext())
        {
            var coord = enumerator.Current;
            minX = Math.Min(minX, coord.X);
            maxX = Math.Max(maxX, coord.X);
            minY = Math.Min(minY, coord.Y);
            maxY = Math.Max(maxY, coord.Y);

            if (hasZ && coord.HasZ)
            {
                minZ = Math.Min(minZ!.Value, coord.Z!.Value);
                maxZ = Math.Max(maxZ!.Value, coord.Z!.Value);
            }
            else if (!coord.HasZ)
            {
                hasZ = false;
                minZ = null;
                maxZ = null;
            }

            if (hasM && coord.HasM)
            {
                minM = Math.Min(minM!.Value, coord.M!.Value);
                maxM = Math.Max(maxM!.Value, coord.M!.Value);
            }
            else if (!coord.HasM)
            {
                hasM = false;
                minM = null;
                maxM = null;
            }
        }

        return new BoundingBox(minX, minY, maxX, maxY, minZ, maxZ, minM, maxM);
    }

    public bool Equals(BoundingBox other)
    {
        return MinX.Equals(other.MinX)
            && MaxX.Equals(other.MaxX)
            && MinY.Equals(other.MinY)
            && MaxY.Equals(other.MaxY)
            && Nullable.Equals(MinZ, other.MinZ)
            && Nullable.Equals(MaxZ, other.MaxZ)
            && Nullable.Equals(MinM, other.MinM)
            && Nullable.Equals(MaxM, other.MaxM);
    }

    public override bool Equals(object? obj)
    {
        return obj is BoundingBox other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MinX, MaxX, MinY, MaxY, MinZ, MaxZ, MinM, MaxM);
    }

    public static bool operator ==(BoundingBox left, BoundingBox right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BoundingBox left, BoundingBox right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        var result =
            $"[{MinX.ToString("F6", CultureInfo.InvariantCulture)}, {MinY.ToString("F6", CultureInfo.InvariantCulture)}, "
            + $"{MaxX.ToString("F6", CultureInfo.InvariantCulture)}, {MaxY.ToString("F6", CultureInfo.InvariantCulture)}]";

        if (HasZ)
        {
            result +=
                $" Z:[{MinZ!.Value.ToString("F6", CultureInfo.InvariantCulture)}, {MaxZ!.Value.ToString("F6", CultureInfo.InvariantCulture)}]";
        }

        if (HasM)
        {
            result +=
                $" M:[{MinM!.Value.ToString("F6", CultureInfo.InvariantCulture)}, {MaxM!.Value.ToString("F6", CultureInfo.InvariantCulture)}]";
        }

        return result;
    }
}
