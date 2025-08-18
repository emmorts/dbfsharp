namespace DbfSharp.Core.Exceptions;

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
}
