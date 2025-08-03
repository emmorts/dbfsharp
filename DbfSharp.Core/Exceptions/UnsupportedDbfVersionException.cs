namespace DbfSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when a DBF file has an unsupported or unrecognized version
/// </summary>
[Serializable]
public class UnsupportedDbfVersionException : DbfException
{
    /// <summary>
    /// Gets the raw version byte that was not recognized
    /// </summary>
    public byte VersionByte { get; }

    /// <summary>
    /// Initializes a new instance of the UnsupportedDbfVersionException class
    /// </summary>
    /// <param name="versionByte">The raw version byte that was not recognized</param>
    public UnsupportedDbfVersionException(byte versionByte)
        : base($"Unsupported or unrecognized DBF version: 0x{versionByte:X2}. This DBF format version is not supported by DbfSharp.")
    {
        VersionByte = versionByte;
    }

    /// <summary>
    /// Initializes a new instance of the UnsupportedDbfVersionException class with a specified error message
    /// </summary>
    /// <param name="versionByte">The raw version byte that was not recognized</param>
    /// <param name="message">The message that describes the error</param>
    public UnsupportedDbfVersionException(byte versionByte, string message)
        : base(message)
    {
        VersionByte = versionByte;
    }

    /// <summary>
    /// Initializes a new instance of the UnsupportedDbfVersionException class with a specified error message and inner exception
    /// </summary>
    /// <param name="versionByte">The raw version byte that was not recognized</param>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public UnsupportedDbfVersionException(byte versionByte, string message, Exception innerException)
        : base(message, innerException)
    {
        VersionByte = versionByte;
    }
}