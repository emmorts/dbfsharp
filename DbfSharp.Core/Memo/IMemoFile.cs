using DbfSharp.Core.Enums;

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
    byte[]? GetMemo(int index);

    /// <summary>
    /// Checks if the memo file is valid and accessible
    /// </summary>
    bool IsValid { get; }
}

/// <summary>
/// Base class for memo data with type information
/// </summary>
public abstract class MemoData
{
    /// <summary>
    /// Gets the raw memo data
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Initializes a new instance of MemoData
    /// </summary>
    /// <param name="data">The raw memo data</param>
    protected MemoData(ReadOnlyMemory<byte> data)
    {
        Data = data;
    }

    /// <summary>
    /// Implicitly converts MemoData to byte array
    /// </summary>
    public static implicit operator byte[](MemoData memo) => memo.Data.ToArray();

    /// <summary>
    /// Implicitly converts MemoData to ReadOnlySpan
    /// </summary>
    public static implicit operator ReadOnlySpan<byte>(MemoData memo) => memo.Data.Span;
}

/// <summary>
/// Represents text memo data
/// </summary>
public sealed class TextMemo : MemoData
{
    /// <summary>
    /// Initializes a new instance of TextMemo
    /// </summary>
    /// <param name="data">The text data</param>
    public TextMemo(ReadOnlyMemory<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Converts the memo to a string using the specified encoding
    /// </summary>
    /// <param name="encoding">The encoding to use</param>
    /// <returns>The memo as a string</returns>
    public string ToString(System.Text.Encoding encoding)
    {
        return encoding.GetString(Data.Span);
    }
}

/// <summary>
/// Represents binary memo data (images, OLE objects, etc.)
/// </summary>
public class BinaryMemo : MemoData
{
    /// <summary>
    /// Initializes a new instance of BinaryMemo
    /// </summary>
    /// <param name="data">The binary data</param>
    public BinaryMemo(ReadOnlyMemory<byte> data) : base(data)
    {
    }
}

/// <summary>
/// Represents picture memo data
/// </summary>
public sealed class PictureMemo : BinaryMemo
{
    /// <summary>
    /// Initializes a new instance of PictureMemo
    /// </summary>
    /// <param name="data">The picture data</param>
    public PictureMemo(ReadOnlyMemory<byte> data) : base(data)
    {
    }
}

/// <summary>
/// Represents OLE object memo data
/// </summary>
public sealed class ObjectMemo : BinaryMemo
{
    /// <summary>
    /// Initializes a new instance of ObjectMemo
    /// </summary>
    /// <param name="data">The OLE object data</param>
    public ObjectMemo(ReadOnlyMemory<byte> data) : base(data)
    {
    }
}

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
    public byte[]? GetMemo(int index) => null;

    /// <summary>
    /// Always returns true (null memo file is always "valid")
    /// </summary>
    public bool IsValid => true;

    /// <summary>
    /// No-op dispose
    /// </summary>
    public void Dispose() { }
}

/// <summary>
/// Factory for creating memo file instances based on file extension and DBF version
/// </summary>
public static class MemoFileFactory
{
    /// <summary>
    /// Creates a memo file instance for the given DBF file
    /// </summary>
    /// <param name="dbfFilePath">The path to the DBF file</param>
    /// <param name="dbfVersion">The DBF version</param>
    /// <param name="options">Reader options</param>
    /// <returns>A memo file instance, or null if no memo file is needed</returns>
    public static IMemoFile? CreateMemoFile(string dbfFilePath, DbfVersion dbfVersion, DbfReaderOptions options)
    {
        if (!dbfVersion.SupportsMemoFields())
            return null;

        var memoFilePath = FindMemoFile(dbfFilePath);
        if (memoFilePath == null)
        {
            if (options.IgnoreMissingMemoFile)
                return NullMemoFile.Instance;
            
            throw new Exceptions.MissingMemoFileException(
                dbfFilePath,
                GetExpectedMemoFileName(dbfFilePath),
                $"Memo file not found for {dbfFilePath}");
        }

        var extension = Path.GetExtension(memoFilePath).ToLowerInvariant();
        return extension switch
        {
            ".fpt" => new VfpMemoFile(memoFilePath, options),
            ".dbt" => CreateDbtMemoFile(memoFilePath, dbfVersion, options),
            _ => throw new NotSupportedException($"Memo file extension {extension} is not supported")
        };
    }

    /// <summary>
    /// Finds a memo file associated with the given DBF file
    /// </summary>
    /// <param name="dbfFilePath">The DBF file path</param>
    /// <returns>The memo file path, or null if not found</returns>
    private static string? FindMemoFile(string dbfFilePath)
    {
        var basePath = Path.ChangeExtension(dbfFilePath, null);
        
        // Try different extensions (case-insensitive)
        var extensions = new[] { ".fpt", ".FPT", ".dbt", ".DBT" };
        
        foreach (var ext in extensions)
        {
            var memoPath = basePath + ext;
            if (File.Exists(memoPath))
                return memoPath;
        }

        return null;
    }

    /// <summary>
    /// Gets the expected memo file name for a DBF file
    /// </summary>
    /// <param name="dbfFilePath">The DBF file path</param>
    /// <returns>The expected memo file name</returns>
    private static string GetExpectedMemoFileName(string dbfFilePath)
    {
        var basePath = Path.ChangeExtension(dbfFilePath, null);
        return basePath + ".fpt"; // Default to FPT
    }

    /// <summary>
    /// Creates a DBT memo file instance based on the DBF version
    /// </summary>
    /// <param name="memoFilePath">The memo file path</param>
    /// <param name="dbfVersion">The DBF version</param>
    /// <param name="options">Reader options</param>
    /// <returns>A DBT memo file instance</returns>
    private static IMemoFile CreateDbtMemoFile(string memoFilePath, DbfVersion dbfVersion, DbfReaderOptions options)
    {
        return dbfVersion switch
        {
            DbfVersion.DBase3PlusWithMemo => new Db3MemoFile(memoFilePath, options),
            DbfVersion.DBase4WithMemo => new Db4MemoFile(memoFilePath, options),
            DbfVersion.DBase4SqlTableWithMemo => new Db4MemoFile(memoFilePath, options),
            _ => new Db4MemoFile(memoFilePath, options) // Default to DB4 format
        };
    }
}