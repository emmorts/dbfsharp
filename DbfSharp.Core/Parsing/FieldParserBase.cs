using System.Buffers;
using System.Text;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Memo;

namespace DbfSharp.Core.Parsing;

/// <summary>
/// Base class for field parsers providing common functionality
/// </summary>
public abstract class FieldParserBase : IFieldParser
{
    private const int StackAllocThreshold = 1024;

    /// <summary>
    /// Determines if this parser can handle the specified field type and DBF version
    /// </summary>
    /// <param name="fieldType">The field type to check</param>
    /// <param name="dbfVersion">The DBF version for context</param>
    /// <returns>True if this parser can handle the field type</returns>
    public abstract bool CanParse(FieldType fieldType, DbfVersion dbfVersion);

    /// <summary>
    /// Parses field data into a .NET object
    /// </summary>
    /// <param name="field">The field definition</param>
    /// <param name="data">The raw field data</param>
    /// <param name="memoFile">The memo file for memo field types (can be null)</param>
    /// <param name="encoding">The encoding to use for text conversion</param>
    /// <param name="options">Reader options for parsing behavior</param>
    /// <returns>The parsed field value</returns>
    public abstract object? Parse(
        DbfField field,
        ReadOnlySpan<byte> data,
        IMemoFile? memoFile,
        Encoding encoding,
        DbfReaderOptions options
    );

    /// <summary>
    /// Decodes text data using the specified encoding with error handling
    /// </summary>
    /// <param name="data">The raw text data</param>
    /// <param name="encoding">The encoding to use</param>
    /// <param name="options">Reader options for error handling</param>
    /// <returns>The decoded string</returns>
    protected static string DecodeText(
        ReadOnlySpan<byte> data,
        Encoding encoding,
        DbfReaderOptions options
    )
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        try
        {
            string result;

            if (options.CharacterDecodeFallback != null)
            {
                var decoder = encoding.GetDecoder();
                decoder.Fallback = options.CharacterDecodeFallback;
                result = DecodeWithFallback(data, decoder);
            }
            else
            {
                result = encoding.GetString(data);
            }

            return options.TrimStrings ? result.Trim() : result;
        }
        catch (DecoderFallbackException ex)
        {
            throw new FieldParsingException(
                default,
                data,
                $"Character decoding failed: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Decodes a byte span into a string using a specific decoder,
    /// managing buffer allocation efficiently.
    /// </summary>
    private static string DecodeWithFallback(ReadOnlySpan<byte> data, Decoder decoder)
    {
        var maxCharCount = decoder.GetCharCount(data, flush: true);
        if (maxCharCount == 0)
        {
            return string.Empty;
        }

        char[]? rentedBuffer = null;
        try
        {
            var chars =
                maxCharCount <= StackAllocThreshold
                    ? stackalloc char[maxCharCount]
                    : rentedBuffer = ArrayPool<char>.Shared.Rent(maxCharCount);

            var charCount = decoder.GetChars(data, chars, flush: true);
            return new string(chars[..charCount]);
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Trims null bytes and spaces from the end of data
    /// </summary>
    /// <param name="data">The data to trim</param>
    /// <returns>The trimmed data</returns>
    protected static ReadOnlySpan<byte> TrimEnd(ReadOnlySpan<byte> data)
    {
        var end = data.Length;
        while (end > 0 && (data[end - 1] == 0 || data[end - 1] == 32))
        {
            end--;
        }
        return data[..end];
    }

    /// <summary>
    /// Trims null bytes, spaces, and asterisks from data (asterisks are used as padding in some files)
    /// </summary>
    /// <param name="data">The data to trim</param>
    /// <returns>The trimmed data</returns>
    protected static ReadOnlySpan<byte> TrimPadding(ReadOnlySpan<byte> data)
    {
        var trimmed = data;

        while (trimmed.Length > 0)
        {
            var lastByte = trimmed[^1];
            if (lastByte is 0 or 32 or 42) // null, space, asterisk
            {
                trimmed = trimmed[..^1];
            }
            else
            {
                break;
            }
        }

        while (trimmed.Length > 0)
        {
            var firstByte = trimmed[0];
            if (firstByte is 0 or 32)
            {
                trimmed = trimmed[1..];
            }
            else
            {
                break;
            }
        }

        return trimmed;
    }

    /// <summary>
    /// Checks if data represents a null/empty value
    /// </summary>
    /// <param name="data">The data to check</param>
    /// <returns>True if the data represents a null value</returns>
    protected static bool IsNullValue(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return true;
        }

        foreach (var b in data)
        {
            if (b != 0 && b != 32 && b != 48) // not null, space, or '0'
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parses an integer from memo index data
    /// </summary>
    /// <param name="data">The memo index data</param>
    /// <returns>The parsed memo index</returns>
    protected static int ParseMemoIndex(ReadOnlySpan<byte> data)
    {
        if (data.Length == 4)
        {
            // 4-byte little-endian integer
            return BitConverter.ToInt32(data);
        }

        // ASCII decimal number
        var trimmed = TrimPadding(data);
        if (IsNullValue(trimmed))
        {
            return 0;
        }

        var text = Encoding.ASCII.GetString(trimmed);

        return int.TryParse(text, out var result)
            ? result
            : throw new FormatException($"Invalid memo index format: '{text}'");
    }
}
