namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a collection of point geometries
/// </summary>
public sealed class MultiPoint : Shape
{
    private readonly Coordinate[] _coordinates;
    private readonly BoundingBox _boundingBox;

    /// <summary>
    /// Gets the coordinates of all points in this multipoint geometry
    /// </summary>
    public IReadOnlyList<Coordinate> Coordinates => _coordinates;

    /// <summary>
    /// Initializes a new multipoint with the specified coordinates
    /// </summary>
    /// <param name="coordinates">The coordinates for the points</param>
    /// <exception cref="ArgumentNullException">Thrown if coordinates is null</exception>
    public MultiPoint(IEnumerable<Coordinate> coordinates)
    {
        if (coordinates == null)
        {
            throw new ArgumentNullException(nameof(coordinates));
        }

        _coordinates = coordinates.ToArray();

        if (_coordinates.Length == 0)
        {
            _boundingBox = new BoundingBox(0, 0, 0, 0);
        }
        else
        {
            _boundingBox = BoundingBox.FromCoordinates(_coordinates);
        }
    }

    /// <summary>
    /// Initializes a new multipoint from a collection of Point objects
    /// </summary>
    /// <param name="points">The points to include</param>
    public MultiPoint(IEnumerable<Point> points)
        : this(points?.Select(p => p.Coordinate) ?? throw new ArgumentNullException(nameof(points)))
    { }

    /// <summary>
    /// Initializes a new multipoint with the specified coordinate array
    /// </summary>
    /// <param name="coordinates">The coordinates for the points</param>
    public MultiPoint(params Coordinate[] coordinates)
        : this((IEnumerable<Coordinate>)coordinates) { }

    /// <inheritdoc />
    public override ShapeType ShapeType
    {
        get
        {
            if (_coordinates.Length == 0)
            {
                return ShapeType.MultiPoint;
            }

            var hasZ = _coordinates.Any(c => c.HasZ);
            var hasM = _coordinates.Any(c => c.HasM);

            if (hasZ && hasM)
            {
                // Note: Shapefile spec doesn't have a combined ZM type, so we prioritize Z
                return ShapeType.MultiPointZ;
            }

            return hasZ ? ShapeType.MultiPointZ
                : hasM ? ShapeType.MultiPointM
                : ShapeType.MultiPoint;
        }
    }

    /// <inheritdoc />
    public override BoundingBox BoundingBox => _boundingBox;

    /// <inheritdoc />
    public override bool IsEmpty => _coordinates.Length == 0;

    /// <summary>
    /// Gets the number of points in this multipoint geometry
    /// </summary>
    public int PointCount => _coordinates.Length;

    /// <summary>
    /// Gets the point at the specified index
    /// </summary>
    /// <param name="index">The zero-based index of the point</param>
    /// <returns>The point at the specified index</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range</exception>
    public Point GetPoint(int index)
    {
        if (index < 0 || index >= _coordinates.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Point(_coordinates[index]);
    }

    /// <summary>
    /// Gets all points as Point objects
    /// </summary>
    /// <returns>An enumerable of Point objects</returns>
    public IEnumerable<Point> GetPoints()
    {
        return _coordinates.Select(c => new Point(c));
    }

    /// <inheritdoc />
    public override IEnumerable<Coordinate> GetCoordinates()
    {
        return _coordinates;
    }

    /// <inheritdoc />
    public override int CoordinateCount => _coordinates.Length;

    /// <inheritdoc />
    public override Shape Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        return new MultiPoint(_coordinates.Select(transform));
    }

    /// <summary>
    /// Creates a new multipoint with an additional point
    /// </summary>
    /// <param name="coordinate">The coordinate to add</param>
    /// <returns>A new multipoint containing all existing points plus the new one</returns>
    public MultiPoint AddPoint(Coordinate coordinate)
    {
        return new MultiPoint(_coordinates.Concat([coordinate]));
    }

    /// <summary>
    /// Creates a new multipoint with an additional point
    /// </summary>
    /// <param name="point">The point to add</param>
    /// <returns>A new multipoint containing all existing points plus the new one</returns>
    public MultiPoint AddPoint(Point point)
    {
        return AddPoint(point.Coordinate);
    }

    /// <summary>
    /// Creates a new multipoint with the specified points removed
    /// </summary>
    /// <param name="predicate">A function to test each coordinate for removal</param>
    /// <returns>A new multipoint with matching coordinates removed</returns>
    public MultiPoint RemoveWhere(Func<Coordinate, bool> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return new MultiPoint(_coordinates.Where(c => !predicate(c)));
    }

    /// <summary>
    /// Finds the closest point to the specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to find the closest point to</param>
    /// <returns>The closest point and its distance, or null if this multipoint is empty</returns>
    public (Point Point, double Distance)? FindClosestPoint(Coordinate coordinate)
    {
        if (_coordinates.Length == 0)
        {
            return null;
        }

        var minDistance = double.MaxValue;
        var closestCoordinate = _coordinates[0];

        foreach (var coord in _coordinates)
        {
            var distance = coordinate.DistanceTo(coord);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCoordinate = coord;
            }
        }

        return (new Point(closestCoordinate), minDistance);
    }

    /// <inheritdoc />
    public override bool IsValid()
    {
        return _coordinates.All(c =>
            !double.IsNaN(c.X)
            && !double.IsInfinity(c.X)
            && !double.IsNaN(c.Y)
            && !double.IsInfinity(c.Y)
            && (!c.HasZ || (!double.IsNaN(c.Z!.Value) && !double.IsInfinity(c.Z!.Value)))
            && (!c.HasM || (!double.IsNaN(c.M!.Value) && !double.IsInfinity(c.M!.Value)))
        );
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetValidationErrors()
    {
        for (int i = 0; i < _coordinates.Length; i++)
        {
            var coord = _coordinates[i];

            if (double.IsNaN(coord.X) || double.IsInfinity(coord.X))
            {
                yield return $"Point {i}: X coordinate is not a valid number";
            }

            if (double.IsNaN(coord.Y) || double.IsInfinity(coord.Y))
            {
                yield return $"Point {i}: Y coordinate is not a valid number";
            }

            if (coord.HasZ && (double.IsNaN(coord.Z!.Value) || double.IsInfinity(coord.Z!.Value)))
            {
                yield return $"Point {i}: Z coordinate is not a valid number";
            }

            if (coord.HasM && (double.IsNaN(coord.M!.Value) || double.IsInfinity(coord.M!.Value)))
            {
                yield return $"Point {i}: M coordinate is not a valid number";
            }
        }
    }

    public override string ToString()
    {
        return $"MULTIPOINT ({_coordinates.Length} points)";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MultiPoint other || _coordinates.Length != other._coordinates.Length)
        {
            return false;
        }

        for (int i = 0; i < _coordinates.Length; i++)
        {
            if (!_coordinates[i].Equals(other._coordinates[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var coordinate in _coordinates)
        {
            hash.Add(coordinate);
        }
        return hash.ToHashCode();
    }
}
