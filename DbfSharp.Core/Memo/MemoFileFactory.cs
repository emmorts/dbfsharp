using DbfSharp.Core.Enums;

namespace DbfSharp.Core.Memo;

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
    public static IMemoFile? CreateMemoFile(
        string dbfFilePath,
        DbfVersion dbfVersion,
        DbfReaderOptions options
    )
    {
        if (!dbfVersion.SupportsMemoFields())
        {
            return null;
        }

        var memoFilePath = FindMemoFile(dbfFilePath);
        if (memoFilePath == null)
        {
            if (options.IgnoreMissingMemoFile)
            {
                return NullMemoFile.Instance;
            }

            throw new Exceptions.MissingMemoFileException(
                dbfFilePath,
                GetExpectedMemoFileName(dbfFilePath),
                $"Memo file not found for {dbfFilePath}"
            );
        }

        var extension = Path.GetExtension(memoFilePath).ToLowerInvariant();

        return extension switch
        {
            ".fpt" => new VfpMemoFile(memoFilePath, options),
            ".dbt" => CreateDbtMemoFile(memoFilePath, dbfVersion, options),
            _ => throw new NotSupportedException(
                $"Memo file extension {extension} is not supported"
            ),
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
        var extensions = new[] { ".fpt", ".dbt" };

        return extensions.Select(ext => basePath + ext).FirstOrDefault(File.Exists);
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
    private static IMemoFile CreateDbtMemoFile(
        string memoFilePath,
        DbfVersion dbfVersion,
        DbfReaderOptions options
    )
    {
        return dbfVersion switch
        {
            DbfVersion.DBase3PlusWithMemo => new Db3MemoFile(memoFilePath, options),
            DbfVersion.DBase4WithMemo => new Db4MemoFile(memoFilePath, options),
            DbfVersion.DBase4SqlTableWithMemo => new Db4MemoFile(memoFilePath, options),
            _ => new Db4MemoFile(memoFilePath, options), // Default to DB4 format
        };
    }
}
