namespace DbfSharp.Console.Commands.Settings;

/// <summary>
/// Settings for the read command
/// </summary>
public record ReadSettings
{
    public string? FilePath { get; init; }
    public OutputFormat Format { get; init; }
    public string? OutputPath { get; init; }
    public int? Limit { get; init; }
    public int Skip { get; init; }
    public bool ShowDeleted { get; init; }
    public string? Fields { get; init; }
    public bool Verbose { get; init; }
    public bool Quiet { get; init; }
    public string? Encoding { get; init; }
    public bool IgnoreCase { get; init; }
    public bool TrimStrings { get; init; }
    public bool IgnoreMissingMemo { get; init; }
}
