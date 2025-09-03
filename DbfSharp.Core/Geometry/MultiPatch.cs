namespace DbfSharp.Core.Geometry;

/// <summary>
/// Represents a collection of surface patches forming complex 3D geometries.
/// Multipatches can represent 3D surfaces, volumes, and complex geometric objects.
/// </summary>
public class MultiPatch : Shape
{
    private readonly PatchPart[] _parts;
    private readonly BoundingBox _boundingBox;

    /// <summary>
    /// Initializes a new instance of the MultiPatch class
    /// </summary>
    /// <param name="parts">The collection of patch parts that make up this MultiPatch</param>
    /// <exception cref="ArgumentNullException">Thrown if parts is null</exception>
    public MultiPatch(IEnumerable<PatchPart> parts)
    {
        if (parts == null)
        {
            throw new ArgumentNullException(nameof(parts));
        }

        _parts = parts.ToArray();
        _boundingBox = CalculateBoundingBox();
    }

    /// <summary>
    /// Initializes a new instance of the MultiPatch class
    /// </summary>
    /// <param name="parts">The array of patch parts that make up this MultiPatch</param>
    public MultiPatch(params PatchPart[] parts)
        : this((IEnumerable<PatchPart>)parts) { }

    /// <inheritdoc />
    public override ShapeType ShapeType => ShapeType.MultiPatch;

    /// <inheritdoc />
    public override BoundingBox BoundingBox => _boundingBox;

    /// <inheritdoc />
    public override bool IsEmpty => _parts.Length == 0;

    /// <summary>
    /// Gets the number of parts in this MultiPatch
    /// </summary>
    public int PartCount => _parts.Length;

    /// <summary>
    /// Gets all patch parts in this MultiPatch
    /// </summary>
    /// <returns>An enumerable of all patch parts</returns>
    public IEnumerable<PatchPart> GetParts() => _parts.AsEnumerable();

    /// <summary>
    /// Gets the patch part at the specified index
    /// </summary>
    /// <param name="partIndex">The zero-based index of the part</param>
    /// <returns>The patch part at the specified index</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the part index is out of range</exception>
    public PatchPart GetPart(int partIndex)
    {
        if (partIndex < 0 || partIndex >= _parts.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(partIndex),
                $"Part index {partIndex} is out of range [0, {_parts.Length})"
            );
        }

        return _parts[partIndex];
    }

    /// <inheritdoc />
    public override IEnumerable<Coordinate> GetCoordinates()
    {
        return _parts.SelectMany(part => part.Coordinates);
    }

    /// <summary>
    /// Gets the total number of coordinates across all parts
    /// </summary>
    public override int CoordinateCount => _parts.Sum(part => part.Coordinates.Count);

    /// <summary>
    /// Creates a copy of this MultiPatch with the specified coordinate transformation applied
    /// </summary>
    /// <param name="transform">The transformation function to apply to each coordinate</param>
    /// <returns>A new MultiPatch with transformed coordinates</returns>
    public override Shape Transform(Func<Coordinate, Coordinate> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        var transformedParts = _parts.Select(part =>
        {
            var transformedCoords = part.Coordinates.Select(transform).ToList();
            return new PatchPart(part.PatchType, transformedCoords);
        });

        return new MultiPatch(transformedParts);
    }

    /// <summary>
    /// Calculates the bounding box for all coordinates in this MultiPatch
    /// </summary>
    private BoundingBox CalculateBoundingBox()
    {
        if (_parts.Length == 0)
        {
            return new BoundingBox(0, 0, 0, 0);
        }

        var allCoords = GetCoordinates().ToList();
        if (allCoords.Count == 0)
        {
            return new BoundingBox(0, 0, 0, 0);
        }

        var xMin = allCoords.Min(c => c.X);
        var yMin = allCoords.Min(c => c.Y);
        var xMax = allCoords.Max(c => c.X);
        var yMax = allCoords.Max(c => c.Y);

        double? zMin = null,
            zMax = null,
            mMin = null,
            mMax = null;

        var zValues = allCoords.Where(c => c.HasZ).Select(c => c.Z!.Value).ToList();
        if (zValues.Count > 0)
        {
            zMin = zValues.Min();
            zMax = zValues.Max();
        }

        var mValues = allCoords.Where(c => c.HasM).Select(c => c.M!.Value).ToList();
        if (mValues.Count > 0)
        {
            mMin = mValues.Min();
            mMax = mValues.Max();
        }

        return new BoundingBox(xMin, yMin, xMax, yMax, zMin, zMax, mMin, mMax);
    }

    /// <summary>
    /// Returns a string representation of this MultiPatch
    /// </summary>
    public override string ToString()
    {
        return $"MultiPatch with {PartCount} parts ({CoordinateCount} coordinates)";
    }
}

/// <summary>
/// Represents a single patch part within a MultiPatch geometry
/// </summary>
public class PatchPart
{
    /// <summary>
    /// Initializes a new instance of the PatchPart class
    /// </summary>
    /// <param name="patchType">The type of this patch part</param>
    /// <param name="coordinates">The coordinates that define this patch part</param>
    /// <exception cref="ArgumentNullException">Thrown if coordinates is null</exception>
    public PatchPart(PatchType patchType, IReadOnlyList<Coordinate> coordinates)
    {
        PatchType = patchType;
        Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
    }

    /// <summary>
    /// Gets the patch type for this part
    /// </summary>
    public PatchType PatchType { get; }

    /// <summary>
    /// Gets the coordinates that define this patch part
    /// </summary>
    public IReadOnlyList<Coordinate> Coordinates { get; }

    /// <summary>
    /// Gets the number of coordinates in this patch part
    /// </summary>
    public int CoordinateCount => Coordinates.Count;

    /// <summary>
    /// Returns a string representation of this patch part
    /// </summary>
    public override string ToString()
    {
        return $"{PatchType} patch with {CoordinateCount} coordinates";
    }
}

/// <summary>
/// Defines the types of patches that can be used in MultiPatch geometries
/// </summary>
public enum PatchType : int
{
    /// <summary>
    /// A triangle strip - a set of connected triangles where each triangle shares an edge with the next
    /// </summary>
    TriangleStrip = 0,

    /// <summary>
    /// A triangle fan - a set of triangles that all share a common vertex
    /// </summary>
    TriangleFan = 1,

    /// <summary>
    /// The outer ring of a polygon surface
    /// </summary>
    OuterRing = 2,

    /// <summary>
    /// An inner ring (hole) within a polygon surface
    /// </summary>
    InnerRing = 3,

    /// <summary>
    /// The first ring of a polygon (equivalent to OuterRing)
    /// </summary>
    FirstRing = 4,

    /// <summary>
    /// A ring that follows the winding order of the first ring
    /// </summary>
    Ring = 5,
}

/// <summary>
/// Extension methods for PatchType enumeration
/// </summary>
public static class PatchTypeExtensions
{
    /// <summary>
    /// Determines whether the patch type represents a ring (polygon boundary)
    /// </summary>
    /// <param name="patchType">The patch type to test</param>
    /// <returns>True if the patch type is a ring variant</returns>
    public static bool IsRing(this PatchType patchType)
    {
        return patchType switch
        {
            PatchType.OuterRing or PatchType.InnerRing or PatchType.FirstRing or PatchType.Ring =>
                true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the patch type represents a triangle-based surface
    /// </summary>
    /// <param name="patchType">The patch type to test</param>
    /// <returns>True if the patch type is triangle-based</returns>
    public static bool IsTriangle(this PatchType patchType)
    {
        return patchType switch
        {
            PatchType.TriangleStrip or PatchType.TriangleFan => true,
            _ => false,
        };
    }

    /// <summary>
    /// Gets a human-readable description of the patch type
    /// </summary>
    /// <param name="patchType">The patch type to describe</param>
    /// <returns>A descriptive string for the patch type</returns>
    public static string GetDescription(this PatchType patchType)
    {
        return patchType switch
        {
            PatchType.TriangleStrip => "Triangle Strip",
            PatchType.TriangleFan => "Triangle Fan",
            PatchType.OuterRing => "Outer Ring",
            PatchType.InnerRing => "Inner Ring",
            PatchType.FirstRing => "First Ring",
            PatchType.Ring => "Ring",
            _ => $"Unknown ({(int)patchType})",
        };
    }
}
