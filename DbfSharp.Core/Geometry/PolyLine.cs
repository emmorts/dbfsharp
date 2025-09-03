namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a polyline geometry consisting of one or more connected line segments (parts)
/// </summary>
public sealed class PolyLine : Shape
{
    private readonly Coordinate[][] _parts;
    private readonly BoundingBox _boundingBox;

    /// <summary>
    /// Gets the parts (individual line segments) of this polyline
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Coordinate>> Parts { get; }

    /// <summary>
    /// Initializes a new polyline with the specified parts
    /// </summary>
    /// <param name="parts">The parts (line segments) of the polyline</param>
    /// <exception cref="ArgumentNullException">Thrown if parts is null</exception>
    /// <exception cref="ArgumentException">Thrown if any part has fewer than 2 coordinates</exception>
    public PolyLine(IEnumerable<IEnumerable<Coordinate>> parts)
    {
        if (parts == null)
        {
            throw new ArgumentNullException(nameof(parts));
        }

        _parts = parts
            .Select(part =>
            {
                var partArray =
                    part?.ToArray()
                    ?? throw new ArgumentNullException(nameof(parts), "Part cannot be null");
                if (partArray.Length < 2)
                {
                    throw new ArgumentException(
                        "Each polyline part must have at least 2 coordinates",
                        nameof(parts)
                    );
                }

                return partArray;
            })
            .ToArray();

        Parts = _parts.Select(part => (IReadOnlyList<Coordinate>)part).ToArray();

        if (_parts.Length == 0)
        {
            _boundingBox = new BoundingBox(0, 0, 0, 0);
        }
        else
        {
            var allCoordinates = _parts.SelectMany(part => part);
            _boundingBox = BoundingBox.FromCoordinates(allCoordinates);
        }
    }

    /// <summary>
    /// Initializes a new single-part polyline with the specified coordinates
    /// </summary>
    /// <param name="coordinates">The coordinates for the single line segment</param>
    public PolyLine(IEnumerable<Coordinate> coordinates)
        : this([coordinates]) { }

    /// <summary>
    /// Initializes a new single-part polyline with the specified coordinate array
    /// </summary>
    /// <param name="coordinates">The coordinates for the single line segment</param>
    public PolyLine(params Coordinate[] coordinates)
        : this((IEnumerable<Coordinate>)coordinates) { }

    /// <inheritdoc />
    public override ShapeType ShapeType
    {
        get
        {
            if (_parts.Length == 0)
            {
                return ShapeType.PolyLine;
            }

            var allCoordinates = _parts.SelectMany(part => part);
            var hasZ = allCoordinates.Any(c => c.HasZ);
            var hasM = allCoordinates.Any(c => c.HasM);

            if (hasZ && hasM)
            {
                // Note: Shapefile spec doesn't have a combined ZM type, so we prioritize Z
                return ShapeType.PolyLineZ;
            }

            return hasZ ? ShapeType.PolyLineZ
                : hasM ? ShapeType.PolyLineM
                : ShapeType.PolyLine;
        }
    }

    /// <inheritdoc />
    public override BoundingBox BoundingBox => _boundingBox;

    /// <inheritdoc />
    public override bool IsEmpty => _parts.Length == 0;

    /// <summary>
    /// Gets the number of parts in this polyline
    /// </summary>
    public int PartCount => _parts.Length;

    /// <summary>
    /// Gets the coordinates for the specified part
    /// </summary>
    /// <param name="partIndex">The zero-based index of the part</param>
    /// <returns>The coordinates for the specified part</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the part index is out of range</exception>
    public IReadOnlyList<Coordinate> GetPart(int partIndex)
    {
        if (partIndex < 0 || partIndex >= _parts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(partIndex));
        }

        return _parts[partIndex];
    }

    /// <inheritdoc />
    public override IEnumerable<Coordinate> GetCoordinates()
    {
        return _parts.SelectMany(part => part);
    }

    /// <inheritdoc />
    public override int CoordinateCount => _parts.Sum(part => part.Length);

    /// <summary>
    /// Calculates the total length of all parts in the polyline
    /// </summary>
    /// <returns>The total 2D length</returns>
    public double Length
    {
        get
        {
            double totalLength = 0;

            foreach (var part in _parts)
            {
                for (int i = 1; i < part.Length; i++)
                {
                    totalLength += part[i - 1].DistanceTo(part[i]);
                }
            }

            return totalLength;
        }
    }

    /// <summary>
    /// Calculates the 3D length of all parts in the polyline (if Z coordinates are available)
    /// </summary>
    /// <returns>The total 3D length, or 2D length if Z coordinates are not available</returns>
    public double Length3D
    {
        get
        {
            double totalLength = 0;

            foreach (var part in _parts)
            {
                for (int i = 1; i < part.Length; i++)
                {
                    totalLength += part[i - 1].Distance3DTo(part[i]);
                }
            }

            return totalLength;
        }
    }

    /// <summary>
    /// Gets the length of a specific part
    /// </summary>
    /// <param name="partIndex">The zero-based index of the part</param>
    /// <returns>The 2D length of the specified part</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the part index is out of range</exception>
    public double GetPartLength(int partIndex)
    {
        if (partIndex < 0 || partIndex >= _parts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(partIndex));
        }

        var part = _parts[partIndex];
        double length = 0;

        for (int i = 1; i < part.Length; i++)
        {
            length += part[i - 1].DistanceTo(part[i]);
        }

        return length;
    }

    /// <inheritdoc />
    public override Shape Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        var transformedParts = _parts.Select(part => part.Select(transform));
        return new PolyLine(transformedParts);
    }

    /// <summary>
    /// Creates a new polyline with an additional part
    /// </summary>
    /// <param name="coordinates">The coordinates for the new part</param>
    /// <returns>A new polyline containing all existing parts plus the new one</returns>
    public PolyLine AddPart(IEnumerable<Coordinate> coordinates)
    {
        if (coordinates == null)
        {
            throw new ArgumentNullException(nameof(coordinates));
        }

        var newParts = _parts.Concat([coordinates.ToArray()]);
        return new PolyLine(newParts);
    }

    /// <summary>
    /// Creates a new polyline with the specified part removed
    /// </summary>
    /// <param name="partIndex">The zero-based index of the part to remove</param>
    /// <returns>A new polyline with the specified part removed</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the part index is out of range</exception>
    public PolyLine RemovePart(int partIndex)
    {
        if (partIndex < 0 || partIndex >= _parts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(partIndex));
        }

        var newParts = _parts.Where((_, index) => index != partIndex);
        return new PolyLine(newParts);
    }

    /// <summary>
    /// Simplifies the polyline by removing redundant vertices within the specified tolerance
    /// </summary>
    /// <param name="tolerance">The distance tolerance for vertex removal</param>
    /// <returns>A simplified polyline</returns>
    public PolyLine Simplify(double tolerance)
    {
        if (tolerance <= 0)
        {
            throw new ArgumentException("Tolerance must be positive", nameof(tolerance));
        }

        var simplifiedParts = _parts
            .Select(part => SimplifyPart(part, tolerance))
            .Where(part => part.Length >= 2);

        return new PolyLine(simplifiedParts);
    }

    private static Coordinate[] SimplifyPart(Coordinate[] part, double tolerance)
    {
        if (part.Length <= 2)
        {
            return part;
        }

        var simplified = new List<Coordinate> { part[0] };

        for (int i = 1; i < part.Length - 1; i++)
        {
            var prev = simplified[^1];
            var current = part[i];
            var next = part[i + 1];

            // Use perpendicular distance to line segment
            var distance = PerpendicularDistance(current, prev, next);
            if (distance > tolerance)
            {
                simplified.Add(current);
            }
        }

        simplified.Add(part[^1]);
        return simplified.ToArray();
    }

    private static double PerpendicularDistance(
        Coordinate point,
        Coordinate lineStart,
        Coordinate lineEnd
    )
    {
        var dx = lineEnd.X - lineStart.X;
        var dy = lineEnd.Y - lineStart.Y;

        if (dx == 0 && dy == 0)
        {
            return point.DistanceTo(lineStart);
        }

        var lengthSquared = dx * dx + dy * dy;
        var t = Math.Max(
            0,
            Math.Min(
                1,
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared
            )
        );

        var projection = new Coordinate(lineStart.X + t * dx, lineStart.Y + t * dy);

        return point.DistanceTo(projection);
    }

    /// <inheritdoc />
    public override bool IsValid()
    {
        return _parts.All(part =>
            part.Length >= 2
            && part.All(c =>
                !double.IsNaN(c.X)
                && !double.IsInfinity(c.X)
                && !double.IsNaN(c.Y)
                && !double.IsInfinity(c.Y)
                && (!c.HasZ || (!double.IsNaN(c.Z!.Value) && !double.IsInfinity(c.Z!.Value)))
                && (!c.HasM || (!double.IsNaN(c.M!.Value) && !double.IsInfinity(c.M!.Value)))
            )
        );
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetValidationErrors()
    {
        for (int partIndex = 0; partIndex < _parts.Length; partIndex++)
        {
            var part = _parts[partIndex];

            if (part.Length < 2)
            {
                yield return $"Part {partIndex}: Must have at least 2 coordinates";
                continue;
            }

            for (int coordIndex = 0; coordIndex < part.Length; coordIndex++)
            {
                var coord = part[coordIndex];

                if (double.IsNaN(coord.X) || double.IsInfinity(coord.X))
                {
                    yield return $"Part {partIndex}, Coordinate {coordIndex}: X coordinate is not a valid number";
                }

                if (double.IsNaN(coord.Y) || double.IsInfinity(coord.Y))
                {
                    yield return $"Part {partIndex}, Coordinate {coordIndex}: Y coordinate is not a valid number";
                }

                if (
                    coord.HasZ
                    && (double.IsNaN(coord.Z!.Value) || double.IsInfinity(coord.Z!.Value))
                )
                {
                    yield return $"Part {partIndex}, Coordinate {coordIndex}: Z coordinate is not a valid number";
                }

                if (
                    coord.HasM
                    && (double.IsNaN(coord.M!.Value) || double.IsInfinity(coord.M!.Value))
                )
                {
                    yield return $"Part {partIndex}, Coordinate {coordIndex}: M coordinate is not a valid number";
                }
            }
        }
    }

    public override string ToString()
    {
        return $"POLYLINE ({_parts.Length} parts, {CoordinateCount} coordinates)";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PolyLine other || _parts.Length != other._parts.Length)
        {
            return false;
        }

        for (int i = 0; i < _parts.Length; i++)
        {
            var thisPart = _parts[i];
            var otherPart = other._parts[i];

            if (thisPart.Length != otherPart.Length)
            {
                return false;
            }

            for (int j = 0; j < thisPart.Length; j++)
            {
                if (!thisPart[j].Equals(otherPart[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var part in _parts)
        {
            foreach (var coordinate in part)
            {
                hash.Add(coordinate);
            }
        }
        return hash.ToHashCode();
    }
}
