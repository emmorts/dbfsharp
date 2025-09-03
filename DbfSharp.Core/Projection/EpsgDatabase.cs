namespace DbfSharp.Core.Projection;

/// <summary>
/// Represents an EPSG coordinate system definition
/// </summary>
public record EpsgDefinition
{
    /// <summary>
    /// The EPSG code
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// The coordinate system name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The coordinate system type
    /// </summary>
    public ProjectionType Type { get; init; }

    /// <summary>
    /// The Well-Known Text definition
    /// </summary>
    public string Wkt { get; init; } = string.Empty;

    /// <summary>
    /// Area of use description
    /// </summary>
    public string? AreaOfUse { get; init; }

    /// <summary>
    /// Additional remarks or notes
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// Whether this coordinate system is deprecated
    /// </summary>
    public bool IsDeprecated { get; init; }
}

/// <summary>
/// Built-in database of common EPSG coordinate system definitions
/// </summary>
public static class EpsgDatabase
{
    private static readonly Dictionary<int, EpsgDefinition> _definitions;
    private static readonly Dictionary<string, List<int>> _nameIndex;

    static EpsgDatabase()
    {
        _definitions = CreateDefinitions();
        _nameIndex = CreateNameIndex(_definitions);
    }

    /// <summary>
    /// Gets an EPSG coordinate system definition by code
    /// </summary>
    /// <param name="epsgCode">The EPSG code</param>
    /// <returns>The coordinate system definition, or null if not found</returns>
    public static EpsgDefinition? GetByCode(int epsgCode)
    {
        return _definitions.TryGetValue(epsgCode, out var definition) ? definition : null;
    }

    /// <summary>
    /// Searches for EPSG coordinate systems by name (case-insensitive partial match)
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>List of matching coordinate system definitions</returns>
    public static List<EpsgDefinition> SearchByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return [];
        }

        var searchTerm = name.Trim().ToLowerInvariant();
        var results = new List<EpsgDefinition>();

        foreach (var kvp in _nameIndex)
        {
            if (kvp.Key.Contains(searchTerm))
            {
                foreach (var code in kvp.Value)
                {
                    if (_definitions.TryGetValue(code, out var definition))
                    {
                        results.Add(definition);
                    }
                }
            }
        }

        return results.OrderBy(d => d.Code).ToList();
    }

    /// <summary>
    /// Gets all geographic coordinate systems (latitude/longitude)
    /// </summary>
    /// <returns>List of geographic coordinate system definitions</returns>
    public static List<EpsgDefinition> GetGeographicSystems()
    {
        return _definitions
            .Values.Where(d => d is { Type: ProjectionType.Geographic, IsDeprecated: false })
            .OrderBy(d => d.Code)
            .ToList();
    }

    /// <summary>
    /// Gets all projected coordinate systems
    /// </summary>
    /// <returns>List of projected coordinate system definitions</returns>
    public static List<EpsgDefinition> GetProjectedSystems()
    {
        return _definitions
            .Values.Where(d => d is { Type: ProjectionType.Projected, IsDeprecated: false })
            .OrderBy(d => d.Code)
            .ToList();
    }

    /// <summary>
    /// Gets common coordinate systems for a specific region/country
    /// </summary>
    /// <param name="region">The region identifier (e.g., "US", "UK", "World")</param>
    /// <returns>List of coordinate systems commonly used in the region</returns>
    public static List<EpsgDefinition> GetByRegion(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            return [];
        }

        var regionUpper = region.Trim().ToUpperInvariant();

        return _definitions
            .Values.Where(d =>
                !d.IsDeprecated
                && (
                    d.AreaOfUse?.ToUpperInvariant().Contains(regionUpper) == true
                    || IsRegionSpecificCode(d.Code, regionUpper)
                )
            )
            .OrderBy(d => d.Code)
            .ToList();
    }

    /// <summary>
    /// Gets the total number of coordinate systems in the database
    /// </summary>
    public static int Count => _definitions.Count;

    /// <summary>
    /// Gets all coordinate system codes in the database
    /// </summary>
    public static IEnumerable<int> AllCodes => _definitions.Keys.OrderBy(c => c);

    /// <summary>
    /// Checks if a coordinate system code is known in the database
    /// </summary>
    /// <param name="epsgCode">The EPSG code to check</param>
    /// <returns>True if the code is in the database</returns>
    public static bool Contains(int epsgCode)
    {
        return _definitions.ContainsKey(epsgCode);
    }

    /// <summary>
    /// Creates a ProjectionFile from an EPSG code
    /// </summary>
    /// <param name="epsgCode">The EPSG code</param>
    /// <returns>A ProjectionFile instance, or null if the code is not found</returns>
    public static ProjectionFile? CreateProjectionFile(int epsgCode)
    {
        var definition = GetByCode(epsgCode);
        if (definition == null)
        {
            return null;
        }

        return new ProjectionFile(definition.Wkt);
    }

    private static Dictionary<int, EpsgDefinition> CreateDefinitions()
    {
        var definitions = new Dictionary<int, EpsgDefinition>();

        // World Geographic Coordinate Systems
        AddDefinition(
            definitions,
            4326,
            "WGS 84",
            ProjectionType.Geographic,
            """GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]]""",
            "World"
        );

        AddDefinition(
            definitions,
            4269,
            "NAD83",
            ProjectionType.Geographic,
            """GEOGCS["NAD83",DATUM["North_American_Datum_1983",SPHEROID["GRS 1980",6378137,298.257222101,AUTHORITY["EPSG","7019"]],AUTHORITY["EPSG","6269"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4269"]]""",
            "North America"
        );

        AddDefinition(
            definitions,
            4267,
            "NAD27",
            ProjectionType.Geographic,
            """GEOGCS["NAD27",DATUM["North_American_Datum_1927",SPHEROID["Clarke 1866",6378206.4,294.9786982139006,AUTHORITY["EPSG","7008"]],AUTHORITY["EPSG","6267"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4267"]]""",
            "North America"
        );

        AddDefinition(
            definitions,
            4277,
            "OSGB 1936",
            ProjectionType.Geographic,
            """GEOGCS["OSGB 1936",DATUM["OSGB_1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],AUTHORITY["EPSG","6277"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4277"]]""",
            "United Kingdom"
        );

        AddDefinition(
            definitions,
            4258,
            "ETRS89",
            ProjectionType.Geographic,
            """GEOGCS["ETRS89",DATUM["European_Terrestrial_Reference_System_1989",SPHEROID["GRS 1980",6378137,298.257222101,AUTHORITY["EPSG","7019"]],AUTHORITY["EPSG","6258"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4258"]]""",
            "Europe"
        );

        // Common Projected Coordinate Systems

        // Web Mercator (very commonly used)
        AddDefinition(
            definitions,
            3857,
            "WGS 84 / Pseudo-Mercator",
            ProjectionType.Projected,
            """PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","3857"]]""",
            "World",
            "Commonly used by web mapping applications"
        );

        // UTM Zones (selection of commonly used ones)
        AddDefinition(
            definitions,
            32633,
            "WGS 84 / UTM zone 33N",
            ProjectionType.Projected,
            """PROJCS["WGS 84 / UTM zone 33N",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",15],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",500000],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","32633"]]""",
            "Europe"
        );

        AddDefinition(
            definitions,
            32634,
            "WGS 84 / UTM zone 34N",
            ProjectionType.Projected,
            """PROJCS["WGS 84 / UTM zone 34N",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",21],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",500000],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","32634"]]""",
            "Europe"
        );

        AddDefinition(
            definitions,
            32618,
            "WGS 84 / UTM zone 18N",
            ProjectionType.Projected,
            """PROJCS["WGS 84 / UTM zone 18N",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",-75],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",500000],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","32618"]]""",
            "North America"
        );

        AddDefinition(
            definitions,
            32610,
            "WGS 84 / UTM zone 10N",
            ProjectionType.Projected,
            """PROJCS["WGS 84 / UTM zone 10N",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",-123],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",500000],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","32610"]]""",
            "North America"
        );

        // British National Grid
        AddDefinition(
            definitions,
            27700,
            "OSGB 1936 / British National Grid",
            ProjectionType.Projected,
            """PROJCS["OSGB 1936 / British National Grid",GEOGCS["OSGB 1936",DATUM["OSGB_1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],AUTHORITY["EPSG","6277"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4277"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",49],PARAMETER["central_meridian",-2],PARAMETER["scale_factor",0.9996012717],PARAMETER["false_easting",400000],PARAMETER["false_northing",-100000],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","27700"]]""",
            "United Kingdom"
        );

        // US State Plane (selection of commonly used ones)
        AddDefinition(
            definitions,
            2154,
            "RGF93 / Lambert-93",
            ProjectionType.Projected,
            """PROJCS["RGF93 / Lambert-93",GEOGCS["RGF93",DATUM["Reseau_Geodesique_Francais_1993",SPHEROID["GRS 1980",6378137,298.257222101,AUTHORITY["EPSG","7019"]],AUTHORITY["EPSG","6171"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4171"]],PROJECTION["Lambert_Conformal_Conic_2SP"],PARAMETER["standard_parallel_1",49],PARAMETER["standard_parallel_2",44],PARAMETER["latitude_of_origin",46.5],PARAMETER["central_meridian",3],PARAMETER["false_easting",700000],PARAMETER["false_northing",6600000],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","2154"]]""",
            "France"
        );

        // Add more common coordinate systems as needed...

        return definitions;
    }

    private static void AddDefinition(
        Dictionary<int, EpsgDefinition> definitions,
        int code,
        string name,
        ProjectionType type,
        string wkt,
        string? areaOfUse = null,
        string? remarks = null,
        bool isDeprecated = false
    )
    {
        definitions[code] = new EpsgDefinition
        {
            Code = code,
            Name = name,
            Type = type,
            Wkt = wkt,
            AreaOfUse = areaOfUse,
            Remarks = remarks,
            IsDeprecated = isDeprecated,
        };
    }

    private static Dictionary<string, List<int>> CreateNameIndex(
        Dictionary<int, EpsgDefinition> definitions
    )
    {
        var index = new Dictionary<string, List<int>>();

        foreach (var kvp in definitions)
        {
            var definition = kvp.Value;
            var words = definition
                .Name.ToLowerInvariant()
                .Split(
                    [' ', '/', '-', '_', '(', ')', '[', ']'],
                    StringSplitOptions.RemoveEmptyEntries
                );

            foreach (var word in words)
            {
                if (word.Length < 2)
                {
                    continue; // Skip single characters
                }

                if (!index.ContainsKey(word))
                {
                    index[word] = [];
                }

                index[word].Add(definition.Code);
            }

            // Also index the full name
            var fullName = definition.Name.ToLowerInvariant();
            if (!index.ContainsKey(fullName))
            {
                index[fullName] = [];
            }

            index[fullName].Add(definition.Code);
        }

        return index;
    }

    private static bool IsRegionSpecificCode(int code, string region)
    {
        return region switch
        {
            "US" or "USA" or "UNITED STATES" => code is >= 2001 and <= 6999, // US State Plane and related
            "UK" or "UNITED KINGDOM" or "BRITAIN" => code is 27700 or 4277,
            "FRANCE" => code == 2154 || code is >= 27581 and <= 27599,
            "GERMANY" => code is >= 31466 and <= 31469,
            _ => false,
        };
    }
}
