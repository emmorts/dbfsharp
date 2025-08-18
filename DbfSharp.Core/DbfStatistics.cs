using DbfSharp.Core.Enums;

namespace DbfSharp.Core;

/// <summary>
/// Provides comprehensive metadata and statistical information about a dBASE (DBF) file,
/// including structural details, record counts, encoding information, and associated memo file data.
/// This class serves as a read-only snapshot of a DBF file's characteristics for analysis,
/// reporting, and validation purposes.
/// </summary>
/// <remarks>
/// The DBF format is a legacy database file format originally created by dBASE and later
/// adopted by various database systems including FoxPro, Clipper, and others. This statistics
/// class captures both the physical file structure (header length, record size) and logical
/// content information (active vs deleted records, field counts) to provide a complete
/// overview of the file's state and characteristics.
/// </remarks>
public sealed class DbfStatistics
{
    /// <summary>
    /// Gets or sets the name of the DBF table.
    /// </summary>
    /// <value>
    /// The table name, typically derived from the filename without extension.
    /// Defaults to an empty string if not specified.
    /// </value>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DBF file format version.
    /// </summary>
    /// <value>
    /// The version enumeration value indicating the specific DBF format variant
    /// (e.g., dBASE III, dBASE IV, FoxPro, etc.).
    /// </value>
    public DbfVersion DbfVersion { get; set; }

    /// <summary>
    /// Gets or sets the date when the DBF file was last updated.
    /// </summary>
    /// <value>
    /// The last modification date as stored in the DBF header, or null if
    /// the date is invalid or not available. Note that some DBF files may
    /// contain invalid or placeholder dates.
    /// </value>
    public DateTime? LastUpdateDate { get; set; }

    /// <summary>
    /// Gets or sets the total number of records in the DBF file.
    /// </summary>
    /// <value>
    /// The sum of active and deleted records. This represents the physical
    /// record count as stored in the DBF header.
    /// </value>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of active (non-deleted) records.
    /// </summary>
    /// <value>
    /// The count of records that are not marked for deletion and are
    /// available for normal data access operations.
    /// </value>
    public int ActiveRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records marked as deleted.
    /// </summary>
    /// <value>
    /// The count of records marked for deletion but not yet physically
    /// removed from the file. These records are typically ignored during
    /// normal data operations but may still occupy space.
    /// </value>
    public int DeletedRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of fields (columns) in each record.
    /// </summary>
    /// <value>
    /// The field count as defined in the DBF header structure.
    /// This determines the number of field descriptors in the header.
    /// </value>
    public int FieldCount { get; set; }

    /// <summary>
    /// Gets or sets the length of each data record in bytes.
    /// </summary>
    /// <value>
    /// The fixed size of each record including the deletion flag byte.
    /// This value is calculated from the field definitions and is used
    /// for file positioning during record access.
    /// </value>
    public int RecordLength { get; set; }

    /// <summary>
    /// Gets or sets the length of the DBF file header in bytes.
    /// </summary>
    /// <value>
    /// The size of the header section containing file metadata and field
    /// definitions. This marks the offset where data records begin.
    /// </value>
    public int HeaderLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the DBF file has an associated memo file.
    /// </summary>
    /// <value>
    /// <c>true</c> if the DBF file references a memo file (typically .DBT or .FPT)
    /// for storing variable-length text data; otherwise, <c>false</c>.
    /// </value>
    public bool HasMemoFile { get; set; }

    /// <summary>
    /// Gets or sets the file path to the associated memo file.
    /// </summary>
    /// <value>
    /// The full or relative path to the memo file, or null if no memo file
    /// is associated or the path is unknown. Only meaningful when
    /// <see cref="HasMemoFile"/> is <c>true</c>.
    /// </value>
    public string? MemoFilePath { get; set; }

    /// <summary>
    /// Gets or sets the character encoding used for text data in the DBF file.
    /// </summary>
    /// <value>
    /// The encoding name (e.g., "ASCII", "UTF-8", "Windows-1252") used to
    /// interpret text fields. Defaults to an empty string if not specified.
    /// The encoding may be determined from the DBF header's language driver ID.
    /// </value>
    public string Encoding { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the DBF file data is currently loaded in memory.
    /// </summary>
    /// <value>
    /// <c>true</c> if the file data is loaded for in-memory access;
    /// <c>false</c> if operating in streaming mode where records are read on-demand.
    /// This affects performance characteristics and memory usage.
    /// </value>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Returns a string representation of the DBF file statistics.
    /// </summary>
    /// <returns>
    /// A multi-line string containing formatted statistics including table name,
    /// version, record counts, field information, file sizes, encoding details,
    /// memo file status, and access mode.
    /// </returns>
    /// <inheritdoc />
    public override string ToString()
    {
        var lines = new[]
        {
            $"Table: {TableName}",
            $"Version: {DbfVersion.GetDescription()}",
            $"Last Updated: {LastUpdateDate?.ToString("yyyy-MM-dd") ?? "Unknown"}",
            $"Records: {ActiveRecords:N0} active, {DeletedRecords:N0} deleted, {TotalRecords:N0} total",
            $"Fields: {FieldCount}",
            $"Record Size: {RecordLength} bytes",
            $"Header Size: {HeaderLength} bytes",
            $"Encoding: {Encoding}",
            $"Memo File: {(HasMemoFile ? MemoFilePath ?? "Yes" : "No")}",
            $"Mode: {(IsLoaded ? "Loaded" : "Streaming")}",
        };

        return string.Join(Environment.NewLine, lines);
    }
}
