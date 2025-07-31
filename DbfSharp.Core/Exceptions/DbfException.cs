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
}
