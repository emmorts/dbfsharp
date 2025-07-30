using DbfSharp.Console.Commands;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Factory for creating appropriate formatters based on output format and options
/// </summary>
public static class FormatterFactory
{
    /// <summary>
    /// Creates the appropriate formatter based on the specified output format and settings
    /// </summary>
    /// <param name="format">The desired output format</param>
    /// <param name="settings">Read command settings for extracting formatter options</param>
    /// <returns>The configured formatter instance</returns>
    public static IDbfFormatter CreateFormatter(OutputFormat format, ReadSettings settings)
    {
        var options = CreateFormatterOptions(settings);

        return format switch
        {
            OutputFormat.Table => new TableFormatter(options),
            OutputFormat.Csv => CsvFormatter.Csv(options),
            OutputFormat.Tsv => CsvFormatter.Tsv(options),
            OutputFormat.Json => new JsonFormatter(options),
            _ => throw new ArgumentException(
                $"Unsupported output format: {format}",
                nameof(format)
            ),
        };
    }

    /// <summary>
    /// Creates a console formatter for interactive display (returns null if format doesn't support console display)
    /// </summary>
    /// <param name="format">The desired output format</param>
    /// <param name="settings">Read command settings for extracting formatter options</param>
    /// <returns>Console formatter or null if not supported</returns>
    public static IConsoleFormatter? CreateConsoleFormatter(
        OutputFormat format,
        ReadSettings settings
    )
    {
        var formatter = CreateFormatter(format, settings);
        return formatter as IConsoleFormatter;
    }

    /// <summary>
    /// Converts ReadSettings to FormatterOptions for consistent configuration
    /// </summary>
    /// <param name="settings">The command settings</param>
    /// <returns>Formatter options configured from command settings</returns>
    private static FormatterOptions CreateFormatterOptions(ReadSettings settings)
    {
        return new FormatterOptions
        {
            IncludeTypeInfo = settings.Verbose,
            MaxDisplayRecords = settings.Limit ?? 50,
            ShowWarnings = !settings.Quiet,
            DateFormat = null, // todo
            PrettyPrint = !settings.Quiet,
        };
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
            _ => ".txt",
        };
    }
}
