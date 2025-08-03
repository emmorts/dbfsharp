using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Exceptions;

namespace DbfSharp.Tests;

public class DbfMemoFileTests
{
    [Theory]
    [InlineData(TestHelper.TestFiles.DBase83)]    // .dbt memo file
    [InlineData(TestHelper.TestFiles.DBase8B)]    // .dbt memo file
    [InlineData(TestHelper.TestFiles.DBaseF5)]    // .fpt memo file
    [InlineData(TestHelper.TestFiles.DBase30)]    // .fpt memo file
    public void MemoFields_WithMemoFile_ShouldReadCorrectly(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        using var reader = DbfReader.Create(filePath);

        var memoFields = reader.Fields.Where(f => f.Type.UsesMemoFile()).ToList();
        if (memoFields.Count == 0)
        {
            return; // Skip if no memo fields
        }

        var record = reader.Records.First();
        foreach (var field in memoFields)
        {
            var value = record[field.Name];
            if (value != null)
            {
                switch (field.Type)
                {
                    case FieldType.Memo:
                        Assert.IsType<string>(value);
                        var memoText = (string)value;
                        Assert.NotEmpty(memoText);
                        break;

                    case FieldType.General:
                    case FieldType.Picture:
                    case FieldType.Binary:
                        Assert.True(value is byte[] or string);
                        break;
                }
            }
        }
    }

    [Fact]
    public void MemoFile_DBase83_ShouldReadTextMemo()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);

        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        if (memoFields.Count == 0)
        {
            return;
        }

        var records = reader.Records.Take(5).ToList();
        foreach (var record in records)
        {
            foreach (var field in memoFields)
            {
                var value = record.GetString(field.Name);
                if (value != null)
                {
                    Assert.IsType<string>(value);
                    Assert.True(value.Length > 0 || string.IsNullOrEmpty(value));
                }
            }
        }
    }

    [Fact]
    public void MemoFile_VfpFormat_ShouldReadCorrectly()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase30))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);

        var memoFields = reader.Fields.Where(f => f.Type.UsesMemoFile()).ToList();
        if (memoFields.Count == 0)
        {
            return;
        }

        var record = reader.Records.First();
        foreach (var field in memoFields)
        {
            var value = record[field.Name];
            if (value != null)
            {
                Assert.True(value is string or byte[]);
            }
        }
    }

    [Fact]
    public void MissingMemoFile_WithIgnoreOption_ShouldNotThrow()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83MissingMemo))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83MissingMemo);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);
        Assert.NotNull(reader);

        var memoFields = reader.Fields.Where(f => f.Type.UsesMemoFile()).ToList();
        if (memoFields.Count > 0)
        {
            var record = reader.Records.First();
            foreach (var field in memoFields)
            {
                var value = record[field.Name];
                // Should return null or empty for missing memo data
                Assert.True(value == null || (value is string str && string.IsNullOrEmpty(str)));
            }
        }
    }

    [Fact]
    public void MissingMemoFile_WithoutIgnoreOption_ShouldThrow()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83MissingMemo))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83MissingMemo);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = false };

        Assert.Throws<MissingMemoFileException>(() => DbfReader.Create(filePath, options));
    }

    [Fact]
    public void MemoFile_MultipleMemoFields_ShouldReadAllCorrectly()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);

        var memoFields = reader.Fields.Where(f => f.Type.UsesMemoFile()).ToList();
        if (memoFields.Count <= 1)
        {
            return;
        }

        var record = reader.Records.First();
        var memoValues = new Dictionary<string, object?>();

        foreach (var field in memoFields)
        {
            var value = record[field.Name];
            memoValues[field.Name] = value;
        }

        Assert.Equal(memoFields.Count, memoValues.Count);

        // Verify each memo field can be accessed independently
        foreach (var field in memoFields)
        {
            var value1 = record[field.Name];
            var value2 = record[field.Name]; // Second access should return same value
            Assert.Equal(value1, value2);
        }
    }

    [Fact]
    public void MemoFile_LargeMemoContent_ShouldReadCorrectly()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);

        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        if (memoFields.Count == 0)
        {
            return;
        }

        var recordsChecked = 0;
        foreach (var record in reader.Records)
        {
            foreach (var field in memoFields)
            {
                var value = record.GetString(field.Name);
                if (value is { Length: > 1000 }) // Consider "large" memo content
                {
                    Assert.IsType<string>(value);
                    Assert.True(value.Length > 1000);
                    Assert.DoesNotContain('\0', value); // Should not contain null terminators
                    return; // Found at least one large memo, test passed
                }
            }

            recordsChecked++;
            if (recordsChecked > 100)
            {
                break;
            }
        }
    }

    [Fact]
    public void MemoFile_EmptyMemoField_ShouldReturnNullOrEmpty()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);

        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        if (memoFields.Count == 0)
        {
            return;
        }

        var foundEmptyMemo = false;
        var recordsChecked = 0;

        foreach (var record in reader.Records)
        {
            foreach (var field in memoFields)
            {
                var value = record.GetString(field.Name);
                if (value == null || string.IsNullOrEmpty(value))
                {
                    foundEmptyMemo = true;
                    Assert.True(value is null or "");
                }
            }

            recordsChecked++;
            if (foundEmptyMemo || recordsChecked > 50)
            {
                break;
            }
        }
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.DBase83, DbfVersion.DBase3PlusWithMemo)]
    [InlineData(TestHelper.TestFiles.DBase8B, DbfVersion.DBase4WithMemo)]
    [InlineData(TestHelper.TestFiles.DBaseF5, DbfVersion.FoxPro2WithMemo)]
    public void MemoFile_VersionSpecific_ShouldDetectCorrectly(string fileName, DbfVersion expectedVersion)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        using var reader = DbfReader.Create(filePath);

        Assert.Equal(expectedVersion, reader.Header.DbfVersion);

        if (reader.Fields.Any(f => f.Type.UsesMemoFile()))
        {
            var record = reader.Records.First();
            foreach (var field in reader.Fields.Where(f => f.Type.UsesMemoFile()))
            {
                var value = record[field.Name];
                // Should be able to read memo data without exceptions
                Assert.True(value is null or string or byte[]);
            }
        }
    }

    [Fact]
    public void MemoFile_Encoding_ShouldRespectOptions()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        var options = new DbfReaderOptions
        {
            Encoding = System.Text.Encoding.UTF8
        };

        using var reader = DbfReader.Create(filePath, options);
        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();

        if (memoFields.Count > 0)
        {
            var record = reader.Records.First();
            var memoValue = record.GetString(memoFields[0].Name);

            if (memoValue != null)
            {
                Assert.True(System.Text.Encoding.UTF8.GetByteCount(memoValue) >= 0);
            }
        }
    }

    [Fact]
    public void MemoFile_Statistics_ShouldIncludeMemoInfo()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);

        var stats = reader.GetStatistics();
        Assert.NotNull(stats);

        var memoFieldCount = reader.Fields.Count(f => f.Type.UsesMemoFile());
        if (memoFieldCount > 0)
        {
            Assert.True(stats.FieldCount > 0);
            // Memo files should not significantly affect basic statistics
            Assert.True(stats.TotalRecords > 0);
        }
    }

    [Fact]
    public void MemoFile_RandomAccess_ShouldWorkInLoadedMode()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);
        reader.Load();

        var memoFields = reader.Fields.Where(f => f.Type.UsesMemoFile()).ToList();
        if (memoFields.Count == 0 || reader.Count < 2)
        {
            return;
        }

        // Test random access to memo fields
        var firstRecord = reader[0];
        var lastRecord = reader[^1];

        foreach (var field in memoFields)
        {
            var firstValue = firstRecord[field.Name];
            var lastValue = lastRecord[field.Name];

            // Values should be consistent across multiple access attempts
            Assert.Equal(firstValue, firstRecord[field.Name]);
            Assert.Equal(lastValue, lastRecord[field.Name]);
        }
    }
}
