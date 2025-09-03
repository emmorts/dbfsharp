using System.Globalization;

namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a coordinate point with optional Z (elevation) and M (measure) values
/// </summary>
public readonly struct Coordinate : IEquatable<Coordinate>
{
    /// <summary>
    /// The X coordinate (longitude in geographic systems)
    /// </summary>
    public double X { get; }

    /// <summary>
    /// The Y coordinate (latitude in geographic systems)
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// The Z coordinate (elevation/height), if present
    /// </summary>
    public double? Z { get; }

    /// <summary>
    /// The M coordinate (measure), if present
    /// </summary>
    public double? M { get; }

    /// <summary>
    /// Initializes a new 2D coordinate
    /// </summary>
    /// <param name="x">The X coordinate</param>
    /// <param name="y">The Y coordinate</param>
    public Coordinate(double x, double y)
    {
        X = x;
        Y = y;
        Z = null;
        M = null;
    }

    /// <summary>
    /// Initializes a new 3D coordinate with Z value
    /// </summary>
    /// <param name="x">The X coordinate</param>
    /// <param name="y">The Y coordinate</param>
    /// <param name="z">The Z coordinate</param>
    public Coordinate(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        M = null;
    }

    /// <summary>
    /// Initializes a new coordinate with both Z and M values
    /// </summary>
    /// <param name="x">The X coordinate</param>
    /// <param name="y">The Y coordinate</param>
    /// <param name="z">The Z coordinate</param>
    /// <param name="m">The M coordinate</param>
    public Coordinate(double x, double y, double? z, double? m)
    {
        X = x;
        Y = y;
        Z = z;
        M = m;
    }

    /// <summary>
    /// Gets a value indicating whether this coordinate has a Z (elevation) value
    /// </summary>
    public bool HasZ => Z.HasValue;

    /// <summary>
    /// Gets a value indicating whether this coordinate has an M (measure) value
    /// </summary>
    public bool HasM => M.HasValue;

    /// <summary>
    /// Gets a value indicating whether this coordinate is 3D (has Z value)
    /// </summary>
    public bool Is3D => HasZ;

    /// <summary>
    /// Gets a value indicating whether this coordinate is measured (has M value)
    /// </summary>
    public bool IsMeasured => HasM;

    /// <summary>
    /// Calculates the 2D distance to another coordinate
    /// </summary>
    /// <param name="other">The other coordinate</param>
    /// <returns>The Euclidean distance</returns>
    public double DistanceTo(Coordinate other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the 3D distance to another coordinate (if both have Z values)
    /// </summary>
    /// <param name="other">The other coordinate</param>
    /// <returns>The 3D Euclidean distance, or 2D distance if Z values are missing</returns>
    public double Distance3DTo(Coordinate other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = (Z ?? 0) - (other.Z ?? 0);

        if (!HasZ || !other.HasZ)
        {
            return Math.Sqrt(dx * dx + dy * dy);
        }

        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool Equals(Coordinate other)
    {
        return X.Equals(other.X)
            && Y.Equals(other.Y)
            && Nullable.Equals(Z, other.Z)
            && Nullable.Equals(M, other.M);
    }

    public override bool Equals(object? obj)
    {
        return obj is Coordinate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, M);
    }

    public static bool operator ==(Coordinate left, Coordinate right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Coordinate left, Coordinate right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        var parts = new List<string>
        {
            X.ToString("F6", CultureInfo.InvariantCulture),
            Y.ToString("F6", CultureInfo.InvariantCulture),
        };

        if (HasZ)
        {
            parts.Add(Z!.Value.ToString("F6", CultureInfo.InvariantCulture));
        }

        if (HasM)
        {
            parts.Add($"M:{M!.Value.ToString("F6", CultureInfo.InvariantCulture)}");
        }

        return $"({string.Join(", ", parts)})";
    }
}
