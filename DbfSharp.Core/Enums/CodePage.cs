using System.Text;

namespace DbfSharp.Core.Enums;

/// <summary>
/// Represents the code pages supported in DBF files based on the language driver byte
/// Mapping from the Python dbfread codepages.py
/// </summary>
public static class CodePage
{
    /// <summary>
    /// Mapping of language driver bytes to encoding information
    /// </summary>
    private static readonly Dictionary<byte, (string EncodingName, string Description)> CodePages = new()
    {
        { 0x00, ("ascii", "plain ol' ascii") },
        { 0x01, ("cp437", "U.S. MS-DOS") },
        { 0x02, ("cp850", "International MS-DOS") },
        { 0x03, ("windows-1252", "Windows ANSI") },
        { 0x04, ("macintosh", "Standard Macintosh") },
        { 0x08, ("cp865", "Danish OEM") },
        { 0x09, ("cp437", "Dutch OEM") },
        { 0x0A, ("cp850", "Dutch OEM (secondary)") },
        { 0x0B, ("cp437", "Finnish OEM") },
        { 0x0D, ("cp437", "French OEM") },
        { 0x0E, ("cp850", "French OEM (secondary)") },
        { 0x0F, ("cp437", "German OEM") },
        { 0x10, ("cp850", "German OEM (secondary)") },
        { 0x11, ("cp437", "Italian OEM") },
        { 0x12, ("cp850", "Italian OEM (secondary)") },
        { 0x13, ("shift_jis", "Japanese Shift-JIS") },
        { 0x14, ("cp850", "Spanish OEM (secondary)") },
        { 0x15, ("cp437", "Swedish OEM") },
        { 0x16, ("cp850", "Swedish OEM (secondary)") },
        { 0x17, ("cp865", "Norwegian OEM") },
        { 0x18, ("cp437", "Spanish OEM") },
        { 0x19, ("cp437", "English OEM (Britain)") },
        { 0x1A, ("cp850", "English OEM (Britain) (secondary)") },
        { 0x1B, ("cp437", "English OEM (U.S.)") },
        { 0x1C, ("cp863", "French OEM (Canada)") },
        { 0x1D, ("cp850", "French OEM (secondary)") },
        { 0x1F, ("cp852", "Czech OEM") },
        { 0x22, ("cp852", "Hungarian OEM") },
        { 0x23, ("cp852", "Polish OEM") },
        { 0x24, ("cp860", "Portuguese OEM") },
        { 0x25, ("cp850", "Portuguese OEM (secondary)") },
        { 0x26, ("cp866", "Russian OEM") },
        { 0x37, ("cp850", "English OEM (U.S.) (secondary)") },
        { 0x40, ("cp852", "Romanian OEM") },
        { 0x4D, ("gb2312", "Chinese GBK (PRC)") },
        { 0x4E, ("euc-kr", "Korean (ANSI/OEM)") },
        { 0x4F, ("big5", "Chinese Big 5 (Taiwan)") },
        { 0x50, ("windows-874", "Thai (ANSI/OEM)") },
        { 0x57, ("windows-1252", "ANSI") },
        { 0x58, ("windows-1252", "Western European ANSI") },
        { 0x59, ("windows-1252", "Spanish ANSI") },
        { 0x64, ("cp852", "Eastern European MS-DOS") },
        { 0x65, ("cp866", "Russian MS-DOS") },
        { 0x66, ("cp865", "Nordic MS-DOS") },
        { 0x67, ("cp861", "Icelandic MS-DOS") },
        { 0x6A, ("cp737", "Greek MS-DOS (437G)") },
        { 0x6B, ("cp857", "Turkish MS-DOS") },
        { 0x78, ("big5", "Traditional Chinese (Hong Kong SAR, Taiwan) Windows") },
        { 0x79, ("euc-kr", "Korean Windows") },
        { 0x7A, ("gb2312", "Chinese Simplified (PRC, Singapore) Windows") },
        { 0x7B, ("shift_jis", "Japanese Windows") },
        { 0x7C, ("windows-874", "Thai Windows") },
        { 0x7D, ("windows-1255", "Hebrew Windows") },
        { 0x7E, ("windows-1256", "Arabic Windows") },
        { 0x96, ("x-mac-cyrillic", "Russian Macintosh") },
        { 0x97, ("x-mac-ce", "Macintosh EE") },
        { 0x98, ("x-mac-greek", "Greek Macintosh") },
        { 0xC8, ("windows-1250", "Eastern European Windows") },
        { 0xC9, ("windows-1251", "Russian Windows") },
        { 0xCA, ("windows-1254", "Turkish Windows") },
        { 0xCB, ("windows-1253", "Greek Windows") }
    };

    /// <summary>
    /// Attempts to get the encoding for a given language driver byte
    /// </summary>
    /// <param name="languageDriver">The language driver byte from the DBF header</param>
    /// <returns>The corresponding Encoding, or null if not found</returns>
    public static Encoding? GetEncoding(byte languageDriver)
    {
        if (!CodePages.TryGetValue(languageDriver, out var codePage))
        {
            return null;
        }

        try
        {
            // Handle special encoding names that .NET uses different names for
            var encodingName = codePage.EncodingName switch
            {
                "cp437" => "ibm437",
                "cp850" => "ibm850",
                "cp852" => "ibm852",
                "cp857" => "ibm857",
                "cp860" => "ibm860",
                "cp861" => "ibm861",
                "cp863" => "ibm863",
                "cp865" => "ibm865",
                "cp866" => "ibm866",
                "cp737" => "ibm737",
                "macintosh" => "macintosh",
                "x-mac-cyrillic" => "x-mac-cyrillic",
                "x-mac-ce" => "x-mac-ce",
                "x-mac-greek" => "x-mac-greek",
                _ => codePage.EncodingName
            };

            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            // Encoding not supported on this platform
            return null;
        }
        catch (NotSupportedException)
        {
            // Encoding not supported on this platform
            return null;
        }
    }

    /// <summary>
    /// Gets the description for a given language driver byte
    /// </summary>
    /// <param name="languageDriver">The language driver byte from the DBF header</param>
    /// <returns>A human-readable description of the code page</returns>
    public static string GetDescription(byte languageDriver)
    {
        if (CodePages.TryGetValue(languageDriver, out var codePage))
        {
            return codePage.Description;
        }

        return $"Unknown (0x{languageDriver:X2})";
    }

    /// <summary>
    /// Attempts to guess the encoding for a language driver byte with fallback to ASCII
    /// </summary>
    /// <param name="languageDriver">The language driver byte from the DBF header</param>
    /// <returns>The corresponding Encoding, or ASCII if not found</returns>
    public static Encoding GuessEncoding(byte languageDriver)
    {
        var encoding = GetEncoding(languageDriver);
        if (encoding != null)
        {
            return encoding;
        }

        // Fallback to ASCII for unknown language drivers
        return Encoding.ASCII;
    }

    /// <summary>
    /// Gets all supported language driver bytes
    /// </summary>
    /// <returns>An enumerable of all supported language driver bytes</returns>
    public static IEnumerable<byte> GetSupportedLanguageDrivers()
    {
        return CodePages.Keys;
    }

    /// <summary>
    /// Checks if a language driver byte is supported
    /// </summary>
    /// <param name="languageDriver">The language driver byte to check</param>
    /// <returns>True if the language driver is supported</returns>
    public static bool IsSupported(byte languageDriver)
    {
        return CodePages.ContainsKey(languageDriver);
    }

    /// <summary>
    /// Gets the encoding name used internally by .NET for a given language driver
    /// </summary>
    /// <param name="languageDriver">The language driver byte</param>
    /// <returns>The .NET encoding name, or null if not supported</returns>
    public static string? GetNetEncodingName(byte languageDriver)
    {
        if (!CodePages.TryGetValue(languageDriver, out var codePage))
        {
            return null;
        }

        return codePage.EncodingName switch
        {
            "cp437" => "ibm437",
            "cp850" => "ibm850",
            "cp852" => "ibm852",
            "cp857" => "ibm857",
            "cp860" => "ibm860",
            "cp861" => "ibm861",
            "cp863" => "ibm863",
            "cp865" => "ibm865",
            "cp866" => "ibm866",
            "cp737" => "ibm737",
            "macintosh" => "macintosh",
            "x-mac-cyrillic" => "x-mac-cyrillic",
            "x-mac-ce" => "x-mac-ce",
            "x-mac-greek" => "x-mac-greek",
            _ => codePage.EncodingName
        };
    }

    /// <summary>
    /// Gets all available code page mappings
    /// </summary>
    /// <returns>A read-only dictionary of all code page mappings</returns>
    public static IReadOnlyDictionary<byte, (string EncodingName, string Description)> GetAllCodePages()
    {
        return CodePages;
    }
}