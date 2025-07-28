using DbfSharp.Core;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Defines the contract for formatting DBF records for output
/// </summary>
public interface IDbfFormatter
{
    /// <summary>
    /// Formats the specified records and writes them to the provided TextWriter
    /// </summary>
    /// <param name="records">The records to format</param>
    /// <param name="fields">The field names to include in the output</param>
    /// <param name="reader">The DBF reader for metadata access</param>
    /// <param name="writer">The TextWriter to write the formatted output to</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task WriteAsync(
        IEnumerable<DbfRecord> records, 
        string[] fields, 
        DbfReader reader, 
        TextWriter writer,
        CancellationToken cancellationToken = default);
}

