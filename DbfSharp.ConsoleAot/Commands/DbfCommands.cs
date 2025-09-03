using ConsoleAppFramework;
using DbfSharp.ConsoleAot.Formatters;

namespace DbfSharp.ConsoleAot.Commands;

/// <summary>
/// Contains all commands for the DbfSharp CLI application with standardized error handling.
/// </summary>
public class DbfCommands
{
    /// <summary>
    /// Reads and displays DBF file contents.
    /// </summary>
    /// <param name="filePath">Path to the DBF file to read or omit to read from stdin.</param>
    /// <param name="format">-f, The output format (table, csv, tsv, json).</param>
    /// <param name="output">-o, The output file path (default: stdout).</param>
    /// <param name="limit">-l, Maximum number of records to display.</param>
    /// <param name="skip">-s, Number of records to skip before reading.</param>
    /// <param name="showDeleted">Includes records marked as deleted in the output.</param>
    /// <param name="fields">A comma-separated list of fields to include (e.g., "NAME,AGE").</param>
    /// <param name="verbose">-v, Enables verbose output, including file information.</param>
    /// <param name="quiet">-q, Suppresses all informational output except for data and errors.</param>
    /// <param name="encoding">Overrides the character encoding for reading text fields (e.g., "UTF-8", "Windows-1252").</param>
    /// <param name="ignoreCase">Treats field names as case-insensitive.</param>
    /// <param name="trimStrings">Trims leading and trailing whitespace from string fields.</param>
    /// <param name="ignoreMissingMemo">Prevents failure if a required memo file (.dbt) is missing.</param>
    /// <param name="boundingBox">Filter by bounding box "minX,minY,maxX,maxY" (shapefile only).</param>
    /// <param name="containsPoint">Filter geometries containing point "x,y" (shapefile only).</param>
    /// <param name="intersectsWith">Filter geometries intersecting with bounding box "minX,minY,maxX,maxY" (shapefile only).</param>
    /// <param name="nearestPoint">Find features nearest to point "x,y" (shapefile only).</param>
    /// <param name="nearestCount">Number of nearest features to return (default: 1).</param>
    /// <param name="nearestDistance">Maximum distance for nearest neighbor search.</param>
    /// <param name="buildSpatialIndex">Build spatial index for faster queries (recommended for large files).</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    [Command("read")]
    public Task<int> Read(
        [Argument] string? filePath = null,
        OutputFormat format = OutputFormat.Table,
        string? output = null,
        int? limit = null,
        int skip = 0,
        bool showDeleted = false,
        string? fields = null,
        bool verbose = false,
        bool quiet = false,
        string? encoding = null,
        bool ignoreCase = true,
        bool trimStrings = true,
        bool ignoreMissingMemo = true,
        string? boundingBox = null,
        string? containsPoint = null,
        string? intersectsWith = null,
        string? nearestPoint = null,
        int? nearestCount = null,
        double? nearestDistance = null,
        bool buildSpatialIndex = false,
        CancellationToken cancellationToken = default
    )
    {
        return ReadCommand.ExecuteAsync(
            filePath,
            format,
            output,
            limit,
            skip,
            showDeleted,
            fields,
            verbose,
            quiet,
            encoding,
            ignoreCase,
            trimStrings,
            ignoreMissingMemo,
            boundingBox,
            containsPoint,
            intersectsWith,
            nearestPoint,
            nearestCount,
            nearestDistance,
            buildSpatialIndex,
            cancellationToken
        );
    }

    /// <summary>
    /// Displays detailed DBF file metadata and structure information.
    /// </summary>
    /// <param name="filePath">Path to the DBF file to analyze, S3 URI (s3://bucket/key), or omit to read from stdin.</param>
    /// <param name="fields">Show field definitions table.</param>
    /// <param name="header">Show header information table.</param>
    /// <param name="stats">Show record statistics table.</param>
    /// <param name="memo">Show memo file information.</param>
    /// <param name="verbose">-v, Show additional detailed information, including sample data.</param>
    /// <param name="quiet">-q, Suppress all informational output except for errors.</param>
    /// <param name="encoding">Override character encoding (e.g., "UTF-8", "Windows-1252").</param>
    /// <param name="ignoreMissingMemo">Don't fail if the memo file is missing.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    [Command("info")]
    public Task<int> Info(
        [Argument] string? filePath = null,
        bool fields = true,
        bool header = true,
        bool stats = true,
        bool memo = true,
        bool verbose = false,
        bool quiet = false,
        string? encoding = null,
        bool ignoreMissingMemo = true,
        CancellationToken cancellationToken = default
    )
    {
        return InfoCommand.ExecuteAsync(
            filePath,
            fields,
            header,
            stats,
            memo,
            verbose,
            quiet,
            encoding,
            ignoreMissingMemo,
            cancellationToken
        );
    }
}
