using System.Text;
using DbfSharp.Console.Utils;
using DbfSharp.Core;

namespace DbfSharp.Console.Commands.Helpers;

/// <summary>
/// Common helper methods shared across commands
/// </summary>
public static class CommonHelpers
{
    /// <summary>
    /// Attempts to get an encoding by name, returning null if not found
    /// </summary>
    /// <param name="encodingName">The encoding name to look up</param>
    /// <returns>The encoding if found, otherwise null</returns>
    public static Encoding? TryGetEncoding(string encodingName)
    {
        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the file size from an input source result
    /// </summary>
    /// <param name="inputSource">The input source to get the size from</param>
    /// <returns>The file size in bytes, or null if not available</returns>
    public static long? GetFileSize(InputSourceResult inputSource)
    {
        try
        {
            if (inputSource.Stream is FileStream fs)
            {
                return fs.Length;
            }

            if (inputSource.Stream.CanSeek)
            {
                return inputSource.Stream.Length;
            }
        }
        catch { /* Ignore */ }
        return null;
    }

    /// <summary>
    /// Calculates the expected file size based on DBF header information
    /// </summary>
    /// <param name="reader">The DBF reader containing header information</param>
    /// <returns>The expected file size in bytes</returns>
    public static long CalculateExpectedFileSize(DbfReader reader)
    {
        var header = reader.Header;
        return header.HeaderLength + header.RecordLength * header.NumberOfRecords;
    }

    /// <summary>
    /// Formats a byte count into a human-readable file size string
    /// </summary>
    /// <param name="bytes">The number of bytes</param>
    /// <returns>A formatted file size string</returns>
    public static string FormatFileSize(long bytes)
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        const double gb = mb * 1024;

        return bytes switch
        {
            _ when bytes >= gb => $"{bytes / gb:F2} GB",
            _ when bytes >= mb => $"{bytes / mb:F2} MB",
            _ when bytes >= kb => $"{bytes / kb:F2} KB",
            _ => $"{bytes} bytes"
        };
    }

    /// <summary>
    /// Formats a value for display in sample data tables
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <returns>A formatted string representation</returns>
    public static string FormatSampleValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s when string.IsNullOrWhiteSpace(s) => "<empty>",
            string { Length: > 20 } s => s[..17] + "...",
            string s => s,
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            bool b => b ? "True" : "False",
            Core.Parsing.InvalidValue => "<invalid>",
            byte[] bytes => $"<{bytes.Length} bytes>",
            _ => value.ToString() ?? "NULL"
        };
    }
}
