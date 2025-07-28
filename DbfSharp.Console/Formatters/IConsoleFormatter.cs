using DbfSharp.Core;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Defines the contract for formatters that can display directly to console
/// </summary>
public interface IConsoleFormatter : IDbfFormatter
{
    /// <summary>
    /// Displays the formatted output directly to the console using specialized console libraries
    /// </summary>
    /// <param name="records">The records to format</param>
    /// <param name="fields">The field names to include in the output</param>
    /// <param name="reader">The DBF reader for metadata access</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    void DisplayToConsole(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        CancellationToken cancellationToken = default
    );
}
