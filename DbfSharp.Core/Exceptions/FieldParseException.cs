using System.Runtime.Serialization;

namespace DbfSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when there are errors parsing field data
/// </summary>
[Serializable]
public class FieldParseException : DbfException
{
    /// <summary>
    /// Gets the name of the field that failed to parse
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// Gets the type of the field that failed to parse
    /// </summary>
    public string? FieldType { get; }

    /// <summary>
    /// Gets the raw data that failed to parse
    /// </summary>
    public byte[]? RawData { get; }

    /// <summary>
    /// Initializes a new instance of the FieldParseException class
    /// </summary>
    public FieldParseException() { }

    /// <summary>
    /// Initializes a new instance of the FieldParseException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public FieldParseException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the FieldParseException class with field information
    /// </summary>
    /// <param name="fieldName">The name of the field that failed to parse</param>
    /// <param name="fieldType">The type of the field that failed to parse</param>
    /// <param name="rawData">The raw data that failed to parse</param>
    /// <param name="message">The message that describes the error</param>
    public FieldParseException(string fieldName, string fieldType, byte[] rawData, string message)
        : base(message)
    {
        FieldName = fieldName;
        FieldType = fieldType;
        RawData = rawData;
    }

    /// <summary>
    /// Initializes a new instance of the FieldParseException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public FieldParseException(string message, Exception innerException)
        : base(message, innerException) { }
}
