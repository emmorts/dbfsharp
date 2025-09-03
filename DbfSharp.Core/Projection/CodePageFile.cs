using System.Text;

namespace DbfSharp.Core.Projection;

/// <summary>
/// Represents a code page file (.cpg) that specifies the character encoding for DBF attribute data
/// </summary>
public class CodePageFile
{
    /// <summary>
    /// Gets the encoding specified in the code page file
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// Gets the original code page identifier from the file
    /// </summary>
    public string CodePageIdentifier { get; }

    /// <summary>
    /// Gets a value indicating whether the encoding was successfully resolved
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the file path this code page information was read from
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the CodePageFile class
    /// </summary>
    /// <param name="codePageIdentifier">The code page identifier from the file</param>
    /// <param name="encoding">The resolved encoding</param>
    /// <param name="filePath">The file path the data was read from</param>
    public CodePageFile(string codePageIdentifier, Encoding encoding, string? filePath = null)
    {
        CodePageIdentifier =
            codePageIdentifier ?? throw new ArgumentNullException(nameof(codePageIdentifier));
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        IsValid = true;
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the CodePageFile class for invalid/unresolved encodings
    /// </summary>
    /// <param name="codePageIdentifier">The code page identifier from the file</param>
    /// <param name="filePath">The file path the data was read from</param>
    public CodePageFile(string codePageIdentifier, string? filePath = null)
    {
        CodePageIdentifier =
            codePageIdentifier ?? throw new ArgumentNullException(nameof(codePageIdentifier));
        Encoding = Encoding.UTF8; // Default fallback
        IsValid = false;
        FilePath = filePath;
    }

    /// <summary>
    /// Reads and parses a code page file (.cpg)
    /// </summary>
    /// <param name="filePath">Path to the .cpg file</param>
    /// <returns>A CodePageFile instance with the parsed encoding information</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file</exception>
    public static CodePageFile Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Code page file not found: {filePath}");
        }

        try
        {
            // Read the file content and parse the encoding
            var content = File.ReadAllText(filePath, Encoding.ASCII).Trim();
            return Parse(content, filePath);
        }
        catch (Exception ex) when (ex is not (ArgumentNullException or FileNotFoundException))
        {
            throw new IOException($"Error reading code page file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads and parses a code page file (.cpg) asynchronously
    /// </summary>
    /// <param name="filePath">Path to the .cpg file</param>
    /// <returns>A task that represents the asynchronous read operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file</exception>
    public static async Task<CodePageFile> ReadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Code page file not found: {filePath}");
        }

        try
        {
            // Read the file content and parse the encoding
            var content = await File.ReadAllTextAsync(filePath, Encoding.ASCII);
            return Parse(content.Trim(), filePath);
        }
        catch (Exception ex) when (ex is not (ArgumentNullException or FileNotFoundException))
        {
            throw new IOException($"Error reading code page file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a code page identifier string and resolves it to an encoding
    /// </summary>
    /// <param name="codePageIdentifier">The code page identifier (e.g., "UTF-8", "WINDOWS-1252")</param>
    /// <param name="filePath">Optional file path for error reporting</param>
    /// <returns>A CodePageFile instance with the parsed encoding information</returns>
    public static CodePageFile Parse(string codePageIdentifier, string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(codePageIdentifier))
        {
            return new CodePageFile("", filePath);
        }

        var identifier = codePageIdentifier.Trim();

        try
        {
            // Try to resolve the encoding using various strategies
            var encoding = ResolveEncoding(identifier);
            if (encoding != null)
            {
                return new CodePageFile(identifier, encoding, filePath);
            }
        }
        catch
        {
            // Fall through to invalid case
        }

        // Return invalid instance if encoding cannot be resolved
        return new CodePageFile(identifier, filePath);
    }

    /// <summary>
    /// Attempts to resolve an encoding from a code page identifier
    /// </summary>
    /// <param name="identifier">The code page identifier</param>
    /// <returns>The resolved encoding, or null if not found</returns>
    private static Encoding? ResolveEncoding(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var normalizedId = identifier.Trim().ToUpperInvariant();

        // Handle common encoding names
        return normalizedId switch
        {
            "UTF-8" or "UTF8" => Encoding.UTF8,
            "UTF-16" or "UTF16" or "UNICODE" => Encoding.Unicode,
            "UTF-16BE" or "UTF16BE" => Encoding.BigEndianUnicode,
            "UTF-32" or "UTF32" => Encoding.UTF32,
            "ASCII" or "US-ASCII" => Encoding.ASCII,
            "WINDOWS-1252" or "CP1252" or "1252" => Encoding.GetEncoding(1252),
            "WINDOWS-1251" or "CP1251" or "1251" => Encoding.GetEncoding(1251),
            "WINDOWS-1250" or "CP1250" or "1250" => Encoding.GetEncoding(1250),
            "ISO-8859-1" or "ISO8859-1" or "LATIN1" => Encoding.GetEncoding("ISO-8859-1"),
            "ISO-8859-2" or "ISO8859-2" or "LATIN2" => Encoding.GetEncoding("ISO-8859-2"),
            "ISO-8859-15" or "ISO8859-15" or "LATIN9" => Encoding.GetEncoding("ISO-8859-15"),
            _ => TryGetEncodingByName(identifier),
        };
    }

    /// <summary>
    /// Attempts to get encoding by name using .NET's encoding provider
    /// </summary>
    /// <param name="name">The encoding name</param>
    /// <returns>The encoding if found, null otherwise</returns>
    private static Encoding? TryGetEncodingByName(string name)
    {
        try
        {
            return Encoding.GetEncoding(name);
        }
        catch
        {
            // If numeric, try as code page
            if (int.TryParse(name, out var codePage))
            {
                try
                {
                    return Encoding.GetEncoding(codePage);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Returns a string representation of this code page information
    /// </summary>
    public override string ToString()
    {
        if (!IsValid)
        {
            return $"Invalid code page: {CodePageIdentifier}";
        }

        return $"{CodePageIdentifier} ({Encoding.EncodingName})";
    }
}
