using DbfSharp.Core.Projection;

namespace DbfSharp.Core.Utils;

/// <summary>
/// Utility for detecting and resolving shapefile component files
/// </summary>
public static class ShapefileDetector
{
    /// <summary>
    /// Represents the components of a shapefile dataset
    /// </summary>
    public readonly struct ShapefileComponents
    {
        /// <summary>
        /// Gets the path to the .shp file (geometry data)
        /// </summary>
        public string? ShpPath { get; }

        /// <summary>
        /// Gets the path to the .shx file (spatial index)
        /// </summary>
        public string? ShxPath { get; }

        /// <summary>
        /// Gets the path to the .dbf file (attribute data)
        /// </summary>
        public string? DbfPath { get; }

        /// <summary>
        /// Gets the path to the .prj file (projection information)
        /// </summary>
        public string? PrjPath { get; }

        /// <summary>
        /// Gets the path to the .cpg file (code page information)
        /// </summary>
        public string? CpgPath { get; }

        /// <summary>
        /// Initializes a new shapefile components structure
        /// </summary>
        /// <param name="shpPath">Path to the .shp file</param>
        /// <param name="shxPath">Path to the .shx file</param>
        /// <param name="dbfPath">Path to the .dbf file</param>
        /// <param name="prjPath">Path to the .prj file</param>
        /// <param name="cpgPath">Path to the .cpg file</param>
        public ShapefileComponents(
            string? shpPath,
            string? shxPath,
            string? dbfPath,
            string? prjPath,
            string? cpgPath
        )
        {
            ShpPath = shpPath;
            ShxPath = shxPath;
            DbfPath = dbfPath;
            PrjPath = prjPath;
            CpgPath = cpgPath;
        }

        /// <summary>
        /// Gets a value indicating whether this represents a complete shapefile (has .shp, .shx, and .dbf)
        /// </summary>
        public bool IsComplete => HasGeometry && HasAttributes;

        /// <summary>
        /// Gets a value indicating whether this has geometric data (.shp and .shx files)
        /// </summary>
        public bool HasGeometry => !string.IsNullOrEmpty(ShpPath) && !string.IsNullOrEmpty(ShxPath);

        /// <summary>
        /// Gets a value indicating whether this has attribute data (.dbf file)
        /// </summary>
        public bool HasAttributes => !string.IsNullOrEmpty(DbfPath);

        /// <summary>
        /// Gets a value indicating whether this has projection information (.prj file)
        /// </summary>
        public bool HasProjection => !string.IsNullOrEmpty(PrjPath);

        /// <summary>
        /// Gets a value indicating whether this has encoding information (.cpg file)
        /// </summary>
        public bool HasEncoding => !string.IsNullOrEmpty(CpgPath);

        /// <summary>
        /// Gets a value indicating whether this represents a DBF-only file (no geometry)
        /// </summary>
        public bool IsDbfOnly => HasAttributes && !HasGeometry;

        /// <summary>
        /// Gets a value indicating whether any component files are present
        /// </summary>
        public bool IsEmpty =>
            string.IsNullOrEmpty(ShpPath)
            && string.IsNullOrEmpty(ShxPath)
            && string.IsNullOrEmpty(DbfPath)
            && string.IsNullOrEmpty(PrjPath)
            && string.IsNullOrEmpty(CpgPath);

        /// <summary>
        /// Gets the primary file path (first non-null component)
        /// </summary>
        public string? PrimaryPath => ShpPath ?? DbfPath ?? ShxPath ?? PrjPath ?? CpgPath;

        /// <summary>
        /// Gets the base name (without extension) for all component files
        /// </summary>
        public string? BaseName =>
            PrimaryPath != null ? Path.GetFileNameWithoutExtension(PrimaryPath) : null;

        /// <summary>
        /// Gets the directory containing the component files
        /// </summary>
        public string? Directory => PrimaryPath != null ? Path.GetDirectoryName(PrimaryPath) : null;

        /// <summary>
        /// Gets a count of how many component files are present
        /// </summary>
        public int ComponentCount
        {
            get
            {
                var count = 0;
                if (!string.IsNullOrEmpty(ShpPath))
                {
                    count++;
                }

                if (!string.IsNullOrEmpty(ShxPath))
                {
                    count++;
                }

                if (!string.IsNullOrEmpty(DbfPath))
                {
                    count++;
                }

                if (!string.IsNullOrEmpty(PrjPath))
                {
                    count++;
                }

                if (!string.IsNullOrEmpty(CpgPath))
                {
                    count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets a list of all present component file paths
        /// </summary>
        /// <returns>An enumerable of existing file paths</returns>
        public IEnumerable<string> GetExistingPaths()
        {
            if (!string.IsNullOrEmpty(ShpPath))
            {
                yield return ShpPath;
            }

            if (!string.IsNullOrEmpty(ShxPath))
            {
                yield return ShxPath;
            }

            if (!string.IsNullOrEmpty(DbfPath))
            {
                yield return DbfPath;
            }

            if (!string.IsNullOrEmpty(PrjPath))
            {
                yield return PrjPath;
            }

            if (!string.IsNullOrEmpty(CpgPath))
            {
                yield return CpgPath;
            }
        }

        /// <summary>
        /// Gets a list of missing critical component file extensions
        /// </summary>
        /// <returns>An enumerable of missing file extensions</returns>
        public IEnumerable<string> GetMissingCriticalComponents()
        {
            if (string.IsNullOrEmpty(ShpPath))
            {
                yield return ".shp";
            }

            if (string.IsNullOrEmpty(ShxPath))
            {
                yield return ".shx";
            }

            if (string.IsNullOrEmpty(DbfPath))
            {
                yield return ".dbf";
            }
        }

        /// <summary>
        /// Returns a string representation of the ShapefileComponents
        /// </summary>
        /// <returns>A string that represents the current ShapefileComponents</returns>
        public override string ToString()
        {
            if (IsEmpty)
            {
                return "No shapefile components found";
            }

            var components = new List<string>();
            if (!string.IsNullOrEmpty(ShpPath))
            {
                components.Add("SHP");
            }

            if (!string.IsNullOrEmpty(ShxPath))
            {
                components.Add("SHX");
            }

            if (!string.IsNullOrEmpty(DbfPath))
            {
                components.Add("DBF");
            }

            if (!string.IsNullOrEmpty(PrjPath))
            {
                components.Add("PRJ");
            }

            if (!string.IsNullOrEmpty(CpgPath))
            {
                components.Add("CPG");
            }

            var status =
                IsComplete ? "Complete"
                : HasGeometry ? "Geometry only"
                : IsDbfOnly ? "Attributes only"
                : "Partial";

            return $"Shapefile components [{string.Join(", ", components)}] - {status}";
        }
    }

    /// <summary>
    /// Detects shapefile components based on a primary file path
    /// </summary>
    /// <param name="primaryFile">Path to any file in the shapefile dataset</param>
    /// <returns>A structure containing paths to all detected component files</returns>
    /// <exception cref="ArgumentNullException">Thrown when primaryFile is null or empty</exception>
    public static ShapefileComponents DetectComponents(string primaryFile)
    {
        if (string.IsNullOrWhiteSpace(primaryFile))
        {
            throw new ArgumentNullException(nameof(primaryFile));
        }

        // Get the base path without extension
        var basePath = Path.ChangeExtension(primaryFile, null);
        var directory = Path.GetDirectoryName(primaryFile) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(primaryFile);

        // Check for each component with case-insensitive matching
        var shpPath = FindFileWithExtension(directory, fileName, ".shp");
        var shxPath = FindFileWithExtension(directory, fileName, ".shx");
        var dbfPath = FindFileWithExtension(directory, fileName, ".dbf");
        var prjPath = FindFileWithExtension(directory, fileName, ".prj");
        var cpgPath = FindFileWithExtension(directory, fileName, ".cpg");

        return new ShapefileComponents(shpPath, shxPath, dbfPath, prjPath, cpgPath);
    }

    /// <summary>
    /// Detects shapefile components and validates file accessibility
    /// </summary>
    /// <param name="primaryFile">Path to any file in the shapefile dataset</param>
    /// <returns>A structure containing paths to all accessible component files</returns>
    /// <exception cref="ArgumentNullException">Thrown when primaryFile is null or empty</exception>
    public static ShapefileComponents DetectAndValidateComponents(string primaryFile)
    {
        var components = DetectComponents(primaryFile);

        // Validate that detected files actually exist and are accessible
        var validatedShp = ValidateFilePath(components.ShpPath);
        var validatedShx = ValidateFilePath(components.ShxPath);
        var validatedDbf = ValidateFilePath(components.DbfPath);
        var validatedPrj = ValidateFilePath(components.PrjPath);
        var validatedCpg = ValidateFilePath(components.CpgPath);

        return new ShapefileComponents(
            validatedShp,
            validatedShx,
            validatedDbf,
            validatedPrj,
            validatedCpg
        );
    }

    /// <summary>
    /// Determines the file type of a given path based on its extension
    /// </summary>
    /// <param name="filePath">The file path to analyze</param>
    /// <returns>The detected file type</returns>
    public static ShapefileFileType GetFileType(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ShapefileFileType.Unknown;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".shp" => ShapefileFileType.Shapefile,
            ".shx" => ShapefileFileType.ShapeIndex,
            ".dbf" => ShapefileFileType.Attributes,
            ".prj" => ShapefileFileType.Projection,
            ".cpg" => ShapefileFileType.CodePage,
            _ => ShapefileFileType.Unknown,
        };
    }

    /// <summary>
    /// Checks if a file path represents a shapefile component
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if the file is a recognized shapefile component</returns>
    public static bool IsShapefileComponent(string filePath)
    {
        return GetFileType(filePath) != ShapefileFileType.Unknown;
    }

    /// <summary>
    /// Gets diagnostic information about shapefile components
    /// </summary>
    /// <param name="components">The shapefile components to analyze</param>
    /// <returns>A collection of diagnostic messages</returns>
    public static IEnumerable<string> GetDiagnostics(ShapefileComponents components)
    {
        if (components.IsEmpty)
        {
            yield return "No shapefile components detected";
            yield break;
        }

        yield return $"Found {components.ComponentCount} component file(s)";

        if (components.IsComplete)
        {
            yield return "Complete shapefile dataset detected (geometry + attributes)";
        }
        else if (components is { HasGeometry: true, HasAttributes: false })
        {
            yield return "Geometry-only shapefile (missing .dbf file for attributes)";
        }
        else if (components.IsDbfOnly)
        {
            yield return "DBF file only (no associated geometry files)";
        }
        else
        {
            var missing = components.GetMissingCriticalComponents().ToList();
            if (missing.Count > 0)
            {
                yield return $"Incomplete shapefile: missing {string.Join(", ", missing)} file(s)";
            }
        }

        if (components.HasProjection)
        {
            yield return "Projection information available (.prj file)";
        }

        if (components.HasEncoding)
        {
            yield return "Encoding information available (.cpg file)";
        }

        // File size information
        var fileSizeInfos = new List<string>();
        foreach (var path in components.GetExistingPaths())
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var extension = Path.GetExtension(path).ToUpperInvariant();
                fileSizeInfos.Add($"{extension}: {FormatFileSize(fileInfo.Length)}");
            }
            catch
            {
                // Ignore file access errors in diagnostics
            }
        }

        foreach (var info in fileSizeInfos)
        {
            yield return info;
        }
    }

    /// <summary>
    /// Finds a file with the specified extension, trying different case variations
    /// </summary>
    private static string? FindFileWithExtension(
        string directory,
        string fileName,
        string extension
    )
    {
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        // Try different case variations of the extension
        var extensions = new[]
        {
            extension.ToLowerInvariant(),
            extension.ToUpperInvariant(),
            char.ToUpperInvariant(extension[0]) + extension[1..].ToLowerInvariant(),
        };

        foreach (var ext in extensions)
        {
            var fullPath = Path.Combine(directory, fileName + ext);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that a file path exists and is accessible
    /// </summary>
    private static string? ValidateFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        try
        {
            return File.Exists(filePath) ? filePath : null;
        }
        catch
        {
            return null; // File access error
        }
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Reads projection information from a .prj file if available
    /// </summary>
    /// <param name="components">The shapefile components</param>
    /// <returns>The projection information, or null if not available or invalid</returns>
    public static ProjectionFile? ReadProjectionFile(ShapefileComponents components)
    {
        if (string.IsNullOrEmpty(components.PrjPath) || !File.Exists(components.PrjPath))
        {
            return null;
        }

        try
        {
            return ProjectionFile.Read(components.PrjPath);
        }
        catch
        {
            // Return null if the file cannot be read or parsed
            return null;
        }
    }

    /// <summary>
    /// Reads code page information from a .cpg file if available
    /// </summary>
    /// <param name="components">The shapefile components</param>
    /// <returns>The code page information, or null if not available or invalid</returns>
    public static CodePageFile? ReadCodePageFile(ShapefileComponents components)
    {
        if (string.IsNullOrEmpty(components.CpgPath) || !File.Exists(components.CpgPath))
        {
            return null;
        }

        try
        {
            return CodePageFile.Read(components.CpgPath);
        }
        catch
        {
            // Return null if the file cannot be read or parsed
            return null;
        }
    }

    /// <summary>
    /// Gets comprehensive metadata about a shapefile dataset including projection and encoding
    /// </summary>
    /// <param name="components">The shapefile components</param>
    /// <returns>A metadata object with all available information</returns>
    public static ShapefileMetadata GetMetadata(ShapefileComponents components)
    {
        var projection = ReadProjectionFile(components);
        var codePage = ReadCodePageFile(components);

        return new ShapefileMetadata(components, projection, codePage);
    }
}

/// <summary>
/// Enumeration of shapefile component file types
/// </summary>
public enum ShapefileFileType
{
    /// <summary>
    /// Unknown or unrecognized file type
    /// </summary>
    Unknown,

    /// <summary>
    /// Shapefile geometry data (.shp)
    /// </summary>
    Shapefile,

    /// <summary>
    /// Shapefile spatial index (.shx)
    /// </summary>
    ShapeIndex,

    /// <summary>
    /// Attribute data (.dbf)
    /// </summary>
    Attributes,

    /// <summary>
    /// Projection information (.prj)
    /// </summary>
    Projection,

    /// <summary>
    /// Code page information (.cpg)
    /// </summary>
    CodePage,
}

/// <summary>
/// Comprehensive metadata about a shapefile dataset
/// </summary>
public class ShapefileMetadata
{
    /// <summary>
    /// Gets the shapefile components (file paths)
    /// </summary>
    public ShapefileDetector.ShapefileComponents Components { get; }

    /// <summary>
    /// Gets the projection information from the .prj file, if available
    /// </summary>
    public ProjectionFile? Projection { get; }

    /// <summary>
    /// Gets the code page information from the .cpg file, if available
    /// </summary>
    public CodePageFile? CodePage { get; }

    /// <summary>
    /// Gets a value indicating whether projection information is available and valid
    /// </summary>
    public bool HasValidProjection => Projection?.IsValid == true;

    /// <summary>
    /// Gets a value indicating whether encoding information is available and valid
    /// </summary>
    public bool HasValidEncoding => CodePage?.IsValid == true;

    /// <summary>
    /// Gets the coordinate system name if projection information is available
    /// </summary>
    public string? CoordinateSystemName => Projection?.CoordinateSystemName;

    /// <summary>
    /// Gets the encoding to use for reading DBF files
    /// </summary>
    public System.Text.Encoding Encoding => CodePage?.Encoding ?? System.Text.Encoding.UTF8;

    /// <summary>
    /// Initializes a new instance of the ShapefileMetadata class
    /// </summary>
    /// <param name="components">The shapefile components</param>
    /// <param name="projection">The projection information</param>
    /// <param name="codePage">The code page information</param>
    public ShapefileMetadata(
        ShapefileDetector.ShapefileComponents components,
        ProjectionFile? projection,
        CodePageFile? codePage
    )
    {
        Components = components;
        Projection = projection;
        CodePage = codePage;
    }

    /// <summary>
    /// Gets a summary of the shapefile metadata
    /// </summary>
    /// <returns>A summary string</returns>
    public string GetSummary()
    {
        var parts = new List<string>();

        parts.Add($"Components: {Components.ComponentCount}/5");

        if (HasValidProjection)
        {
            parts.Add($"CRS: {CoordinateSystemName}");
        }
        else if (Components.HasProjection)
        {
            parts.Add("CRS: Invalid");
        }
        else
        {
            parts.Add("CRS: Not specified");
        }

        if (HasValidEncoding)
        {
            parts.Add($"Encoding: {CodePage!.CodePageIdentifier}");
        }
        else if (Components.HasEncoding)
        {
            parts.Add("Encoding: Invalid");
        }
        else
        {
            parts.Add("Encoding: Default (UTF-8)");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Returns a string representation of this metadata
    /// </summary>
    public override string ToString()
    {
        return GetSummary();
    }
}
