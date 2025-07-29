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
    object? Parse(
        DbfField field,
        ReadOnlySpan<byte> data,
        IMemoFile? memoFile,
        System.Text.Encoding encoding,
        DbfReaderOptions options
    );
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
    public FieldParsingException(
        DbfField field,
        ReadOnlySpan<byte> rawData,
        string message,
        Exception? innerException = null
    )
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
