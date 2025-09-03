using System.Globalization;
using System.Text;

namespace DbfSharp.Core.Projection;

/// <summary>
/// Advanced WKT (Well-Known Text) parser for coordinate system definitions
/// </summary>
public static class WktParser
{
    /// <summary>
    /// Parses WKT text and extracts comprehensive coordinate system information
    /// </summary>
    /// <param name="wkt">The WKT text to parse</param>
    /// <returns>A detailed parse result with all extracted information</returns>
    public static WktParseResult Parse(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return new WktParseResult
            {
                IsValid = false,
                ErrorMessage = "WKT text is null or empty",
            };
        }

        try
        {
            var result = new WktParseResult { IsValid = true, OriginalWkt = wkt.Trim() };

            // Tokenize the WKT for more robust parsing
            var tokens = TokenizeWkt(wkt);
            if (tokens.Count == 0)
            {
                return new WktParseResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to tokenize WKT",
                };
            }

            // Parse the root element
            var rootElement = ParseElement(tokens, 0, out _);
            if (rootElement == null)
            {
                return new WktParseResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to parse root WKT element",
                };
            }

            // Extract information based on the root type
            ExtractCoordinateSystemInfo(rootElement, result);

            return result;
        }
        catch (Exception ex)
        {
            return new WktParseResult
            {
                IsValid = false,
                ErrorMessage = $"Error parsing WKT: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Tokenizes WKT text into a list of tokens for parsing
    /// </summary>
    private static List<string> TokenizeWkt(string wkt)
    {
        var tokens = new List<string>();
        var currentToken = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < wkt.Length; i++)
        {
            char c = wkt[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentToken.Append(c);
            }
            else if (!inQuotes && c is '[' or ']' or ',')
            {
                if (currentToken.Length > 0)
                {
                    var token = currentToken.ToString().Trim();
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                    }

                    currentToken.Clear();
                }
                tokens.Add(c.ToString());
            }
            else if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (currentToken.Length > 0)
                {
                    var token = currentToken.ToString().Trim();
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                    }

                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }

        if (currentToken.Length > 0)
        {
            var token = currentToken.ToString().Trim();
            if (!string.IsNullOrEmpty(token))
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    /// <summary>
    /// Parses a WKT element from tokens
    /// </summary>
    private static WktElement? ParseElement(List<string> tokens, int startIndex, out int endIndex)
    {
        endIndex = startIndex;

        if (startIndex >= tokens.Count)
        {
            return null;
        }

        var element = new WktElement { Type = tokens[startIndex] };

        if (startIndex + 1 >= tokens.Count || tokens[startIndex + 1] != "[")
        {
            return element;
        }

        endIndex = startIndex + 2; // Skip the opening bracket

        while (endIndex < tokens.Count && tokens[endIndex] != "]")
        {
            if (tokens[endIndex] == ",")
            {
                endIndex++;
                continue;
            }

            // Check if this is a nested element
            if (endIndex + 1 < tokens.Count && tokens[endIndex + 1] == "[")
            {
                var childElement = ParseElement(tokens, endIndex, out var childEndIndex);
                if (childElement != null)
                {
                    element.Children.Add(childElement);
                }
                endIndex = childEndIndex + 1;
            }
            else
            {
                // This is a value (string or number)
                var value = tokens[endIndex];
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2); // Remove quotes
                }
                element.Values.Add(value);
                endIndex++;
            }
        }

        return element;
    }

    /// <summary>
    /// Extracts coordinate system information from the parsed WKT element
    /// </summary>
    private static void ExtractCoordinateSystemInfo(WktElement rootElement, WktParseResult result)
    {
        // Set projection type based on root element
        result.ProjectionType = rootElement.Type.ToUpperInvariant() switch
        {
            "PROJCS" => ProjectionType.Projected,
            "GEOGCS" => ProjectionType.Geographic,
            "GEOCCS" => ProjectionType.Geocentric,
            "COMPD_CS" => ProjectionType.Compound,
            "VERT_CS" => ProjectionType.Vertical,
            "LOCAL_CS" => ProjectionType.Local,
            _ => ProjectionType.Unknown,
        };

        // Extract coordinate system name (first value)
        if (rootElement.Values.Count > 0)
        {
            result.CoordinateSystemName = rootElement.Values[0];
        }

        // Extract EPSG code from AUTHORITY element
        ExtractAuthorityInfo(rootElement, result);

        // Extract detailed information based on type
        switch (result.ProjectionType)
        {
            case ProjectionType.Projected:
                ExtractProjectedInfo(rootElement, result);
                break;
            case ProjectionType.Geographic:
                ExtractGeographicInfo(rootElement, result);
                break;
            case ProjectionType.Geocentric:
                ExtractGeocentricInfo(rootElement, result);
                break;
            case ProjectionType.Compound:
                ExtractCompoundInfo(rootElement, result);
                break;
            case ProjectionType.Vertical:
                ExtractVerticalInfo(rootElement, result);
                break;
        }
    }

    /// <summary>
    /// Extracts AUTHORITY information (including EPSG codes)
    /// </summary>
    private static void ExtractAuthorityInfo(WktElement element, WktParseResult result)
    {
        var authorityElement = FindElement(element, "AUTHORITY");
        if (authorityElement is { Values.Count: >= 2 })
        {
            result.Authority = authorityElement.Values[0];
            result.AuthorityCode = authorityElement.Values[1];

            // If it's EPSG, parse the code
            if (string.Equals(result.Authority, "EPSG", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(result.AuthorityCode, out int epsgCode))
                {
                    result.EpsgCode = epsgCode;
                }
            }
        }
    }

    /// <summary>
    /// Extracts information for projected coordinate systems
    /// </summary>
    private static void ExtractProjectedInfo(WktElement rootElement, WktParseResult result)
    {
        // Find the geographic coordinate system within the projected system
        var geogcsElement = FindElement(rootElement, "GEOGCS");
        if (geogcsElement != null)
        {
            ExtractGeographicInfo(geogcsElement, result);
        }

        // Extract projection name
        var projectionElement = FindElement(rootElement, "PROJECTION");
        if (projectionElement is { Values.Count: > 0 })
        {
            result.ProjectionName = projectionElement.Values[0];
        }

        // Extract projection parameters
        var parameterElements = FindElements(rootElement, "PARAMETER");
        var parameters = new Dictionary<string, double>();

        foreach (var paramElement in parameterElements)
        {
            if (paramElement.Values.Count >= 2)
            {
                var paramName = paramElement.Values[0];
                if (
                    double.TryParse(
                        paramElement.Values[1],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double paramValue
                    )
                )
                {
                    parameters[paramName] = paramValue;
                }
            }
        }

        result.Parameters = parameters;

        // Extract linear unit
        var unitElement = FindElement(rootElement, "UNIT");
        if (unitElement is { Values.Count: >= 2 })
        {
            result.LinearUnit = unitElement.Values[0];
            if (
                double.TryParse(
                    unitElement.Values[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double unitValue
                )
            )
            {
                result.LinearUnitValue = unitValue;
            }
        }
    }

    /// <summary>
    /// Extracts information for geographic coordinate systems
    /// </summary>
    private static void ExtractGeographicInfo(WktElement element, WktParseResult result)
    {
        // Extract datum
        var datumElement = FindElement(element, "DATUM");
        if (datumElement is { Values.Count: > 0 })
        {
            result.Datum = datumElement.Values[0];

            // Extract spheroid within datum
            var spheroidElement = FindElement(datumElement, "SPHEROID");
            if (spheroidElement is { Values.Count: >= 3 })
            {
                result.Spheroid = spheroidElement.Values[0];
                if (
                    double.TryParse(
                        spheroidElement.Values[1],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double semiMajor
                    )
                )
                {
                    result.SemiMajorAxis = semiMajor;
                }
                if (
                    double.TryParse(
                        spheroidElement.Values[2],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double inverseFlattening
                    )
                )
                {
                    result.InverseFlattening = inverseFlattening;
                }
            }
        }

        // Extract prime meridian
        var primeMeridianElement = FindElement(element, "PRIMEM");
        if (primeMeridianElement is { Values.Count: >= 2 })
        {
            result.PrimeMeridian = primeMeridianElement.Values[0];
            if (
                double.TryParse(
                    primeMeridianElement.Values[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double pmValue
                )
            )
            {
                result.PrimeMeridianValue = pmValue;
            }
        }

        // Extract angular unit (for geographic systems)
        var unitElement = FindElement(element, "UNIT");
        if (unitElement is { Values.Count: >= 2 })
        {
            result.AngularUnit = unitElement.Values[0];
            if (
                double.TryParse(
                    unitElement.Values[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double unitValue
                )
            )
            {
                result.AngularUnitValue = unitValue;
            }
        }
    }

    /// <summary>
    /// Extracts information for geocentric coordinate systems
    /// </summary>
    private static void ExtractGeocentricInfo(WktElement rootElement, WktParseResult result)
    {
        // Geocentric systems are similar to geographic but use linear units
        ExtractGeographicInfo(rootElement, result);

        // Override unit extraction for linear units
        var unitElement = FindElement(rootElement, "UNIT");
        if (unitElement is { Values.Count: >= 2 })
        {
            result.LinearUnit = unitElement.Values[0];
            result.AngularUnit = null; // Geocentric doesn't use angular units
            if (
                double.TryParse(
                    unitElement.Values[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double unitValue
                )
            )
            {
                result.LinearUnitValue = unitValue;
                result.AngularUnitValue = null;
            }
        }
    }

    /// <summary>
    /// Extracts information for compound coordinate systems
    /// </summary>
    private static void ExtractCompoundInfo(WktElement rootElement, WktParseResult result)
    {
        // Compound coordinate systems contain multiple coordinate systems
        var childSystems = new List<WktParseResult>();

        foreach (var child in rootElement.Children)
        {
            if (child.Type.EndsWith("CS", StringComparison.OrdinalIgnoreCase))
            {
                var childResult = new WktParseResult { IsValid = true };
                ExtractCoordinateSystemInfo(child, childResult);
                childSystems.Add(childResult);
            }
        }

        result.ChildSystems = childSystems;

        // Take horizontal system info from the first geographic/projected system
        var horizontalSystem = childSystems.FirstOrDefault(cs =>
            cs.ProjectionType is ProjectionType.Geographic or ProjectionType.Projected
        );

        if (horizontalSystem != null)
        {
            result.Datum = horizontalSystem.Datum;
            result.Spheroid = horizontalSystem.Spheroid;
            result.PrimeMeridian = horizontalSystem.PrimeMeridian;
            result.ProjectionName = horizontalSystem.ProjectionName;
            result.Parameters = horizontalSystem.Parameters;
        }
    }

    /// <summary>
    /// Extracts information for vertical coordinate systems
    /// </summary>
    private static void ExtractVerticalInfo(WktElement rootElement, WktParseResult result)
    {
        // Extract vertical datum
        var vdatumElement = FindElement(rootElement, "VERT_DATUM");
        if (vdatumElement is { Values.Count: > 0 })
        {
            result.VerticalDatum = vdatumElement.Values[0];
        }

        // Extract vertical unit
        var unitElement = FindElement(rootElement, "UNIT");
        if (unitElement is { Values.Count: >= 2 })
        {
            result.LinearUnit = unitElement.Values[0];
            if (
                double.TryParse(
                    unitElement.Values[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double unitValue
                )
            )
            {
                result.LinearUnitValue = unitValue;
            }
        }
    }

    /// <summary>
    /// Finds the first element with the specified type
    /// </summary>
    private static WktElement? FindElement(WktElement parent, string elementType)
    {
        return parent.Children.FirstOrDefault(c =>
            string.Equals(c.Type, elementType, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Finds all elements with the specified type
    /// </summary>
    private static List<WktElement> FindElements(WktElement parent, string elementType)
    {
        return parent
            .Children.Where(c =>
                string.Equals(c.Type, elementType, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();
    }
}

/// <summary>
/// Represents a parsed WKT element
/// </summary>
public class WktElement
{
    /// <summary>
    /// The type of the WKT element (e.g., PROJCS, GEOGCS, DATUM)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The values contained in this element
    /// </summary>
    public List<string> Values { get; set; } = [];

    /// <summary>
    /// Child elements contained within this element
    /// </summary>
    public List<WktElement> Children { get; set; } = [];
}

/// <summary>
/// Enhanced result of WKT parsing with comprehensive coordinate system information
/// </summary>
public class WktParseResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the WKT was successfully parsed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if parsing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the original WKT text
    /// </summary>
    public string? OriginalWkt { get; set; }

    /// <summary>
    /// Gets or sets the coordinate system name
    /// </summary>
    public string? CoordinateSystemName { get; set; }

    /// <summary>
    /// Gets or sets the projection type
    /// </summary>
    public ProjectionType ProjectionType { get; set; }

    /// <summary>
    /// Gets or sets the projection name (for projected coordinate systems)
    /// </summary>
    public string? ProjectionName { get; set; }

    /// <summary>
    /// Gets or sets the datum information
    /// </summary>
    public string? Datum { get; set; }

    /// <summary>
    /// Gets or sets the spheroid/ellipsoid information
    /// </summary>
    public string? Spheroid { get; set; }

    /// <summary>
    /// Gets or sets the semi-major axis of the spheroid
    /// </summary>
    public double? SemiMajorAxis { get; set; }

    /// <summary>
    /// Gets or sets the inverse flattening of the spheroid
    /// </summary>
    public double? InverseFlattening { get; set; }

    /// <summary>
    /// Gets or sets the prime meridian information
    /// </summary>
    public string? PrimeMeridian { get; set; }

    /// <summary>
    /// Gets or sets the prime meridian value in degrees
    /// </summary>
    public double? PrimeMeridianValue { get; set; }

    /// <summary>
    /// Gets or sets the linear unit information
    /// </summary>
    public string? LinearUnit { get; set; }

    /// <summary>
    /// Gets or sets the linear unit conversion factor to meters
    /// </summary>
    public double? LinearUnitValue { get; set; }

    /// <summary>
    /// Gets or sets the angular unit information
    /// </summary>
    public string? AngularUnit { get; set; }

    /// <summary>
    /// Gets or sets the angular unit conversion factor to radians
    /// </summary>
    public double? AngularUnitValue { get; set; }

    /// <summary>
    /// Gets or sets the projection parameters
    /// </summary>
    public IReadOnlyDictionary<string, double> Parameters { get; set; } =
        new Dictionary<string, double>();

    /// <summary>
    /// Gets or sets the authority name (e.g., "EPSG")
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the authority code (e.g., "4326")
    /// </summary>
    public string? AuthorityCode { get; set; }

    /// <summary>
    /// Gets or sets the EPSG code if available
    /// </summary>
    public int? EpsgCode { get; set; }

    /// <summary>
    /// Gets or sets the vertical datum (for vertical coordinate systems)
    /// </summary>
    public string? VerticalDatum { get; set; }

    /// <summary>
    /// Gets or sets child coordinate systems (for compound coordinate systems)
    /// </summary>
    public List<WktParseResult> ChildSystems { get; set; } = [];
}
