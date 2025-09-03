namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a polygon geometry consisting of one or more rings (exterior and interior holes)
/// </summary>
public sealed class Polygon : Shape
{
    private readonly Coordinate[][] _rings;
    private readonly BoundingBox _boundingBox;

    /// <summary>
    /// Gets the rings of this polygon (first ring is exterior, subsequent rings are holes)
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Coordinate>> Rings { get; }

    /// <summary>
    /// Initializes a new polygon with the specified rings
    /// </summary>
    /// <param name="rings">The rings of the polygon (first is exterior, others are holes)</param>
    /// <exception cref="ArgumentNullException">Thrown if rings is null</exception>
    /// <exception cref="ArgumentException">Thrown if any ring has fewer than 4 coordinates or is not closed</exception>
    public Polygon(IEnumerable<IEnumerable<Coordinate>> rings)
    {
        if (rings == null)
        {
            throw new ArgumentNullException(nameof(rings));
        }

        _rings = rings
            .Select(ring =>
            {
                var ringArray =
                    ring?.ToArray()
                    ?? throw new ArgumentNullException(nameof(rings), "Ring cannot be null");
                if (ringArray.Length < 4)
                {
                    throw new ArgumentException(
                        "Each polygon ring must have at least 4 coordinates",
                        nameof(rings)
                    );
                }

                // Ensure ring is closed (first and last coordinates are the same)
                if (!ringArray[0].Equals(ringArray[^1]))
                {
                    // Auto-close the ring
                    ringArray = ringArray.Concat([ringArray[0]]).ToArray();
                }

                return ringArray;
            })
            .ToArray();

        Rings = _rings.Select(ring => (IReadOnlyList<Coordinate>)ring).ToArray();

        if (_rings.Length == 0)
        {
            _boundingBox = new BoundingBox(0, 0, 0, 0);
        }
        else
        {
            var allCoordinates = _rings.SelectMany(ring => ring);
            _boundingBox = BoundingBox.FromCoordinates(allCoordinates);
        }
    }

    /// <summary>
    /// Initializes a new single-ring polygon with the specified coordinates
    /// </summary>
    /// <param name="coordinates">The coordinates for the exterior ring</param>
    public Polygon(IEnumerable<Coordinate> coordinates)
        : this([coordinates]) { }

    /// <summary>
    /// Initializes a new single-ring polygon with the specified coordinate array
    /// </summary>
    /// <param name="coordinates">The coordinates for the exterior ring</param>
    public Polygon(params Coordinate[] coordinates)
        : this((IEnumerable<Coordinate>)coordinates) { }

    /// <inheritdoc />
    public override ShapeType ShapeType
    {
        get
        {
            if (_rings.Length == 0)
            {
                return ShapeType.Polygon;
            }

            var allCoordinates = _rings.SelectMany(ring => ring);
            var hasZ = allCoordinates.Any(c => c.HasZ);
            var hasM = allCoordinates.Any(c => c.HasM);

            if (hasZ && hasM)
            {
                // Note: Shapefile spec doesn't have a combined ZM type, so we prioritize Z
                return ShapeType.PolygonZ;
            }

            return hasZ ? ShapeType.PolygonZ
                : hasM ? ShapeType.PolygonM
                : ShapeType.Polygon;
        }
    }

    /// <inheritdoc />
    public override BoundingBox BoundingBox => _boundingBox;

    /// <inheritdoc />
    public override bool IsEmpty => _rings.Length == 0;

    /// <summary>
    /// Gets the number of rings in this polygon
    /// </summary>
    public int RingCount => _rings.Length;

    /// <summary>
    /// Gets the exterior ring of the polygon
    /// </summary>
    public IReadOnlyList<Coordinate>? ExteriorRing => _rings.Length > 0 ? _rings[0] : null;

    /// <summary>
    /// Gets the interior rings (holes) of the polygon
    /// </summary>
    public IEnumerable<IReadOnlyList<Coordinate>> InteriorRings
    {
        get
        {
            for (int i = 1; i < _rings.Length; i++)
            {
                yield return _rings[i];
            }
        }
    }

    /// <summary>
    /// Gets the number of interior rings (holes) in this polygon
    /// </summary>
    public int InteriorRingCount => Math.Max(0, _rings.Length - 1);

    /// <summary>
    /// Gets the coordinates for the specified ring
    /// </summary>
    /// <param name="ringIndex">The zero-based index of the ring</param>
    /// <returns>The coordinates for the specified ring</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the ring index is out of range</exception>
    public IReadOnlyList<Coordinate> GetRing(int ringIndex)
    {
        if (ringIndex < 0 || ringIndex >= _rings.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(ringIndex));
        }

        return _rings[ringIndex];
    }

    /// <inheritdoc />
    public override IEnumerable<Coordinate> GetCoordinates()
    {
        return _rings.SelectMany(ring => ring);
    }

    /// <inheritdoc />
    public override int CoordinateCount => _rings.Sum(ring => ring.Length);

    /// <summary>
    /// Calculates the area of the polygon (exterior ring area minus interior ring areas)
    /// </summary>
    /// <returns>The signed area of the polygon (positive for counter-clockwise exterior rings)</returns>
    public double Area
    {
        get
        {
            if (_rings.Length == 0)
            {
                return 0;
            }

            var area = CalculateRingArea(_rings[0]);

            // Subtract interior ring areas
            for (int i = 1; i < _rings.Length; i++)
            {
                area -= Math.Abs(CalculateRingArea(_rings[i]));
            }

            return Math.Abs(area);
        }
    }

    /// <summary>
    /// Calculates the perimeter of the polygon (sum of all ring perimeters)
    /// </summary>
    /// <returns>The total perimeter length</returns>
    public double Perimeter
    {
        get
        {
            double totalPerimeter = 0;

            foreach (var ring in _rings)
            {
                for (int i = 1; i < ring.Length; i++)
                {
                    totalPerimeter += ring[i - 1].DistanceTo(ring[i]);
                }
            }

            return totalPerimeter;
        }
    }

    /// <summary>
    /// Determines whether the polygon contains the specified point
    /// Uses the ray casting algorithm for point-in-polygon testing
    /// </summary>
    /// <param name="point">The point to test</param>
    /// <returns>True if the point is inside the polygon</returns>
    public bool Contains(Coordinate point)
    {
        if (_rings.Length == 0)
        {
            return false;
        }

        // Check if point is inside exterior ring
        if (!IsPointInRing(point, _rings[0]))
        {
            return false;
        }

        // Check if point is inside any interior ring (hole)
        for (int i = 1; i < _rings.Length; i++)
        {
            if (IsPointInRing(point, _rings[i]))
            {
                return false; // Point is in a hole
            }
        }

        return true;
    }

    private static bool IsPointInRing(Coordinate point, Coordinate[] ring)
    {
        bool inside = false;
        int j = ring.Length - 1;

        for (int i = 0; i < ring.Length; j = i++)
        {
            if (
                ((ring[i].Y > point.Y) != (ring[j].Y > point.Y))
                && (
                    point.X
                    < (ring[j].X - ring[i].X) * (point.Y - ring[i].Y) / (ring[j].Y - ring[i].Y)
                        + ring[i].X
                )
            )
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static double CalculateRingArea(Coordinate[] ring)
    {
        if (ring.Length < 3)
        {
            return 0;
        }

        double area = 0;
        int j = ring.Length - 1;

        for (int i = 0; i < ring.Length; j = i++)
        {
            area += (ring[j].X + ring[i].X) * (ring[j].Y - ring[i].Y);
        }

        return area / 2.0;
    }

    /// <summary>
    /// Determines whether the exterior ring is oriented counter-clockwise (standard orientation)
    /// </summary>
    /// <returns>True if the exterior ring is counter-clockwise</returns>
    public bool IsCounterClockwise()
    {
        if (_rings.Length == 0)
        {
            return true;
        }

        return CalculateRingArea(_rings[0]) > 0;
    }

    /// <inheritdoc />
    public override Shape Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        var transformedRings = _rings.Select(ring => ring.Select(transform));
        return new Polygon(transformedRings);
    }

    /// <summary>
    /// Creates a new polygon with an additional interior ring (hole)
    /// </summary>
    /// <param name="coordinates">The coordinates for the new interior ring</param>
    /// <returns>A new polygon containing all existing rings plus the new interior ring</returns>
    public Polygon AddInteriorRing(IEnumerable<Coordinate> coordinates)
    {
        if (coordinates == null)
        {
            throw new ArgumentNullException(nameof(coordinates));
        }

        if (_rings.Length == 0)
        {
            throw new InvalidOperationException(
                "Cannot add interior ring to polygon without exterior ring"
            );
        }

        var newRings = _rings.Concat([coordinates.ToArray()]);
        return new Polygon(newRings);
    }

    /// <summary>
    /// Creates a new polygon with the specified ring removed
    /// </summary>
    /// <param name="ringIndex">The zero-based index of the ring to remove</param>
    /// <returns>A new polygon with the specified ring removed</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the ring index is out of range</exception>
    /// <exception cref="InvalidOperationException">Thrown if attempting to remove the exterior ring when interior rings exist</exception>
    public Polygon RemoveRing(int ringIndex)
    {
        if (ringIndex < 0 || ringIndex >= _rings.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(ringIndex));
        }

        if (ringIndex == 0 && _rings.Length > 1)
        {
            throw new InvalidOperationException(
                "Cannot remove exterior ring when interior rings exist"
            );
        }

        var newRings = _rings.Where((_, index) => index != ringIndex);
        return new Polygon(newRings);
    }

    /// <inheritdoc />
    public override bool IsValid()
    {
        if (_rings.Length == 0)
        {
            return true;
        }

        // Check each ring
        foreach (var ring in _rings)
        {
            if (ring.Length < 4)
            {
                return false;
            }

            // Check if ring is closed
            if (!ring[0].Equals(ring[^1]))
            {
                return false;
            }

            // Check for valid coordinates
            if (
                ring.Any(c =>
                    double.IsNaN(c.X)
                    || double.IsInfinity(c.X)
                    || double.IsNaN(c.Y)
                    || double.IsInfinity(c.Y)
                    || (c.HasZ && (double.IsNaN(c.Z!.Value) || double.IsInfinity(c.Z!.Value)))
                    || (c.HasM && (double.IsNaN(c.M!.Value) || double.IsInfinity(c.M!.Value)))
                )
            )
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetValidationErrors()
    {
        for (int ringIndex = 0; ringIndex < _rings.Length; ringIndex++)
        {
            var ring = _rings[ringIndex];
            var ringType = ringIndex == 0 ? "Exterior" : "Interior";

            if (ring.Length < 4)
            {
                yield return $"{ringType} ring {ringIndex}: Must have at least 4 coordinates";
                continue;
            }

            if (!ring[0].Equals(ring[^1]))
            {
                yield return $"{ringType} ring {ringIndex}: Ring is not closed (first and last coordinates must be the same)";
            }

            for (int coordIndex = 0; coordIndex < ring.Length; coordIndex++)
            {
                var coord = ring[coordIndex];

                if (double.IsNaN(coord.X) || double.IsInfinity(coord.X))
                {
                    yield return $"{ringType} ring {ringIndex}, Coordinate {coordIndex}: X coordinate is not a valid number";
                }

                if (double.IsNaN(coord.Y) || double.IsInfinity(coord.Y))
                {
                    yield return $"{ringType} ring {ringIndex}, Coordinate {coordIndex}: Y coordinate is not a valid number";
                }

                if (
                    coord.HasZ
                    && (double.IsNaN(coord.Z!.Value) || double.IsInfinity(coord.Z!.Value))
                )
                {
                    yield return $"{ringType} ring {ringIndex}, Coordinate {coordIndex}: Z coordinate is not a valid number";
                }

                if (
                    coord.HasM
                    && (double.IsNaN(coord.M!.Value) || double.IsInfinity(coord.M!.Value))
                )
                {
                    yield return $"{ringType} ring {ringIndex}, Coordinate {coordIndex}: M coordinate is not a valid number";
                }
            }
        }
    }

    /// <summary>
    /// Returns a string representation of the Polygon
    /// </summary>
    /// <returns>A string that represents the current Polygon</returns>
    public override string ToString()
    {
        var holes = InteriorRingCount > 0 ? $", {InteriorRingCount} holes" : "";
        return $"POLYGON ({_rings.Length} rings, {CoordinateCount} coordinates{holes})";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Polygon
    /// </summary>
    /// <param name="obj">The object to compare with the current Polygon</param>
    /// <returns>true if the specified object is equal to the current Polygon; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Polygon other || _rings.Length != other._rings.Length)
        {
            return false;
        }

        for (int i = 0; i < _rings.Length; i++)
        {
            var thisRing = _rings[i];
            var otherRing = other._rings[i];

            if (thisRing.Length != otherRing.Length)
            {
                return false;
            }

            for (int j = 0; j < thisRing.Length; j++)
            {
                if (!thisRing[j].Equals(otherRing[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Serves as the default hash function for Polygon objects
    /// </summary>
    /// <returns>A hash code for the current Polygon</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var ring in _rings)
        {
            foreach (var coordinate in ring)
            {
                hash.Add(coordinate);
            }
        }
        return hash.ToHashCode();
    }
}
