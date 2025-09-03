namespace DbfSharp.Core.Geometry;

/// <summary>
/// Shapefile geometry type codes as defined in the ESRI Shapefile specification
/// </summary>
public enum ShapeType : int
{
    /// <summary>
    /// Null shape - no geometric data
    /// </summary>
    NullShape = 0,

    /// <summary>
    /// Point - a single coordinate pair
    /// </summary>
    Point = 1,

    /// <summary>
    /// PolyLine - one or more connected line segments
    /// </summary>
    PolyLine = 3,

    /// <summary>
    /// Polygon - one or more rings forming enclosed areas
    /// </summary>
    Polygon = 5,

    /// <summary>
    /// MultiPoint - a collection of point coordinates
    /// </summary>
    MultiPoint = 8,

    /// <summary>
    /// PointZ - a point with X, Y, and Z (elevation) coordinates
    /// </summary>
    PointZ = 11,

    /// <summary>
    /// PolyLineZ - polyline with Z (elevation) coordinates
    /// </summary>
    PolyLineZ = 13,

    /// <summary>
    /// PolygonZ - polygon with Z (elevation) coordinates
    /// </summary>
    PolygonZ = 15,

    /// <summary>
    /// MultiPointZ - collection of points with Z (elevation) coordinates
    /// </summary>
    MultiPointZ = 18,

    /// <summary>
    /// PointM - point with measure (M) coordinate
    /// </summary>
    PointM = 21,

    /// <summary>
    /// PolyLineM - polyline with measure (M) coordinates
    /// </summary>
    PolyLineM = 23,

    /// <summary>
    /// PolygonM - polygon with measure (M) coordinates
    /// </summary>
    PolygonM = 25,

    /// <summary>
    /// MultiPointM - collection of points with measure (M) coordinates
    /// </summary>
    MultiPointM = 28,

    /// <summary>
    /// MultiPatch - complex 3D surface or volume geometry
    /// </summary>
    MultiPatch = 31,
}

/// <summary>
/// Extension methods for ShapeType enumeration
/// </summary>
public static class ShapeTypeExtensions
{
    /// <summary>
    /// Determines whether the shape type has Z (elevation) coordinates
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type includes Z coordinates</returns>
    public static bool HasZ(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.PointZ
            or ShapeType.PolyLineZ
            or ShapeType.PolygonZ
            or ShapeType.MultiPointZ
            or ShapeType.MultiPatch => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the shape type has M (measure) coordinates
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type includes M coordinates</returns>
    public static bool HasM(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.PointM
            or ShapeType.PolyLineM
            or ShapeType.PolygonM
            or ShapeType.MultiPointM
            or ShapeType.MultiPatch => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the shape type represents a 2D geometry
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type is 2D (no Z or M coordinates)</returns>
    public static bool Is2D(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.Point or ShapeType.PolyLine or ShapeType.Polygon or ShapeType.MultiPoint =>
                true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the shape type represents a point geometry
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type is a point variant</returns>
    public static bool IsPoint(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.Point or ShapeType.PointZ or ShapeType.PointM => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the shape type represents a multipoint geometry
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type is a multipoint variant</returns>
    public static bool IsMultiPoint(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.MultiPoint or ShapeType.MultiPointZ or ShapeType.MultiPointM => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the shape type represents a polyline geometry
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type is a polyline variant</returns>
    public static bool IsPolyLine(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.PolyLine or ShapeType.PolyLineZ or ShapeType.PolyLineM => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the shape type represents a polygon geometry
    /// </summary>
    /// <param name="shapeType">The shape type to test</param>
    /// <returns>True if the shape type is a polygon variant</returns>
    public static bool IsPolygon(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.Polygon or ShapeType.PolygonZ or ShapeType.PolygonM => true,
            _ => false,
        };
    }

    /// <summary>
    /// Gets the base 2D shape type for a given shape type
    /// </summary>
    /// <param name="shapeType">The shape type to get the base type for</param>
    /// <returns>The corresponding 2D shape type</returns>
    public static ShapeType GetBaseType(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.Point or ShapeType.PointZ or ShapeType.PointM => ShapeType.Point,
            ShapeType.PolyLine or ShapeType.PolyLineZ or ShapeType.PolyLineM => ShapeType.PolyLine,
            ShapeType.Polygon or ShapeType.PolygonZ or ShapeType.PolygonM => ShapeType.Polygon,
            ShapeType.MultiPoint or ShapeType.MultiPointZ or ShapeType.MultiPointM =>
                ShapeType.MultiPoint,
            _ => shapeType,
        };
    }

    /// <summary>
    /// Gets a human-readable description of the shape type
    /// </summary>
    /// <param name="shapeType">The shape type to describe</param>
    /// <returns>A descriptive string for the shape type</returns>
    public static string GetDescription(this ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.NullShape => "Null Shape",
            ShapeType.Point => "Point",
            ShapeType.PolyLine => "Polyline",
            ShapeType.Polygon => "Polygon",
            ShapeType.MultiPoint => "MultiPoint",
            ShapeType.PointZ => "Point (3D)",
            ShapeType.PolyLineZ => "Polyline (3D)",
            ShapeType.PolygonZ => "Polygon (3D)",
            ShapeType.MultiPointZ => "MultiPoint (3D)",
            ShapeType.PointM => "Point (Measured)",
            ShapeType.PolyLineM => "Polyline (Measured)",
            ShapeType.PolygonM => "Polygon (Measured)",
            ShapeType.MultiPointM => "MultiPoint (Measured)",
            ShapeType.MultiPatch => "MultiPatch",
            _ => $"Unknown ({(int)shapeType})",
        };
    }
}
