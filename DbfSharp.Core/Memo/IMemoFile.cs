namespace DbfSharp.Core.Memo;

/// <summary>
/// Interface for reading memo data from memo files (FPT, DBT)
/// </summary>
public interface IMemoFile : IDisposable
{
    /// <summary>
    /// Gets the memo file path
    /// </summary>
    string? FilePath { get; }

    /// <summary>
    /// Gets a memo by its index
    /// </summary>
    /// <param name="index">The memo index</param>
    /// <returns>The memo data, or null if not found</returns>
    MemoData? GetMemo(int index);

    /// <summary>
    /// Checks if the memo file is valid and accessible
    /// </summary>
    bool IsValid { get; }
}
