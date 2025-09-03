using DbfSharp.ConsoleAot.Commands.Configuration;
using DbfSharp.ConsoleAot.Input;

namespace DbfSharp.ConsoleAot.Formatters;

/// <summary>
/// Factory for creating appropriate formatters based on output format and options
/// </summary>
public static class FormatterFactory
{
    /// <summary>
    /// Creates the appropriate formatter based on the specified output format and settings
    /// </summary>
    /// <param name="format">The desired output format</param>
    /// <param name="configuration">Read command settings for extracting formatter options</param>
    /// <returns>The configured formatter instance</returns>
    public static IDbfFormatter CreateFormatter(
        OutputFormat format,
        ReadConfiguration configuration
    )
    {
        var options = CreateFormatterOptions(configuration);

        return format switch
        {
            OutputFormat.Table => new TableFormatter(options),
            OutputFormat.Csv => CsvFormatter.Csv(options),
            OutputFormat.Tsv => CsvFormatter.Tsv(options),
            OutputFormat.Json => new JsonFormatter(options),
            OutputFormat.GeoJson => new GeoJsonFormatter(options),
            _ => throw new ArgumentException(
                $"Unsupported output format: {format}",
                nameof(format)
            ),
        };
    }

    /// <summary>
    /// Converts ReadSettings to FormatterOptions for consistent configuration
    /// </summary>
    /// <param name="configuration">The command settings</param>
    /// <returns>Formatter options configured from command settings</returns>
    private static FormatterConfiguration CreateFormatterOptions(ReadConfiguration configuration)
    {
        return new FormatterConfiguration
        {
            IncludeTypeInfo = configuration.Verbose,
            MaxDisplayRecords = configuration.Limit ?? 50,
            ShowWarnings = !configuration.Quiet,
            DateFormat = null, // todo
            PrettyPrint = !configuration.Quiet,
        };
    }

    /// <summary>
    /// Creates the appropriate shapefile formatter based on the specified output format and settings
    /// </summary>
    /// <param name="format">The desired output format</param>
    /// <param name="configuration">Read command settings for extracting formatter options</param>
    /// <returns>The configured shapefile formatter instance</returns>
    public static IShapefileFormatter CreateShapefileFormatter(
        OutputFormat format,
        ReadConfiguration configuration
    )
    {
        var options = CreateFormatterOptions(configuration);

        return format switch
        {
            OutputFormat.GeoJson => new GeoJsonFormatter(options),
            _ => throw new ArgumentException(
                $"Output format {format} is not supported for shapefile data. Use GeoJson for geographic features.",
                nameof(format)
            ),
        };
    }

    /// <summary>
    /// Determines the optimal output format for the given input source
    /// </summary>
    /// <param name="input">The shapefile input source</param>
    /// <returns>The recommended output format</returns>
    public static OutputFormat DetermineOptimalFormat(ShapefileInputSource input)
    {
        if (input.IsShapefile)
        {
            // For shapefiles with geometry, default to GeoJSON
            return OutputFormat.GeoJson;
        }

        if (input.IsDbfOnly)
        {
            // For DBF-only files, default to table format
            return OutputFormat.Table;
        }

        // Fallback
        return OutputFormat.Table;
    }

    /// <summary>
    /// Checks if the specified format is suitable for shapefile data
    /// </summary>
    /// <param name="format">The output format to check</param>
    /// <returns>True if the format supports shapefile features</returns>
    public static bool SupportsShapefileFeatures(OutputFormat format)
    {
        return format == OutputFormat.GeoJson;
    }

    /// <summary>
    /// Determines if the specified format is suitable for console display
    /// </summary>
    /// <param name="format">The output format to check</param>
    /// <returns>True if the format supports rich console display</returns>
    public static bool SupportsConsoleDisplay(OutputFormat format)
    {
        return format == OutputFormat.Table;
    }

    /// <summary>
    /// Gets the recommended file extension for the specified format
    /// </summary>
    /// <param name="format">The output format</param>
    /// <returns>File extension including the dot (e.g., ".csv")</returns>
    public static string GetFileExtension(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Table => ".txt",
            OutputFormat.Csv => ".csv",
            OutputFormat.Tsv => ".tsv",
            OutputFormat.Json => ".json",
            OutputFormat.GeoJson => ".geojson",
            _ => ".txt",
        };
    }
}
