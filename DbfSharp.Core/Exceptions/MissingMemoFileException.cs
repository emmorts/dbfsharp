using System.Runtime.Serialization;

namespace DbfSharp.Core.Exceptions;

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
}
