namespace DbfSharp.Core;

/// <summary>
/// Defines the types of warnings that can be raised by DbfReader
/// </summary>
public enum DbfWarningType
{
    /// <summary>
    /// Duplicate field names detected in the DBF file
    /// </summary>
    DuplicateFieldNames,

    /// <summary>
    /// Field parsing issues or data validation warnings
    /// </summary>
    FieldParsingIssue,

    /// <summary>
    /// File structure anomalies
    /// </summary>
    StructuralIssue,
}
