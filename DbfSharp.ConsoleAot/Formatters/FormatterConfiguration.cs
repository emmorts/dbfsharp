namespace DbfSharp.ConsoleAot.Formatters;

/// <summary>
/// Base formatter context containing common formatting options and utilities
/// </summary>
public record FormatterConfiguration
{
    /// <summary>
    /// Whether to include field type information in headers where applicable
    /// </summary>
    public bool IncludeTypeInfo { get; init; } = false;

    /// <summary>
    /// Maximum number of records to display (for console formatters to prevent overflow)
    /// </summary>
    public int? MaxDisplayRecords { get; init; }

    /// <summary>
    /// Whether to show warning messages for formatting issues
    /// </summary>
    public bool ShowWarnings { get; init; } = true;

    /// <summary>
    /// Custom date format string (defaults to ISO format if not specified)
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Whether to pretty-print the output where applicable
    /// </summary>
    public bool PrettyPrint { get; init; } = true;
}
