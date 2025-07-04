using System.Text;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Memo;

namespace DbfSharp.Core.Parsing;

/// <summary>
/// Interface for parsing DBF field data into .NET objects
/// </summary>
public interface IFieldParser
{
    /// <summary>
    /// Determines if this parser can handle the specified field type and DBF version
    /// </summary>
    /// <param name="fieldType">The field type to check</param>
    /// <param name="dbfVersion">The DBF version for context</param>
    /// <returns>True if this parser can handle the field type</returns>
    bool CanParse(FieldType fieldType, DbfVersion dbfVersion);

    /// <summary>
    /// Parses field data into a .NET object
    /// </summary>
    /// <param name="field">The field definition</param>
    /// <param name="data">The raw field data</param>
    /// <param name="memoFile">The memo file for memo field types (can be null)</param>
    /// <param name="encoding">The encoding to use for text conversion</param>
    /// <param name="options">Reader options for parsing behavior</param>
    /// <returns>The parsed field value</returns>
    object? Parse(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, System.Text.Encoding encoding, DbfReaderOptions options);
}

/// <summary>
/// Exception thrown when field parsing fails
/// </summary>
public class FieldParsingException : Exception
{
    /// <summary>
    /// Gets the field that failed to parse
    /// </summary>
    public DbfField Field { get; }

    /// <summary>
    /// Gets the raw data that failed to parse
    /// </summary>
    public byte[] RawData { get; }

    /// <summary>
    /// Initializes a new instance of FieldParsingException
    /// </summary>
    /// <param name="field">The field that failed to parse</param>
    /// <param name="rawData">The raw data that failed to parse</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception that caused the parsing failure</param>
    public FieldParsingException(DbfField field, ReadOnlySpan<byte> rawData, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Field = field;
        RawData = rawData.ToArray();
    }
}

/// <summary>
/// Represents an invalid field value that could not be parsed
/// This allows the reader to continue processing while marking invalid data
/// </summary>
public sealed class InvalidValue
{
    /// <summary>
    /// Gets the raw data that could not be parsed
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; }

    /// <summary>
    /// Gets the field that contained the invalid data
    /// </summary>
    public DbfField Field { get; }

    /// <summary>
    /// Gets the error message describing why the data was invalid
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of InvalidValue
    /// </summary>
    /// <param name="rawData">The raw data that could not be parsed</param>
    /// <param name="field">The field that contained the invalid data</param>
    /// <param name="errorMessage">The error message</param>
    public InvalidValue(ReadOnlyMemory<byte> rawData, DbfField field, string errorMessage)
    {
        RawData = rawData;
        Field = field;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Returns a string representation of the invalid value
    /// </summary>
    public override string ToString()
    {
        var dataString = Convert.ToHexString(RawData.Span);
        return $"InvalidValue(Field: {Field.Name}, Data: {dataString}, Error: {ErrorMessage})";
    }
}

/// <summary>
/// Base class for field parsers providing common functionality
/// </summary>
public abstract class FieldParserBase : IFieldParser
{
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
    public abstract object? Parse(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, System.Text.Encoding encoding, DbfReaderOptions options);

    /// <summary>
    /// Decodes text data using the specified encoding with error handling
    /// </summary>
    /// <param name="data">The raw text data</param>
    /// <param name="encoding">The encoding to use</param>
    /// <param name="options">Reader options for error handling</param>
    /// <returns>The decoded string</returns>
    protected static string DecodeText(ReadOnlySpan<byte> data, System.Text.Encoding encoding, DbfReaderOptions options)
    {
        if (data.IsEmpty)
            return string.Empty;

        try
        {
            // Apply custom decoder fallback if specified
            if (options.CharacterDecodeFallback != null)
            {
                var decoder = encoding.GetDecoder();
                decoder.Fallback = options.CharacterDecodeFallback;
                
                var maxCharCount = decoder.GetCharCount(data, flush: true);
                Span<char> chars = stackalloc char[maxCharCount];
                var charCount = decoder.GetChars(data, chars, flush: true);
                
                var result = new string(chars.Slice(0, charCount));
                return options.TrimStrings ? result.Trim() : result;
            }
            else
            {
                var result = encoding.GetString(data);
                return options.TrimStrings ? result.Trim() : result;
            }
        }
        catch (DecoderFallbackException ex)
        {
            throw new FieldParsingException(
                default, // Field info not available at this level
                data,
                $"Character decoding failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Trims null bytes and spaces from the end of data
    /// </summary>
    /// <param name="data">The data to trim</param>
    /// <returns>The trimmed data</returns>
    protected static ReadOnlySpan<byte> TrimEnd(ReadOnlySpan<byte> data)
    {
        // Trim null bytes and spaces from the end
        var end = data.Length;
        while (end > 0 && (data[end - 1] == 0 || data[end - 1] == 32))
        {
            end--;
        }
        return data.Slice(0, end);
    }

    /// <summary>
    /// Trims null bytes, spaces, and asterisks from data (asterisks are used as padding in some files)
    /// </summary>
    /// <param name="data">The data to trim</param>
    /// <returns>The trimmed data</returns>
    protected static ReadOnlySpan<byte> TrimPadding(ReadOnlySpan<byte> data)
    {
        // Trim common padding characters
        var trimmed = data;
        
        // Trim from end
        while (trimmed.Length > 0)
        {
            var lastByte = trimmed[trimmed.Length - 1];
            if (lastByte == 0 || lastByte == 32 || lastByte == 42) // null, space, asterisk
            {
                trimmed = trimmed.Slice(0, trimmed.Length - 1);
            }
            else
            {
                break;
            }
        }
        
        // Trim from start
        while (trimmed.Length > 0)
        {
            var firstByte = trimmed[0];
            if (firstByte == 0 || firstByte == 32)
            {
                trimmed = trimmed.Slice(1);
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
            return true;

        // Check if all bytes are null, space, or zero
        foreach (var b in data)
        {
            if (b != 0 && b != 32 && b != 48) // not null, space, or '0'
                return false;
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
        else
        {
            // ASCII decimal number
            var trimmed = TrimPadding(data);
            if (IsNullValue(trimmed))
                return 0;

            var text = System.Text.Encoding.ASCII.GetString(trimmed);
            if (int.TryParse(text, out var result))
                return result;
            
            throw new FormatException($"Invalid memo index format: '{text}'");
        }
    }
}