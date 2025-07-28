using System.Runtime.Serialization;

namespace DbfSharp.Core.Exceptions;

/// <summary>
/// Base exception class for all DBF-related errors
/// </summary>
[Serializable]
public class DbfException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DbfException class
    /// </summary>
    public DbfException() { }

    /// <summary>
    /// Initializes a new instance of the DbfException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public DbfException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the DbfException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DbfException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the DbfException class with serialized data
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    protected DbfException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}

/// <summary>
/// Exception thrown when a DBF file is not found
/// </summary>
[Serializable]
public class DbfNotFoundException : DbfException
{
    /// <summary>
    /// Gets the path of the file that was not found
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the DbfNotFoundException class
    /// </summary>
    public DbfNotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the DbfNotFoundException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public DbfNotFoundException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the DbfNotFoundException class with a specified file path
    /// </summary>
    /// <param name="filePath">The path of the file that was not found</param>
    /// <param name="message">The message that describes the error</param>
    public DbfNotFoundException(string filePath, string message)
        : base(message)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the DbfNotFoundException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DbfNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the DbfNotFoundException class with serialized data
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    protected DbfNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        FilePath = info.GetString(nameof(FilePath));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(FilePath), FilePath);
    }
}

/// <summary>
/// Exception thrown when a required memo file is missing
/// </summary>
[Serializable]
public class MissingMemoFileException : DbfException
{
    /// <summary>
    /// Gets the path of the memo file that was expected
    /// </summary>
    public string? MemoFilePath { get; }

    /// <summary>
    /// Gets the path of the DBF file that requires the memo file
    /// </summary>
    public string? DbfFilePath { get; }

    /// <summary>
    /// Initializes a new instance of the MissingMemoFileException class
    /// </summary>
    public MissingMemoFileException() { }

    /// <summary>
    /// Initializes a new instance of the MissingMemoFileException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public MissingMemoFileException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the MissingMemoFileException class with file paths
    /// </summary>
    /// <param name="dbfFilePath">The path of the DBF file</param>
    /// <param name="memoFilePath">The path of the expected memo file</param>
    /// <param name="message">The message that describes the error</param>
    public MissingMemoFileException(string dbfFilePath, string memoFilePath, string message)
        : base(message)
    {
        DbfFilePath = dbfFilePath;
        MemoFilePath = memoFilePath;
    }

    /// <summary>
    /// Initializes a new instance of the MissingMemoFileException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public MissingMemoFileException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the MissingMemoFileException class with serialized data
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    protected MissingMemoFileException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        DbfFilePath = info.GetString(nameof(DbfFilePath));
        MemoFilePath = info.GetString(nameof(MemoFilePath));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(DbfFilePath), DbfFilePath);
        info.AddValue(nameof(MemoFilePath), MemoFilePath);
    }
}

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

    /// <summary>
    /// Initializes a new instance of the FieldParseException class with serialized data
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    protected FieldParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        FieldName = info.GetString(nameof(FieldName));
        FieldType = info.GetString(nameof(FieldType));
        RawData = (byte[]?)info.GetValue(nameof(RawData), typeof(byte[]));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(FieldName), FieldName);
        info.AddValue(nameof(FieldType), FieldType);
        info.AddValue(nameof(RawData), RawData);
    }
}
