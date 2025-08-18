using BenchmarkDotNet.Running;
using DbfSharp.Benchmark;

public class Program
{
    private static readonly List<DbfFieldInfo> BenchmarkFields =
    [
        new DbfFieldInfo("ID", 'N', 10),
        new DbfFieldInfo("NAME", 'C', 50),
        new DbfFieldInfo("EMAIL", 'C', 50),
        new DbfFieldInfo("JOIN_DATE", 'D', 8),
        new DbfFieldInfo("SCORE", 'N', 10, 2),
        new DbfFieldInfo("ADDRESS", 'C', 100),
        new DbfFieldInfo("CITY", 'C', 50),
        new DbfFieldInfo("STATE", 'C', 2),
        new DbfFieldInfo("ZIP", 'C', 10),
        new DbfFieldInfo("NOTES", 'C', 100),
    ];

    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "generate-test-files")
        {
            GenerateAllTestFiles();
            return;
        }

        BenchmarkRunner.Run<DbfReaderBenchmarks>();
    }

    private static void GenerateAllTestFiles()
    {
        var testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(testDataDir);

        var rowCounts = new[] { 100, 10_000, 100_000, 1_000_000 };

        foreach (var rowCount in rowCounts)
        {
            var fileName = $"benchmark_{rowCount}.dbf";
            var filePath = Path.Combine(testDataDir, fileName);

            Console.WriteLine($"Generating {fileName} with {rowCount:N0} rows...");
            var startTime = DateTime.Now;

            DbfFileGenerator.Generate(filePath, rowCount, BenchmarkFields);

            var duration = DateTime.Now - startTime;
            var fileSize = new FileInfo(filePath).Length;
            Console.WriteLine(
                $"Generated {fileName} ({fileSize:N0} bytes) in {duration.TotalSeconds:F2}s"
            );
        }
    }
}
