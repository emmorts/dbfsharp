using System.Text;

namespace DbfSharp.Core.Projection;

/// <summary>
/// Represents a projection file (.prj) that contains coordinate system information in Well-Known Text (WKT) format
/// </summary>
public class ProjectionFile
{
    /// <summary>
    /// Gets the original WKT (Well-Known Text) string from the projection file
    /// </summary>
    public string WktText { get; }

    /// <summary>
    /// Gets the coordinate system name extracted from the WKT
    /// </summary>
    public string? CoordinateSystemName { get; }

    /// <summary>
    /// Gets the projection type (e.g., "PROJCS", "GEOGCS")
    /// </summary>
    public ProjectionType ProjectionType { get; }

    /// <summary>
    /// Gets the datum information if available
    /// </summary>
    public string? Datum { get; }

    /// <summary>
    /// Gets the spheroid/ellipsoid information if available
    /// </summary>
    public string? Spheroid { get; }

    /// <summary>
    /// Gets the prime meridian information if available
    /// </summary>
    public string? PrimeMeridian { get; }

    /// <summary>
    /// Gets the linear unit information if available
    /// </summary>
    public string? LinearUnit { get; }

    /// <summary>
    /// Gets the angular unit information if available
    /// </summary>
    public string? AngularUnit { get; }

    /// <summary>
    /// Gets the projection parameters as key-value pairs
    /// </summary>
    public IReadOnlyDictionary<string, double> Parameters { get; }

    /// <summary>
    /// Gets the projection name (for projected coordinate systems)
    /// </summary>
    public string? ProjectionName { get; }

    /// <summary>
    /// Gets the EPSG code if available
    /// </summary>
    public int? EpsgCode { get; }

    /// <summary>
    /// Gets the authority name (e.g., "EPSG")
    /// </summary>
    public string? Authority { get; }

    /// <summary>
    /// Gets the authority code (e.g., "4326")
    /// </summary>
    public string? AuthorityCode { get; }

    /// <summary>
    /// Gets the semi-major axis of the spheroid in meters
    /// </summary>
    public double? SemiMajorAxis { get; }

    /// <summary>
    /// Gets the inverse flattening of the spheroid
    /// </summary>
    public double? InverseFlattening { get; }

    /// <summary>
    /// Gets the prime meridian value in degrees
    /// </summary>
    public double? PrimeMeridianValue { get; }

    /// <summary>
    /// Gets the linear unit conversion factor to meters
    /// </summary>
    public double? LinearUnitValue { get; }

    /// <summary>
    /// Gets the angular unit conversion factor to radians
    /// </summary>
    public double? AngularUnitValue { get; }

    /// <summary>
    /// Gets the vertical datum (for vertical coordinate systems)
    /// </summary>
    public string? VerticalDatum { get; }

    /// <summary>
    /// Gets child coordinate systems (for compound coordinate systems)
    /// </summary>
    public IReadOnlyList<ProjectionFile> ChildSystems { get; }

    /// <summary>
    /// Gets a value indicating whether the WKT was successfully parsed
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the file path this projection information was read from
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the ProjectionFile class
    /// </summary>
    /// <param name="wktText">The WKT text from the projection file</param>
    /// <param name="filePath">The file path the data was read from</param>
    public ProjectionFile(string wktText, string? filePath = null)
    {
        WktText = wktText ?? throw new ArgumentNullException(nameof(wktText));
        FilePath = filePath;

        // Parse the WKT using the advanced parser
        var parseResult = WktParser.Parse(wktText);

        CoordinateSystemName = parseResult.CoordinateSystemName;
        ProjectionType = parseResult.ProjectionType;
        ProjectionName = parseResult.ProjectionName;
        Datum = parseResult.Datum;
        Spheroid = parseResult.Spheroid;
        SemiMajorAxis = parseResult.SemiMajorAxis;
        InverseFlattening = parseResult.InverseFlattening;
        PrimeMeridian = parseResult.PrimeMeridian;
        PrimeMeridianValue = parseResult.PrimeMeridianValue;
        LinearUnit = parseResult.LinearUnit;
        LinearUnitValue = parseResult.LinearUnitValue;
        AngularUnit = parseResult.AngularUnit;
        AngularUnitValue = parseResult.AngularUnitValue;
        Parameters = parseResult.Parameters;
        Authority = parseResult.Authority;
        AuthorityCode = parseResult.AuthorityCode;
        EpsgCode = parseResult.EpsgCode;
        VerticalDatum = parseResult.VerticalDatum;
        IsValid = parseResult.IsValid;

        // Convert child systems if any
        var childSystems = new List<ProjectionFile>();
        foreach (var childResult in parseResult.ChildSystems)
        {
            var childProjection = CreateFromParseResult(childResult, null);
            childSystems.Add(childProjection);
        }
        ChildSystems = childSystems;
    }

    /// <summary>
    /// Private constructor for creating instances from parse results
    /// </summary>
    private ProjectionFile(WktParseResult parseResult, string? filePath)
    {
        WktText = parseResult.OriginalWkt ?? string.Empty;
        FilePath = filePath;
        CoordinateSystemName = parseResult.CoordinateSystemName;
        ProjectionType = parseResult.ProjectionType;
        ProjectionName = parseResult.ProjectionName;
        Datum = parseResult.Datum;
        Spheroid = parseResult.Spheroid;
        SemiMajorAxis = parseResult.SemiMajorAxis;
        InverseFlattening = parseResult.InverseFlattening;
        PrimeMeridian = parseResult.PrimeMeridian;
        PrimeMeridianValue = parseResult.PrimeMeridianValue;
        LinearUnit = parseResult.LinearUnit;
        LinearUnitValue = parseResult.LinearUnitValue;
        AngularUnit = parseResult.AngularUnit;
        AngularUnitValue = parseResult.AngularUnitValue;
        Parameters = parseResult.Parameters;
        Authority = parseResult.Authority;
        AuthorityCode = parseResult.AuthorityCode;
        EpsgCode = parseResult.EpsgCode;
        VerticalDatum = parseResult.VerticalDatum;
        IsValid = parseResult.IsValid;

        var childSystems = new List<ProjectionFile>();
        foreach (var childResult in parseResult.ChildSystems)
        {
            var childProjection = CreateFromParseResult(childResult, null);
            childSystems.Add(childProjection);
        }
        ChildSystems = childSystems;
    }

    /// <summary>
    /// Creates a ProjectionFile from a parse result
    /// </summary>
    private static ProjectionFile CreateFromParseResult(
        WktParseResult parseResult,
        string? filePath
    )
    {
        return new ProjectionFile(parseResult, filePath);
    }

    /// <summary>
    /// Reads and parses a projection file (.prj)
    /// </summary>
    /// <param name="filePath">Path to the .prj file</param>
    /// <returns>A ProjectionFile instance with the parsed coordinate system information</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file</exception>
    public static ProjectionFile Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Projection file not found: {filePath}");
        }

        try
        {
            // Read the file content - .prj files are typically single-line WKT
            var content = File.ReadAllText(filePath, Encoding.UTF8).Trim();
            return new ProjectionFile(content, filePath);
        }
        catch (Exception ex) when (ex is not (ArgumentNullException or FileNotFoundException))
        {
            throw new IOException($"Error reading projection file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads and parses a projection file (.prj) asynchronously
    /// </summary>
    /// <param name="filePath">Path to the .prj file</param>
    /// <returns>A task that represents the asynchronous read operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file</exception>
    public static async Task<ProjectionFile> ReadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Projection file not found: {filePath}");
        }

        try
        {
            // Read the file content - .prj files are typically single-line WKT
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return new ProjectionFile(content.Trim(), filePath);
        }
        catch (Exception ex) when (ex is not (ArgumentNullException or FileNotFoundException))
        {
            throw new IOException($"Error reading projection file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a human-readable summary of the coordinate system
    /// </summary>
    /// <returns>A summary string describing the coordinate system</returns>
    public string GetSummary()
    {
        if (!IsValid)
        {
            return "Invalid coordinate system";
        }

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(CoordinateSystemName))
        {
            parts.Add($"Name: {CoordinateSystemName}");
        }

        parts.Add($"Type: {ProjectionType.GetDescription()}");

        if (EpsgCode.HasValue)
        {
            parts.Add($"EPSG: {EpsgCode.Value}");
        }

        if (!string.IsNullOrEmpty(ProjectionName) && ProjectionType == ProjectionType.Projected)
        {
            parts.Add($"Projection: {ProjectionName}");
        }

        if (!string.IsNullOrEmpty(Datum))
        {
            parts.Add($"Datum: {Datum}");
        }

        if (!string.IsNullOrEmpty(LinearUnit))
        {
            parts.Add($"Unit: {LinearUnit}");
        }
        else if (!string.IsNullOrEmpty(AngularUnit))
        {
            parts.Add($"Unit: {AngularUnit}");
        }

        if (ChildSystems.Count > 0)
        {
            parts.Add($"Components: {ChildSystems.Count}");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Returns a string representation of this projection information
    /// </summary>
    public override string ToString()
    {
        return IsValid ? GetSummary() : "Invalid projection file";
    }
}

/// <summary>
/// Enumeration of coordinate system projection types
/// </summary>
public enum ProjectionType
{
    /// <summary>
    /// Unknown or unrecognized projection type
    /// </summary>
    Unknown,

    /// <summary>
    /// Geographic coordinate system (latitude/longitude)
    /// </summary>
    Geographic,

    /// <summary>
    /// Projected coordinate system (planar coordinates)
    /// </summary>
    Projected,

    /// <summary>
    /// Geocentric coordinate system (3D Cartesian)
    /// </summary>
    Geocentric,

    /// <summary>
    /// Compound coordinate system (combination of horizontal and vertical)
    /// </summary>
    Compound,

    /// <summary>
    /// Vertical coordinate system (heights/depths)
    /// </summary>
    Vertical,

    /// <summary>
    /// Local coordinate system (engineering/arbitrary)
    /// </summary>
    Local,
}

/// <summary>
/// Extension methods for ProjectionType enumeration
/// </summary>
public static class ProjectionTypeExtensions
{
    /// <summary>
    /// Gets a human-readable description of the projection type
    /// </summary>
    /// <param name="projectionType">The projection type to describe</param>
    /// <returns>A descriptive string for the projection type</returns>
    public static string GetDescription(this ProjectionType projectionType)
    {
        return projectionType switch
        {
            ProjectionType.Geographic => "Geographic (Lat/Lon)",
            ProjectionType.Projected => "Projected (Planar)",
            ProjectionType.Geocentric => "Geocentric (3D Cartesian)",
            ProjectionType.Compound => "Compound (Horizontal + Vertical)",
            ProjectionType.Vertical => "Vertical (Height/Depth)",
            ProjectionType.Local => "Local (Engineering)",
            ProjectionType.Unknown => "Unknown",
            _ => $"Unknown ({(int)projectionType})",
        };
    }
}
