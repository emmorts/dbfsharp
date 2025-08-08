using DbfSharp.Core;

namespace DbfSharp.Tests;

/// <summary>
/// Test to discover the actual structure of our test files
/// </summary>
public class ActualFileStructureTest
{
    [Fact]
    public void DiscoverActualFileStructures()
    {
        var testFiles = new[]
        {
            TestHelper.TestFiles.People,
            TestHelper.TestFiles.DBase03,
            TestHelper.TestFiles.DBase30,
            TestHelper.TestFiles.DBase83,
            TestHelper.TestFiles.Cp1251
        };

        foreach (var fileName in testFiles)
        {
            if (!TestHelper.TestFileExists(fileName))
            {
                continue;
            }

            var filePath = TestHelper.GetTestFilePath(fileName);
            var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
            using var reader = DbfReader.Create(filePath, options);

            // output the actual structure for debugging
            Console.WriteLine($"\n=== {fileName} ===");
            Console.WriteLine($"Version: {reader.Header.DbfVersion}");
            Console.WriteLine($"Records: {reader.Header.NumberOfRecords}");
            Console.WriteLine($"Header Length: {reader.Header.HeaderLength}");  
            Console.WriteLine($"Record Length: {reader.Header.RecordLength}");
            Console.WriteLine($"Field Count: {reader.Fields.Count}");

            if (reader.Fields.Count <= 20) // only show details for files with reasonable field counts
            {
                Console.WriteLine("Fields:");
                for (var i = 0; i < reader.Fields.Count; i++)
                {
                    var field = reader.Fields[i];
                    Console.WriteLine($"  {i + 1}: {field.Name} ({field.Type}) Length={field.Length} Decimals={field.DecimalCount}");
                }

                // load and show first record if available
                reader.Load();
                if (reader.Count > 0)
                {
                    Console.WriteLine("First Record:");
                    var firstRecord = reader[0];
                    for (var i = 0; i < firstRecord.FieldCount; i++)
                    {
                        var fieldName = reader.Fields[i].Name;
                        var value = firstRecord[i];
                        Console.WriteLine($"  {fieldName}: {value}");
                    }
                }
            }
        }

        // this test always passes - it's just for discovery
        Assert.True(true);
    }
}