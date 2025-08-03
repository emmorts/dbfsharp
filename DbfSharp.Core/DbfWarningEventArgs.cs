namespace DbfSharp.Core;

/// <summary>
/// Provides data for DbfReader warning events
/// </summary>
public class DbfWarningEventArgs : EventArgs
{
    /// <summary>
    /// Gets the warning message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the type of warning
    /// </summary>
    public DbfWarningType WarningType { get; }

    /// <summary>
    /// Gets additional context information about the warning
    /// </summary>
    public string? Context { get; }

    /// <summary>
    /// Initializes a new instance of the DbfWarningEventArgs class
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="warningType">The type of warning</param>
    /// <param name="context">Optional context information</param>
    public DbfWarningEventArgs(string message, DbfWarningType warningType, string? context = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        WarningType = warningType;
        Context = context;
    }
}
