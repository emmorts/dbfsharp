using DbfSharp.ConsoleAot.Formatters;
using DbfSharp.Core.Spatial;

namespace DbfSharp.ConsoleAot.Commands.Configuration;

/// <summary>
/// Settings for the read command
/// </summary>
public record ReadConfiguration
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
    
    // Spatial query options
    public string? BoundingBox { get; init; }
    public string? ContainsPoint { get; init; }
    public string? IntersectsWith { get; init; }
    public SpatialRelationship? SpatialRelation { get; init; }
    public double? NearestDistance { get; init; }
    public string? NearestPoint { get; init; }
    public int? NearestCount { get; init; }
    public bool BuildSpatialIndex { get; init; }
}
