using System.Text;

namespace DbfSharp.ConsoleAot.Text;

/// <summary>
/// Resolves and validates character encodings for DBF file processing
/// </summary>
public static class EncodingResolver
{
    private static readonly Dictionary<string, string> CommonEncodingAliases = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["utf8"] = "UTF-8",
        ["utf-8"] = "UTF-8",
        ["ascii"] = "ASCII",
        ["ansi"] = "Windows-1252",
        ["cp1251"] = "Windows-1251",
        ["cp1252"] = "Windows-1252",
        ["windows1252"] = "Windows-1252",
        ["iso88591"] = "ISO-8859-1",
        ["iso-8859-1"] = "ISO-8859-1",
        ["latin1"] = "ISO-8859-1",
        ["cp437"] = "IBM437",
        ["dos"] = "IBM437",
        ["oem"] = "IBM437",
    };

    /// <summary>
    /// Resolves an encoding by name, throwing an exception if not found.
    /// </summary>
    /// <param name="encodingName">The encoding name or alias to resolve</param>
    /// <returns>The resolved encoding</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the encoding name is not recognized or supported
    /// </exception>
    public static Encoding Resolve(string encodingName)
    {
        var encoding = TryResolve(encodingName);

        return encoding
            ?? throw new ArgumentException(
                $"Encoding '{encodingName}' is not recognized or supported."
            );
    }

    /// <summary>
    /// Attempts to resolve an encoding by name, handling common aliases and variations.
    /// Since CodePagesEncodingProvider is registered, this can resolve many more encodings.
    /// </summary>
    /// <param name="encodingName">The encoding name or alias to resolve</param>
    /// <returns>The resolved encoding, or null if not found</returns>
    public static Encoding? TryResolve(string encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            return null;
        }

        var normalizedName = encodingName.Trim();

        if (CommonEncodingAliases.TryGetValue(normalizedName, out var canonicalName))
        {
            normalizedName = canonicalName;
        }

        try
        {
            return Encoding.GetEncoding(normalizedName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
