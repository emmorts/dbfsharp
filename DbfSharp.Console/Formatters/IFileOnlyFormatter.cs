using DbfSharp.Core;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Defines the contract for formatters that can only write to files (not stdout)
/// </summary>
public interface IFileOnlyFormatter : IDbfFormatter
{
    /// <summary>
    /// Formats the specified records and writes them directly to a file
    /// </summary>
    /// <param name="records">The records to format</param>
    /// <param name="fields">The field names to include in the output</param>
    /// <param name="reader">The DBF reader for metadata access</param>
    /// <param name="filePath">The file path to write to</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task WriteToFileAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        string filePath,
        CancellationToken cancellationToken = default
    );
}
