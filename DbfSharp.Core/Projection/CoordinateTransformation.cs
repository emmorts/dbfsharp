using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Projection;

/// <summary>
/// Represents a coordinate transformation between two coordinate systems
/// </summary>
public class CoordinateTransformation
{
    /// <summary>
    /// Gets the source coordinate system
    /// </summary>
    public ProjectionFile SourceCoordinateSystem { get; }

    /// <summary>
    /// Gets the target coordinate system
    /// </summary>
    public ProjectionFile TargetCoordinateSystem { get; }

    /// <summary>
    /// Gets the transformation type
    /// </summary>
    public TransformationType TransformationType { get; }

    /// <summary>
    /// Gets a value indicating whether this transformation is valid
    /// </summary>
    public bool IsValid => SourceCoordinateSystem.IsValid && TargetCoordinateSystem.IsValid;

    /// <summary>
    /// Gets the inverse transformation (target to source)
    /// </summary>
    public CoordinateTransformation Inverse => new(TargetCoordinateSystem, SourceCoordinateSystem);

    /// <summary>
    /// Initializes a new coordinate transformation
    /// </summary>
    /// <param name="source">The source coordinate system</param>
    /// <param name="target">The target coordinate system</param>
    public CoordinateTransformation(ProjectionFile source, ProjectionFile target)
    {
        SourceCoordinateSystem = source ?? throw new ArgumentNullException(nameof(source));
        TargetCoordinateSystem = target ?? throw new ArgumentNullException(nameof(target));
        TransformationType = DetermineTransformationType(source, target);
    }

    /// <summary>
    /// Transforms a single coordinate from source to target coordinate system
    /// </summary>
    /// <param name="coordinate">The coordinate to transform</param>
    /// <returns>The transformed coordinate</returns>
    public Coordinate Transform(Coordinate coordinate)
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot transform with invalid coordinate systems");
        }

        return TransformationType switch
        {
            TransformationType.Identity => coordinate,
            TransformationType.GeographicToProjected => TransformGeographicToProjected(coordinate),
            TransformationType.ProjectedToGeographic => TransformProjectedToGeographic(coordinate),
            TransformationType.ProjectedToProjected => TransformProjectedToProjected(coordinate),
            TransformationType.GeographicToGeographic => TransformGeographicToGeographic(
                coordinate
            ),
            _ => throw new NotSupportedException(
                $"Transformation type {TransformationType} is not supported"
            ),
        };
    }

    /// <summary>
    /// Transforms multiple coordinates efficiently
    /// </summary>
    /// <param name="coordinates">The coordinates to transform</param>
    /// <returns>The transformed coordinates</returns>
    public IEnumerable<Coordinate> Transform(IEnumerable<Coordinate> coordinates)
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot transform with invalid coordinate systems");
        }

        return coordinates.Select(Transform);
    }

    /// <summary>
    /// Creates a transformation function that can be used with Shape.Transform()
    /// </summary>
    /// <returns>A function that transforms coordinates from source to target system</returns>
    public Func<Coordinate, Coordinate> CreateTransformFunction()
    {
        return Transform;
    }

    private TransformationType DetermineTransformationType(
        ProjectionFile source,
        ProjectionFile target
    )
    {
        // Check for identity transformation
        if (
            source.EpsgCode.HasValue
            && target.EpsgCode.HasValue
            && source.EpsgCode == target.EpsgCode
        )
        {
            return TransformationType.Identity;
        }

        // Determine transformation type based on coordinate system types
        var sourceType = source.ProjectionType;
        var targetType = target.ProjectionType;

        return (sourceType, targetType) switch
        {
            (ProjectionType.Geographic, ProjectionType.Projected) =>
                TransformationType.GeographicToProjected,
            (ProjectionType.Projected, ProjectionType.Geographic) =>
                TransformationType.ProjectedToGeographic,
            (ProjectionType.Projected, ProjectionType.Projected) =>
                TransformationType.ProjectedToProjected,
            (ProjectionType.Geographic, ProjectionType.Geographic) =>
                TransformationType.GeographicToGeographic,
            _ => TransformationType.Unsupported,
        };
    }

    private Coordinate TransformGeographicToProjected(Coordinate geographic)
    {
        // Convert degrees to radians for geographic coordinates
        var lonRad = geographic.X * Math.PI / 180.0;
        var latRad = geographic.Y * Math.PI / 180.0;

        // Apply projection transformation based on target projection
        var projectionName = TargetCoordinateSystem.ProjectionName?.ToLowerInvariant();

        return projectionName switch
        {
            "transverse_mercator" => TransformToTransverseMercator(
                lonRad,
                latRad,
                geographic.Z,
                geographic.M
            ),
            "mercator" or "mercator_1sp" => TransformToMercator(
                lonRad,
                latRad,
                geographic.Z,
                geographic.M
            ),
            "web_mercator" => TransformToWebMercator(lonRad, latRad, geographic.Z, geographic.M),
            _ => throw new NotSupportedException(
                $"Projection '{TargetCoordinateSystem.ProjectionName}' is not supported"
            ),
        };
    }

    private Coordinate TransformProjectedToGeographic(Coordinate projected)
    {
        // Apply inverse projection transformation based on source projection
        var projectionName = SourceCoordinateSystem.ProjectionName?.ToLowerInvariant();

        var geographic = projectionName switch
        {
            "transverse_mercator" => InverseTransverseMercator(
                projected.X,
                projected.Y,
                projected.Z,
                projected.M
            ),
            "mercator" or "mercator_1sp" => InverseMercator(
                projected.X,
                projected.Y,
                projected.Z,
                projected.M
            ),
            "web_mercator" => InverseWebMercator(
                projected.X,
                projected.Y,
                projected.Z,
                projected.M
            ),
            _ => throw new NotSupportedException(
                $"Projection '{SourceCoordinateSystem.ProjectionName}' is not supported"
            ),
        };

        // Convert radians to degrees for geographic coordinates
        return new Coordinate(
            geographic.X * 180.0 / Math.PI,
            geographic.Y * 180.0 / Math.PI,
            geographic.Z,
            geographic.M
        );
    }

    private Coordinate TransformProjectedToProjected(Coordinate projected)
    {
        // For projected to projected, go through geographic coordinates
        // This is a simplified approach - more sophisticated implementations would use direct transformations
        var geographic = TransformProjectedToGeographic(projected);
        var tempTransform = new CoordinateTransformation(
            new ProjectionFile(CreateWgs84GeographicWkt()),
            TargetCoordinateSystem
        );
        return tempTransform.TransformGeographicToProjected(geographic);
    }

    private Coordinate TransformGeographicToGeographic(Coordinate geographic)
    {
        // For now, assume simple datum shifts or return as-is
        // In a full implementation, this would handle datum transformations
        return geographic;
    }

    private Coordinate TransformToMercator(double lonRad, double latRad, double? z, double? m)
    {
        // Standard Mercator projection
        var semiMajorAxis = TargetCoordinateSystem.SemiMajorAxis ?? 6378137.0; // WGS84 default
        var centralMeridian = GetProjectionParameter("central_meridian", 0.0) * Math.PI / 180.0;
        var falseEasting = GetProjectionParameter("false_easting", 0.0);
        var falseNorthing = GetProjectionParameter("false_northing", 0.0);

        var x = semiMajorAxis * (lonRad - centralMeridian) + falseEasting;
        var y = semiMajorAxis * Math.Log(Math.Tan(Math.PI / 4.0 + latRad / 2.0)) + falseNorthing;

        return new Coordinate(x, y, z, m);
    }

    private Coordinate TransformToWebMercator(double lonRad, double latRad, double? z, double? m)
    {
        // Web Mercator (EPSG:3857) - simplified spherical Mercator
        const double R = 6378137.0; // Earth radius in meters

        var x = R * lonRad;
        var y = R * Math.Log(Math.Tan(Math.PI / 4.0 + latRad / 2.0));

        return new Coordinate(x, y, z, m);
    }

    private Coordinate TransformToTransverseMercator(
        double lonRad,
        double latRad,
        double? z,
        double? m
    )
    {
        // Simplified Transverse Mercator (UTM-style) projection
        var semiMajorAxis = TargetCoordinateSystem.SemiMajorAxis ?? 6378137.0;
        var inverseFlattening = TargetCoordinateSystem.InverseFlattening ?? 298.257223563;
        var centralMeridian = GetProjectionParameter("central_meridian", 0.0) * Math.PI / 180.0;
        var latitudeOfOrigin = GetProjectionParameter("latitude_of_origin", 0.0) * Math.PI / 180.0;
        var scaleFactor = GetProjectionParameter("scale_factor", 1.0);
        var falseEasting = GetProjectionParameter("false_easting", 0.0);
        var falseNorthing = GetProjectionParameter("false_northing", 0.0);

        // Simplified UTM calculation (this is a basic implementation)
        var eccentricity = Math.Sqrt(
            2.0 / inverseFlattening - 1.0 / (inverseFlattening * inverseFlattening)
        );
        var n =
            semiMajorAxis
            / Math.Sqrt(1.0 - eccentricity * eccentricity * Math.Sin(latRad) * Math.Sin(latRad));
        var t = Math.Tan(latRad);
        var c =
            eccentricity
            * eccentricity
            * Math.Cos(latRad)
            * Math.Cos(latRad)
            / (1.0 - eccentricity * eccentricity);
        var a = Math.Cos(latRad) * (lonRad - centralMeridian);

        var x = scaleFactor * n * (a + (1.0 - t * t + c) * a * a * a / 6.0) + falseEasting;
        var y = scaleFactor * (latRad - latitudeOfOrigin + n * t * (a * a / 2.0)) + falseNorthing;

        return new Coordinate(x, y, z, m);
    }

    private Coordinate InverseMercator(double x, double y, double? z, double? m)
    {
        var semiMajorAxis = SourceCoordinateSystem.SemiMajorAxis ?? 6378137.0;
        var centralMeridian =
            GetSourceProjectionParameter("central_meridian", 0.0) * Math.PI / 180.0;
        var falseEasting = GetSourceProjectionParameter("false_easting", 0.0);
        var falseNorthing = GetSourceProjectionParameter("false_northing", 0.0);

        var lonRad = ((x - falseEasting) / semiMajorAxis) + centralMeridian;
        var latRad = 2.0 * Math.Atan(Math.Exp((y - falseNorthing) / semiMajorAxis)) - Math.PI / 2.0;

        return new Coordinate(lonRad, latRad, z, m);
    }

    private Coordinate InverseWebMercator(double x, double y, double? z, double? m)
    {
        const double R = 6378137.0;

        var lonRad = x / R;
        var latRad = 2.0 * Math.Atan(Math.Exp(y / R)) - Math.PI / 2.0;

        return new Coordinate(lonRad, latRad, z, m);
    }

    private Coordinate InverseTransverseMercator(double x, double y, double? z, double? m)
    {
        // Simplified inverse Transverse Mercator
        // This is a basic implementation - production code would use more sophisticated algorithms
        var semiMajorAxis = SourceCoordinateSystem.SemiMajorAxis ?? 6378137.0;
        var centralMeridian =
            GetSourceProjectionParameter("central_meridian", 0.0) * Math.PI / 180.0;
        var scaleFactor = GetSourceProjectionParameter("scale_factor", 1.0);
        var falseEasting = GetSourceProjectionParameter("false_easting", 0.0);
        var falseNorthing = GetSourceProjectionParameter("false_northing", 0.0);

        // Simplified inverse calculation
        var xNorm = (x - falseEasting) / scaleFactor;
        var yNorm = (y - falseNorthing) / scaleFactor;

        var lonRad = centralMeridian + xNorm / semiMajorAxis;
        var latRad = yNorm / semiMajorAxis;

        return new Coordinate(lonRad, latRad, z, m);
    }

    private double GetProjectionParameter(string parameterName, double defaultValue)
    {
        return TargetCoordinateSystem.Parameters.TryGetValue(parameterName, out var value)
            ? value
            : defaultValue;
    }

    private double GetSourceProjectionParameter(string parameterName, double defaultValue)
    {
        return SourceCoordinateSystem.Parameters.TryGetValue(parameterName, out var value)
            ? value
            : defaultValue;
    }

    private static string CreateWgs84GeographicWkt()
    {
        return """GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.01745329251994328]]""";
    }
}

/// <summary>
/// Enumeration of coordinate transformation types
/// </summary>
public enum TransformationType
{
    /// <summary>
    /// Identity transformation (source and target are the same)
    /// </summary>
    Identity,

    /// <summary>
    /// Geographic to projected coordinate transformation
    /// </summary>
    GeographicToProjected,

    /// <summary>
    /// Projected to geographic coordinate transformation
    /// </summary>
    ProjectedToGeographic,

    /// <summary>
    /// Projected to projected coordinate transformation (via geographic)
    /// </summary>
    ProjectedToProjected,

    /// <summary>
    /// Geographic to geographic coordinate transformation (datum shift)
    /// </summary>
    GeographicToGeographic,

    /// <summary>
    /// Unsupported transformation type
    /// </summary>
    Unsupported,
}
