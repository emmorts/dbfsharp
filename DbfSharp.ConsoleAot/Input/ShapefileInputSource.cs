using DbfSharp.Core;
using DbfSharp.Core.Utils;

namespace DbfSharp.ConsoleAot.Input;

/// <summary>
/// Represents an input source for shapefile datasets, combining geometry and attribute data
/// </summary>
public sealed class ShapefileInputSource : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the input source for the .shp file (geometry data)
    /// </summary>
    public InputSource? ShpSource { get; }

    /// <summary>
    /// Gets the input source for the .shx file (spatial index)
    /// </summary>
    public InputSource? ShxSource { get; }

    /// <summary>
    /// Gets the input source for the .dbf file (attribute data)
    /// </summary>
    public InputSource? DbfSource { get; }

    /// <summary>
    /// Gets the shapefile components that were detected
    /// </summary>
    public ShapefileDetector.ShapefileComponents Components { get; }

    /// <summary>
    /// Gets a value indicating whether this represents a complete shapefile with geometry data
    /// </summary>
    public bool IsShapefile => Components.HasGeometry;

    /// <summary>
    /// Gets a value indicating whether this represents a DBF-only source (no geometry)
    /// </summary>
    public bool IsDbfOnly => Components.IsDbfOnly;

    /// <summary>
    /// Gets a value indicating whether this source has attribute data
    /// </summary>
    public bool HasAttributes => Components.HasAttributes;

    /// <summary>
    /// Gets a value indicating whether this source has a spatial index
    /// </summary>
    public bool HasIndex => Components.HasGeometry && ShxSource != null;

    /// <summary>
    /// Gets a value indicating whether this represents a complete dataset (geometry + attributes)
    /// </summary>
    public bool IsComplete => Components.IsComplete;

    /// <summary>
    /// Gets the primary source name for display purposes
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Initializes a new shapefile input source
    /// </summary>
    /// <param name="components">The detected shapefile components</param>
    /// <param name="shpSource">The .shp file source</param>
    /// <param name="shxSource">The .shx file source</param>
    /// <param name="dbfSource">The .dbf file source</param>
    /// <param name="displayName">The display name for this source</param>
    public ShapefileInputSource(
        ShapefileDetector.ShapefileComponents components,
        InputSource? shpSource,
        InputSource? shxSource,
        InputSource? dbfSource,
        string displayName
    )
    {
        Components = components;
        ShpSource = shpSource;
        ShxSource = shxSource;
        DbfSource = dbfSource;
        DisplayName = displayName;
    }

    /// <summary>
    /// Creates a shapefile reader from this input source
    /// </summary>
    /// <returns>A new ShapefileReader instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when no geometry data is available</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this source has been disposed</exception>
    public ShapefileReader CreateShapefileReader()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileInputSource));

        if (!IsShapefile)
        {
            throw new InvalidOperationException(
                "Cannot create shapefile reader: no geometry data available"
            );
        }

        // Create DBF reader if available
        DbfReader? dbfReader = null;
        if (HasAttributes && DbfSource != null)
        {
            try
            {
                dbfReader = DbfReader.Create(DbfSource.Stream);
            }
            catch
            {
                // Continue without attributes if DBF reading fails
            }
        }

        return ShapefileReader.Create(
            ShpSource!.Stream,
            ShxSource?.Stream,
            dbfReader,
            ownsStreams: false, // InputSource manages stream disposal
            DisplayName
        );
    }

    /// <summary>
    /// Creates a DBF reader from this input source
    /// </summary>
    /// <returns>A new DbfReader instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when no attribute data is available</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this source has been disposed</exception>
    public DbfReader CreateDbfReader()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileInputSource));

        if (!HasAttributes || DbfSource == null)
        {
            throw new InvalidOperationException(
                "Cannot create DBF reader: no attribute data available"
            );
        }

        return DbfReader.Create(DbfSource.Stream);
    }

    /// <summary>
    /// Gets diagnostic information about this shapefile input source
    /// </summary>
    /// <returns>A collection of diagnostic messages</returns>
    public IEnumerable<string> GetDiagnostics()
    {
        foreach (var diagnostic in ShapefileDetector.GetDiagnostics(Components))
        {
            yield return diagnostic;
        }

        // Add source-specific information
        if (ShpSource != null)
        {
            var size = ShpSource.GetFileSize();
            if (size.HasValue)
            {
                yield return $"Geometry file size: {FormatFileSize(size.Value)}";
            }
        }

        if (DbfSource != null)
        {
            var size = DbfSource.GetFileSize();
            if (size.HasValue)
            {
                yield return $"Attribute file size: {FormatFileSize(size.Value)}";
            }
        }
    }

    /// <summary>
    /// Gets a summary of missing components
    /// </summary>
    /// <returns>A collection of missing component descriptions</returns>
    public IEnumerable<string> GetMissingComponents()
    {
        var missing = Components.GetMissingCriticalComponents().ToList();
        if (missing.Count > 0)
        {
            foreach (var component in missing)
            {
                var description = component switch
                {
                    ".shp" => "Geometry data",
                    ".shx" => "Spatial index (performance may be reduced)",
                    ".dbf" => "Attribute data",
                    _ => component,
                };
                yield return $"Missing {component}: {description}";
            }
        }
    }

    /// <summary>
    /// Gets warnings about this shapefile input source
    /// </summary>
    /// <returns>A collection of warning messages</returns>
    public IEnumerable<string> GetWarnings()
    {
        if (IsShapefile && !HasIndex)
        {
            yield return "No spatial index (.shx) found - random access will not be available";
        }

        if (IsShapefile && !HasAttributes)
        {
            yield return "No attribute data (.dbf) found - features will have geometry only";
        }

        if (IsDbfOnly)
        {
            yield return "DBF file only - no geometric visualization possible";
        }

        if (!Components.HasProjection)
        {
            yield return "No projection file (.prj) found - coordinate system is unknown";
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ShpSource?.Dispose();
        ShxSource?.Dispose();
        DbfSource?.Dispose();

        _disposed = true;
    }

    public override string ToString()
    {
        var type =
            IsShapefile ? "Shapefile"
            : IsDbfOnly ? "DBF"
            : "Unknown";
        var completeness = IsComplete ? "Complete" : "Partial";
        return $"{type} ({completeness}): {DisplayName}";
    }
}
