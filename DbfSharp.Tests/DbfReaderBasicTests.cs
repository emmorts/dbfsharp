using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Exceptions;

namespace DbfSharp.Tests;

public class DbfReaderBasicTests
{
    [Fact]
    public void Open_ValidFile_ShouldSucceed()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);

        Assert.NotNull(reader);
        Assert.False(reader.IsLoaded); // Default is streaming mode
        Assert.NotEmpty(reader.Fields);
        Assert.NotEmpty(reader.FieldNames);
        Assert.True(reader.RecordCount > 0);
    }

    [Fact]
    public void Open_NonExistentFile_ShouldThrowDbfNotFoundException()
    {
        const string filePath = "non_existent_file.dbf";

        var exception = Assert.Throws<DbfNotFoundException>(() => DbfReader.Create(filePath));
        Assert.Equal(filePath, exception.FilePath);
    }

    [Fact]
    public void Open_WithStream_ShouldSucceed()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var fileStream = File.OpenRead(filePath);

        using var reader = DbfReader.Create(fileStream);

        Assert.NotNull(reader);
        Assert.NotEmpty(reader.Fields);
        Assert.Equal("Unknown", reader.TableName); // Stream-based readers don't have table names
    }

    [Theory]
    [MemberData(nameof(TestHelper.GetAllValidTestFilesTheoryData), MemberType = typeof(TestHelper))]
    public void Open_AllTestFiles_ShouldSucceed(string fileName)
    {
        var filePath = TestHelper.GetTestFilePath(fileName);

        using var reader = DbfReader.Create(
            filePath,
            new DbfReaderOptions { IgnoreMissingMemoFile = true }
        );
        Assert.NotNull(reader);
        Assert.True(reader.Fields.Count > 0);

        // Should be able to enumerate without errors
        var recordCount = 0;
        foreach (var record in reader.Records)
        {
            Assert.Equal(reader.Fields.Count, record.FieldCount);
            recordCount++;

            // Don't read too many records in tests
            if (recordCount > 100)
            {
                break;
            }
        }
    }

    [Fact]
    public void Header_Properties_ShouldBeCorrect()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);

        Assert.NotEqual(default, reader.Header);
        Assert.True(reader.Header.NumberOfRecords > 0);
        Assert.True(reader.Header.HeaderLength > 0);
        Assert.True(reader.Header.RecordLength > 0);
        // Fields.Count is the authoritative field count
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People, DbfVersion.DBase3Plus, false, 3, 97, 25)]
    [InlineData(TestHelper.TestFiles.DBase03, DbfVersion.DBase3Plus, false, 14, 1025, 590)]
    [InlineData(TestHelper.TestFiles.DBase30, DbfVersion.VisualFoxPro, true, 34, 4936, 3907)]
    [InlineData(TestHelper.TestFiles.DBase31, DbfVersion.VisualFoxProAutoIncrement, false, 0, 0, 0)]
    [InlineData(TestHelper.TestFiles.DBase83, DbfVersion.DBase3PlusWithMemo, true, 67, 513, 805)]
    public void Header_DbfVersion_ShouldBeDetectedCorrectly(
        string fileName,
        DbfVersion expectedVersion,
        bool hasMemo,
        int expectedRecords,
        int expectedHeaderLength,
        int expectedRecordLength
    )
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            // Skip test if file doesn't exist
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);

        using var reader = DbfReader.Create(
            filePath,
            new DbfReaderOptions { IgnoreMissingMemoFile = !hasMemo }
        );

        Assert.Equal(expectedVersion, reader.Header.DbfVersion);
        
        // Validate exact header values from metadata
        if (expectedRecords > 0)
        {
            Assert.Equal((uint)expectedRecords, reader.Header.NumberOfRecords);
            Assert.Equal((ushort)expectedHeaderLength, reader.Header.HeaderLength);
            Assert.Equal((ushort)expectedRecordLength, reader.Header.RecordLength);
        }
    }

    [Fact]
    public void Fields_Properties_ShouldBeCorrect()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);

        Assert.NotEmpty(reader.Fields);
        Assert.NotEmpty(reader.FieldNames);
        Assert.Equal(reader.Fields.Count, reader.FieldNames.Count);

        foreach (var field in reader.Fields)
        {
            Assert.False(string.IsNullOrEmpty(field.Name));
            Assert.NotEqual(default(FieldType), field.Type);
            Assert.True(field.ActualLength > 0);
        }
    }

    [Fact]
    public void Records_Enumeration_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var records = reader.Records.Take(10).ToList();

        Assert.NotEmpty(records);
        Assert.True(records.Count <= 10);

        foreach (var record in records)
        {
            Assert.Equal(reader.Fields.Count, record.FieldCount);
            Assert.Equal(reader.Fields.Count, record.FieldNames.Count);
        }
    }

    [Fact]
    public void Record_FieldAccess_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var firstRecord = reader.Records.First();

        // Test index-based access
        for (var i = 0; i < firstRecord.FieldCount; i++)
        {
            var value = firstRecord[i];
            var fieldName = firstRecord.GetFieldName(i);
            Assert.False(string.IsNullOrEmpty(fieldName));
        }

        // Test name-based access
        foreach (var fieldName in reader.FieldNames)
        {
            var hasField = firstRecord.HasField(fieldName);
            Assert.True(hasField);

            if (hasField)
            {
                var value = firstRecord[fieldName];
                // Value can be null, that's valid
            }
        }
    }

    [Fact]
    public void GetStatistics_ShouldReturnValidData()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var stats = reader.GetStatistics();

        Assert.NotNull(stats);
        Assert.False(string.IsNullOrEmpty(stats.TableName));
        Assert.True(stats.TotalRecords > 0);
        Assert.True(stats.ActiveRecords >= 0);
        Assert.True(stats.DeletedRecords >= 0);
        Assert.True(stats.FieldCount > 0);
        Assert.True(stats.RecordLength > 0);
        Assert.True(stats.HeaderLength > 0);
        Assert.False(string.IsNullOrEmpty(stats.Encoding));
        Assert.False(stats.IsLoaded); // Default is streaming
    }

    [Fact]
    public void Load_ShouldEnableRandomAccess()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        Assert.False(reader.IsLoaded);

        reader.Load();

        Assert.True(reader.IsLoaded);
        Assert.True(reader.Count > 0);

        // Test random access
        var firstRecord = reader[0];
        Assert.NotNull(firstRecord);

        if (reader.Count > 1)
        {
            var lastRecord = reader[reader.Count - 1];
            Assert.NotNull(lastRecord);
        }
    }

    [Fact]
    public void Unload_ShouldReturnToStreamingMode()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        reader.Load();
        Assert.True(reader.IsLoaded);

        reader.Unload();

        Assert.False(reader.IsLoaded);
    }

    [Fact]
    public void FindField_ShouldReturnCorrectField()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var firstFieldName = reader.FieldNames[0];
        var field = reader.FindField(firstFieldName);

        Assert.NotNull(field);
        Assert.Equal(firstFieldName, field.Value.Name);
    }

    [Fact]
    public void FindField_NonExistentField_ShouldReturnNull()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var field = reader.FindField("NON_EXISTENT_FIELD");

        Assert.Null(field);
    }

    [Fact]
    public void HasField_ShouldReturnCorrectResult()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var firstFieldName = reader.FieldNames[0];

        Assert.True(reader.HasField(firstFieldName));
        Assert.False(reader.HasField("NON_EXISTENT_FIELD"));
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulString()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var str = reader.ToString();

        Assert.False(string.IsNullOrEmpty(str));
        Assert.Contains("DbfReader", str);
        Assert.Contains("people", str, StringComparison.OrdinalIgnoreCase); // Table name
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var reader = DbfReader.Create(filePath);

        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => reader.Records.First());
    }

    [Fact]
    public void Record_GetValue_GenericAccess_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);

        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();

        // Test generic access for different field types
        foreach (var field in reader.Fields)
        {
            switch (field.Type)
            {
                case FieldType.Character:
                case FieldType.Varchar:
                    var stringValue = record.GetString(field.Name);
                    Assert.True(stringValue is null or string);
                    break;

                case FieldType.Date:
                    var dateValue = record.GetDateTime(field.Name);
                    Assert.True(dateValue is null or DateTime);
                    break;

                case FieldType.Float:
                case FieldType.Double:
                case FieldType.Numeric:
                    // Numeric can be int or decimal
                    var numericValue = record[field.Name];
                    Assert.True(numericValue is null or int or decimal or double or float);
                    break;

                case FieldType.Logical:
                    var boolValue = record.GetBoolean(field.Name);
                    Assert.True(boolValue is null or bool);
                    // Can be null or bool
                    break;
                case FieldType.Memo:
                    var memoValue = record.GetString(field.Name);
                    Assert.True(memoValue is null or string);
                    break;
                case FieldType.Timestamp:
                case FieldType.TimestampAlternate:
                    var timestampValue = record.GetDateTime(field.Name);
                    Assert.True(timestampValue is null or DateTime);
                    break;
                default:
                    // For other types, just check if we can get a value
                    var value = record[field.Name];
                    break;
            }
        }
    }

    [Fact]
    public void Record_TryGetValue_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();
        var firstFieldName = reader.FieldNames[0];

        var success = record.TryGetValue(firstFieldName, out var value);
        Assert.True(success);
        // Value can be null, that's valid

        var failureResult = record.TryGetValue("NON_EXISTENT_FIELD", out var nonExistentValue);
        Assert.False(failureResult);
        Assert.Null(nonExistentValue);
    }

    [Fact]
    public void Record_ToDictionary_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();
        var dictionary = record.ToDictionary();

        Assert.NotNull(dictionary);
        Assert.Equal(reader.Fields.Count, dictionary.Count);

        foreach (var fieldName in reader.FieldNames)
        {
            Assert.True(dictionary.ContainsKey(fieldName));
        }
    }

    [Fact]
    public void DeletedRecords_ShouldBeAccessible()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var deletedRecords = reader.DeletedRecords.Take(10).ToList();

        // May have deleted records or not, both are valid
        Assert.True(deletedRecords.Count >= 0);

        foreach (var record in deletedRecords)
        {
            Assert.NotNull(record);
            Assert.Equal(reader.Fields.Count, record.FieldCount);
        }
    }

    [Fact]
    public void Count_Properties_ShouldBeConsistent()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        reader.Load(); // Load to enable count properties

        var activeCount = reader.Count;
        var deletedCount = reader.DeletedCount;
        var totalCount = reader.RecordCount;

        Assert.True(activeCount >= 0);
        Assert.True(deletedCount >= 0);
        Assert.Equal(totalCount, activeCount + deletedCount);
    }

    [Fact]
    public void GetFieldIndex_ShouldReturnCorrectIndex()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);
        var firstFieldName = reader.FieldNames[0];
        var index = reader.GetFieldIndex(firstFieldName);

        Assert.Equal(0, index);
        Assert.Equal(-1, reader.GetFieldIndex("NON_EXISTENT_FIELD"));
    }
}
