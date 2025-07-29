using System.Reflection;

namespace DbfSharp.Tests;

/// <summary>
/// Helper methods for test setup and data access
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Gets the path to the test data directory
    /// </summary>
    public static string TestDataPath
    {
        get
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var directory = Path.GetDirectoryName(assemblyLocation)!;

            return Path.Combine(directory, "Resources");
        }
    }

    /// <summary>
    /// Gets the full path to a test DBF file
    /// </summary>
    /// <param name="fileName">The DBF file name</param>
    /// <returns>The full path to the test file</returns>
    public static string GetTestFilePath(string fileName)
    {
        var path = Path.Combine(TestDataPath, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Test file not found: {fileName}. Expected at: {path}"
            );
        }

        return path;
    }

    /// <summary>
    /// Checks if a test file exists
    /// </summary>
    /// <param name="fileName">The file name to check</param>
    /// <returns>True if the file exists</returns>
    public static bool TestFileExists(string fileName)
    {
        var path = Path.Combine(TestDataPath, fileName);
        return File.Exists(path);
    }

    /// <summary>
    /// Gets all available test DBF files
    /// </summary>
    /// <returns>An enumerable of test file names</returns>
    public static IEnumerable<string> GetAllTestDbfFiles()
    {
        if (!Directory.Exists(TestDataPath))
        {
            return [];
        }

        return Directory
            .GetFiles(TestDataPath, "*.dbf")
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Cast<string>()
            .OrderBy(name => name);
    }

    /// <summary>
    /// Creates a temporary copy of a test file for modification tests
    /// </summary>
    /// <param name="sourceFileName">The source test file name</param>
    /// <returns>The path to the temporary copy</returns>
    public static string CreateTempCopy(string sourceFileName)
    {
        var sourcePath = GetTestFilePath(sourceFileName);
        var tempPath = Path.GetTempFileName();
        File.Copy(sourcePath, tempPath, true);

        // Also copy memo file if it exists
        var memoExtensions = new[] { ".fpt", ".dbt" };
        var baseSourcePath = Path.ChangeExtension(sourcePath, null);
        var baseTempPath = Path.ChangeExtension(tempPath, null);

        foreach (var ext in memoExtensions)
        {
            var sourceMemoPath = baseSourcePath + ext;
            if (File.Exists(sourceMemoPath))
            {
                var tempMemoPath = baseTempPath + ext;
                File.Copy(sourceMemoPath, tempMemoPath, true);
            }
        }

        return tempPath;
    }

    /// <summary>
    /// Safely deletes a temporary file and its associated memo files
    /// </summary>
    /// <param name="filePath">The file path to delete</param>
    public static void DeleteTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Delete associated memo files
            var memoExtensions = new[] { ".fpt", ".dbt" };
            var basePath = Path.ChangeExtension(filePath, null);

            foreach (var ext in memoExtensions)
            {
                var memoPath = basePath + ext;
                if (File.Exists(memoPath))
                {
                    File.Delete(memoPath);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    /// <summary>
    /// Gets test file information for parameterized tests
    /// </summary>
    public static class TestFiles
    {
        public const string People = "people.dbf";
        public const string DBase02 = "dbase_02.dbf";
        public const string DBase03 = "dbase_03.dbf";
        public const string DBase03Cyrillic = "dbase_03_cyrillic.dbf";
        public const string DBase30 = "dbase_30.dbf";
        public const string DBase31 = "dbase_31.dbf";
        public const string DBase32 = "dbase_32.dbf";
        public const string DBase83 = "dbase_83.dbf";
        public const string DBase83MissingMemo = "dbase_83_missing_memo.dbf";
        public const string DBase8B = "dbase_8b.dbf";
        public const string DBase8C = "dbase_8c.dbf";
        public const string DBaseF5 = "dbase_f5.dbf";
        public const string InvalidValue = "invalid_value.dbf";
        public const string Cp1251 = "cp1251.dbf";
        public const string Polygon = "polygon.dbf";
    }

    /// <summary>
    /// Expected DBF versions for test files
    /// </summary>
    public static readonly Dictionary<string, Core.Enums.DbfVersion> ExpectedVersions = new()
    {
        { TestFiles.DBase02, Core.Enums.DbfVersion.DBase2 },
        { TestFiles.DBase03, Core.Enums.DbfVersion.DBase3Plus },
        { TestFiles.DBase03Cyrillic, Core.Enums.DbfVersion.DBase3Plus },
        { TestFiles.DBase30, Core.Enums.DbfVersion.VisualFoxPro },
        { TestFiles.DBase31, Core.Enums.DbfVersion.VisualFoxProAutoIncrement },
        { TestFiles.DBase32, Core.Enums.DbfVersion.VisualFoxProVarchar },
        { TestFiles.DBase83, Core.Enums.DbfVersion.DBase3PlusWithMemo },
        { TestFiles.DBase8B, Core.Enums.DbfVersion.DBase4WithMemo },
        { TestFiles.DBaseF5, Core.Enums.DbfVersion.FoxPro2WithMemo },
    };

    /// <summary>
    /// Gets theory data for all available test files
    /// </summary>
    /// <returns>Theory data containing file names</returns>
    public static TheoryData<string> GetAllValidTestFilesTheoryData()
    {
        var theoryData = new TheoryData<string>
        {
            TestFiles.People,
            TestFiles.DBase02,
            TestFiles.DBase03,
            TestFiles.DBase03Cyrillic,
            TestFiles.DBase30,
            TestFiles.DBase31,
            TestFiles.DBase32,
            TestFiles.DBase83,
            TestFiles.DBase83MissingMemo,
            TestFiles.DBase8B,
            TestFiles.DBaseF5,
            TestFiles.Cp1251
        };

        return theoryData;
    }

    /// <summary>
    /// Gets theory data for files with specific characteristics
    /// </summary>
    /// <param name="predicate">Predicate to filter files</param>
    /// <returns>Theory data containing filtered file names</returns>
    public static TheoryData<string> GetFilteredTestFilesTheoryData(Func<string, bool> predicate)
    {
        var theoryData = new TheoryData<string>();
        foreach (var file in GetAllTestDbfFiles().Where(predicate))
        {
            theoryData.Add(file);
        }

        return theoryData;
    }

    /// <summary>
    /// Gets theory data for files that should have memo files
    /// </summary>
    public static TheoryData<string> GetMemoFileTestData()
    {
        return GetFilteredTestFilesTheoryData(fileName =>
            fileName.Contains("83")
            || fileName.Contains("8b")
            || fileName.Contains("8c")
            || fileName.Contains("f5")
        );
    }
}
