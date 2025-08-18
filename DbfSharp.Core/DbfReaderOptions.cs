using System.Text;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Core;

/// <summary>
/// Configuration options for DbfReader
/// </summary>
public record DbfReaderOptions
{
    /// <summary>
    /// Gets or sets the character encoding to use for text fields.
    /// If null, the encoding will be auto-detected from the language driver byte in the header.
    /// </summary>
    public Encoding? Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Gets or sets whether field name lookups should be case-insensitive.
    /// Default is true for compatibility with most DBF usage patterns.
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets whether field names should be converted to lowercase.
    /// Default is false to preserve original casing.
    /// </summary>
    public bool LowerCaseFieldNames { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to ignore missing memo files instead of throwing an exception.
    /// Default is false - missing memo files will cause an exception.
    /// </summary>
    public bool IgnoreMissingMemoFile { get; set; }

    /// <summary>
    /// Gets or sets how to handle character decoding errors.
    /// Default uses strict error handling which throws exceptions on invalid characters.
    /// </summary>
    public DecoderFallback? CharacterDecodeFallback { get; set; }

    /// <summary>
    /// Gets or sets a custom field parser for handling special field types or custom parsing logic.
    /// If null, the default field parser will be used.
    /// </summary>
    public IFieldParser? CustomFieldParser { get; set; }

    /// <summary>
    /// Gets or sets whether to enable string interning for field names to reduce memory usage.
    /// Default is true. Disable for applications that process many different DBF files.
    /// </summary>
    public bool EnableStringInterning { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to trim whitespace from character fields.
    /// Default is true to match typical DBF behavior.
    /// </summary>
    public bool TrimStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to return raw field data without parsing for debugging purposes.
    /// When true, all field values will be returned as byte arrays.
    /// Default is false.
    /// </summary>
    public bool RawMode { get; set; }

    /// <summary>
    /// Gets or sets the buffer size used for reading files.
    /// Default is 16KB for better memory efficiency. Larger values may improve performance for large files.
    /// </summary>
    public int BufferSize { get; set; } = 16384;

    /// <summary>
    /// Gets or sets whether to validate field definitions when reading the header.
    /// Default is true. Disable for maximum compatibility with malformed files.
    /// </summary>
    public bool ValidateFields { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to skip deleted records during enumeration.
    /// Default is true. When false, deleted records will be included in the enumeration.
    /// </summary>
    public bool SkipDeletedRecords { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of records to read.
    /// If null (default), all records will be read.
    /// Useful for testing or sampling large files.
    /// </summary>
    public int? MaxRecords { get; set; }

    /// <summary>
    /// Gets or sets whether to use memory-mapped files for better performance with large files.
    /// Default is false for compatibility. Enable for better performance with large files on 64-bit systems.
    /// </summary>
    public bool UseMemoryMapping { get; set; }

    /// <summary>
    /// Creates default options optimized for performance
    /// </summary>
    /// <returns>DbfReaderOptions configured for best performance</returns>
    public static DbfReaderOptions CreatePerformanceOptimized()
    {
        return new DbfReaderOptions
        {
            EnableStringInterning = true, // reduce memory for repeated field names
            BufferSize = 128 * 1024,
            UseMemoryMapping = Environment.Is64BitProcess,
            ValidateFields = false,
            TrimStrings = false,
        };
    }

    /// <summary>
    /// Creates default options optimized for memory usage
    /// </summary>
    /// <returns>DbfReaderOptions configured for minimal memory usage</returns>
    public static DbfReaderOptions CreateMemoryOptimized()
    {
        return new DbfReaderOptions
        {
            EnableStringInterning = true,
            BufferSize = 4 * 1024,
            UseMemoryMapping = false,
            RawMode = false,
            TrimStrings = true,
        };
    }

    /// <summary>
    /// Creates options optimized for minimal memory allocation during enumeration
    /// </summary>
    /// <returns>DbfReaderOptions configured for zero-allocation reading</returns>
    public static DbfReaderOptions CreateZeroAllocationOptimized()
    {
        return new DbfReaderOptions
        {
            EnableStringInterning = false,
            BufferSize = 1024,
            UseMemoryMapping = false,
            RawMode = false,
            TrimStrings = false,
            ValidateFields = false,
            SkipDeletedRecords = true,
        };
    }

    /// <summary>
    /// Creates default options optimized for compatibility
    /// </summary>
    /// <returns>DbfReaderOptions configured for maximum compatibility</returns>
    public static DbfReaderOptions CreateCompatibilityOptimized()
    {
        return new DbfReaderOptions
        {
            IgnoreCase = true,
            IgnoreMissingMemoFile = true,
            ValidateFields = false,
            CharacterDecodeFallback = DecoderFallback.ReplacementFallback,
            TrimStrings = true,
            SkipDeletedRecords = true,
        };
    }

    /// <summary>
    /// Returns a string representation of the options
    /// </summary>
    public override string ToString()
    {
        var options = new List<string>();

        if (Encoding != null)
        {
            options.Add($"Encoding={Encoding.EncodingName}");
        }

        if (!IgnoreCase)
        {
            options.Add("CaseSensitive");
        }

        if (LowerCaseFieldNames)
        {
            options.Add("LowerCaseNames");
        }

        if (IgnoreMissingMemoFile)
        {
            options.Add("IgnoreMissingMemo");
        }

        if (RawMode)
        {
            options.Add("RawMode");
        }

        if (!TrimStrings)
        {
            options.Add("NoTrim");
        }

        if (!ValidateFields)
        {
            options.Add("NoValidation");
        }

        if (!SkipDeletedRecords)
        {
            options.Add("IncludeDeleted");
        }

        if (MaxRecords.HasValue)
        {
            options.Add($"MaxRecords={MaxRecords}");
        }

        if (UseMemoryMapping)
        {
            options.Add("MemoryMapped");
        }

        if (BufferSize != 16384)
        {
            options.Add($"Buffer={BufferSize}");
        }

        return options.Count > 0 ? string.Join(", ", options) : "Default";
    }
}
