namespace DbfSharp.Core.Memo;

/// <summary>
/// A null memo file implementation that returns null for all memo lookups
/// Used when memo files are missing but IgnoreMissingMemoFile is true
/// </summary>
public sealed class NullMemoFile : IMemoFile
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static readonly NullMemoFile Instance = new();

    private NullMemoFile() { }

    /// <summary>
    /// Gets the memo file path (always null)
    /// </summary>
    public string? FilePath => null;

    /// <summary>
    /// Always returns null
    /// </summary>
    public MemoData? GetMemo(int index)
    {
        return null;
    }

    /// <summary>
    /// Always returns true (null memo file is always "valid")
    /// </summary>
    public bool IsValid => true;

    /// <summary>
    /// No-op dispose
    /// </summary>
    public void Dispose() { }
}
