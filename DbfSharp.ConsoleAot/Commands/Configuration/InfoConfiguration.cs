namespace DbfSharp.ConsoleAot.Commands.Configuration;

/// <summary>
/// Settings for the info command
/// </summary>
public record InfoConfiguration
{
    public string? FilePath { get; init; }
    public bool ShowFields { get; init; }
    public bool ShowHeader { get; init; }
    public bool ShowStats { get; init; }
    public bool ShowMemo { get; init; }
    public bool Verbose { get; init; }
    public bool Quiet { get; init; }
    public string? Encoding { get; init; }
    public bool IgnoreMissingMemo { get; init; }
}
