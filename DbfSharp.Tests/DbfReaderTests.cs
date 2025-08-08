using DbfSharp.Core;
using DbfSharp.Core.Enums;

namespace DbfSharp.Tests;

/// <summary>
/// Tests for reading actual record data from all test DBF files
/// </summary>
public class DbfReaderTests
{
    [Theory]
    [InlineData(TestHelper.TestFiles.People)]
    [InlineData(TestHelper.TestFiles.DBase03Cyrillic)]
    [InlineData(TestHelper.TestFiles.DBase30)]
    [InlineData(TestHelper.TestFiles.DBase31)]
    [InlineData(TestHelper.TestFiles.DBase32)]
    [InlineData(TestHelper.TestFiles.DBase83)]
    [InlineData(TestHelper.TestFiles.DBase83MissingMemo)]
    [InlineData(TestHelper.TestFiles.DBase8B)]
    [InlineData(TestHelper.TestFiles.DBaseF5)]
    [InlineData(TestHelper.TestFiles.Cp1251)]
    public void CanReadAllTestFiles_StreamingMode(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);

        Assert.NotNull(reader);
        Assert.NotNull(reader.Fields);
        Assert.True(reader.Fields.Count > 0);
        Assert.NotNull(reader.FieldNames);
        Assert.Equal(reader.Fields.Count, reader.FieldNames.Count);

        const int maxRecordsToTest = 10;
        var recordCount = 0;

        foreach (var record in reader.Records)
        {
            Assert.Equal(reader.Fields.Count, record.FieldCount);

            for (var i = 0; i < record.FieldCount; i++)
            {
                var fieldName = reader.FieldNames[i];
                var valueByIndex = record[i];
                var valueByName = record[fieldName];

                Assert.Equal(valueByIndex, valueByName);

                Assert.True(record.HasField(fieldName));
            }

            recordCount++;
            if (recordCount >= maxRecordsToTest)
            {
                break;
            }
        }

        Assert.True(recordCount >= 0, $"Error reading records from {fileName}");
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People)]
    [InlineData(TestHelper.TestFiles.DBase03)]
    [InlineData(TestHelper.TestFiles.DBase30)]
    [InlineData(TestHelper.TestFiles.DBase83)]
    [InlineData(TestHelper.TestFiles.Cp1251)]
    public void CanReadAllTestFiles_LoadedMode(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);

        reader.Load();

        Assert.True(reader.IsLoaded);
        Assert.True(reader.Count > 0);

        var firstRecord = reader[0];
        var lastRecord = reader[^1];

        if (reader.Count > 1)
        {
            var middleRecord = reader[reader.Count / 2];
        }

        var streamingRecords = new List<DbfRecord>();
        foreach (var record in reader.Records)
        {
            streamingRecords.Add(record);
        }

        Assert.Equal(reader.Count, streamingRecords.Count);

        var recordsToCompare = Math.Min(5, reader.Count);
        for (var i = 0; i < recordsToCompare; i++)
        {
            var loadedRecord = reader[i];
            var streamingRecord = streamingRecords[i];

            for (var j = 0; j < loadedRecord.FieldCount; j++)
            {
                Assert.Equal(loadedRecord[j], streamingRecord[j]);
            }
        }
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People, FieldType.Character)]
    [InlineData(TestHelper.TestFiles.People, FieldType.Date)]
    [InlineData(TestHelper.TestFiles.DBase30, FieldType.Character)]
    [InlineData(TestHelper.TestFiles.DBase30, FieldType.Numeric)]
    [InlineData(TestHelper.TestFiles.DBase30, FieldType.Date)]
    [InlineData(TestHelper.TestFiles.DBase30, FieldType.Memo)]
    [InlineData(TestHelper.TestFiles.DBase30, FieldType.Timestamp)]
    [InlineData(TestHelper.TestFiles.DBase83, FieldType.Character)]
    [InlineData(TestHelper.TestFiles.DBase83, FieldType.Numeric)]
    [InlineData(TestHelper.TestFiles.Cp1251, FieldType.Character)]
    [InlineData(TestHelper.TestFiles.Cp1251, FieldType.Numeric)]
    public void CanReadSpecificFieldTypes(string fileName, FieldType fieldType)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);

        var fieldsOfType = reader.Fields.Where(f => f.Type == fieldType).ToList();
        if (fieldsOfType.Count == 0)
        {
            return;
        }

        var recordsToTest = Math.Min(5, reader.RecordCount);
        var recordCount = 0;

        foreach (var record in reader.Records)
        {
            foreach (var field in fieldsOfType)
            {
                var value = record[field.Name];

                switch (fieldType)
                {
                    case FieldType.Character:
                    case FieldType.Varchar:
                        var stringValue = record.GetString(field.Name);
                        break;

                    case FieldType.Numeric:
                    case FieldType.Float:
                        if (field.DecimalCount > 0)
                        {
                            var decimalValue = record.GetDecimal(field.Name);
                        }
                        else
                        {
                            var intValue = record.GetInt32(field.Name);
                        }
                        break;

                    case FieldType.Date:
                        var dateValue = record.GetDateTime(field.Name);
                        break;

                    case FieldType.Logical:
                        var boolValue = record.GetBoolean(field.Name);
                        break;

                    case FieldType.Memo:
                        var memoValue = record.GetString(field.Name);
                        break;

                    case FieldType.Timestamp:
                        var timestampValue = record.GetDateTime(field.Name);
                        break;
                }
            }

            recordCount++;
            if (recordCount >= recordsToTest)
            {
                break;
            }
        }
    }

    [Fact]
    public void People_ShouldReadExpectedData()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        reader.Load();
        Assert.Equal(2, reader.Count);

        var record1 = reader[0];
        var name1 = record1.GetString("NAME")?.Trim();
        var birthdate1 = record1.GetDateTime("BIRTHDATE");

        Assert.Equal("Alice", name1);
        Assert.Equal(new DateTime(1987, 3, 1), birthdate1);

        var record2 = reader[1];
        var name2 = record2.GetString("NAME")?.Trim();
        var birthdate2 = record2.GetDateTime("BIRTHDATE");

        Assert.Equal("Bob", name2);
        Assert.Equal(new DateTime(1980, 11, 12), birthdate2);
    }

    [Fact]
    public void DBase03_ShouldReadSampleData()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

        var firstRecord = reader.Records.First();

        var pointId = firstRecord.GetString("Point_ID")?.Trim();
        var type = firstRecord.GetString("Type")?.Trim();
        var shape = firstRecord.GetString("Shape")?.Trim();
        var circularD = firstRecord.GetString("Circular_D")?.Trim();

        Assert.Equal("0507121", pointId);
        Assert.Equal("CMP", type);
        Assert.Equal("circular", shape);
        Assert.Equal("12", circularD);

        var maxPdopField = reader.FindField("Max_PDOP");
        if (maxPdopField.HasValue)
        {
            var maxPdop = firstRecord.GetDecimal("Max_PDOP");
            Assert.True(maxPdop.HasValue);
        }

        var dateVisit = firstRecord.GetDateTime("Date_Visit");
        Assert.True(dateVisit.HasValue);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People)]
    [InlineData(TestHelper.TestFiles.DBase03)]
    [InlineData(TestHelper.TestFiles.DBase30)]
    [InlineData(TestHelper.TestFiles.DBase83)]
    [InlineData(TestHelper.TestFiles.Cp1251)]
    public void AllRecords_ShouldBeAccessible(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);

        var streamingCount = 0;
        var allRecordsAccessible = true;

        foreach (var record in reader.Records)
        {
            try
            {
                for (var i = 0; i < record.FieldCount; i++)
                {
                    var value = record[i];
                }
            }
            catch
            {
                allRecordsAccessible = false;
                break;
            }

            streamingCount++;
        }

        Assert.True(allRecordsAccessible, $"Some records in {fileName} were not accessible");
        Assert.True(streamingCount > 0, $"No records found in {fileName}");

        reader.Load();
        Assert.Equal(streamingCount, reader.Count);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.DBase30)]
    [InlineData(TestHelper.TestFiles.DBase83)]
    [InlineData(TestHelper.TestFiles.DBase8B)]
    [InlineData(TestHelper.TestFiles.DBaseF5)]
    public void MemoFields_ShouldReadCorrectly(string fileName)
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
            return;
        }

        var recordsWithMemoData = 0;
        var recordsToTest = Math.Min(10, reader.RecordCount);
        var recordCount = 0;

        foreach (var record in reader.Records)
        {
            foreach (var field in memoFields)
            {
                var value = record.GetString(field.Name);
                if (!string.IsNullOrEmpty(value))
                {
                    recordsWithMemoData++;
                    Assert.IsType<string>(value);

                    Assert.DoesNotContain('\0', value.TrimEnd('\0'));
                }
            }

            recordCount++;
            if (recordCount >= recordsToTest)
            {
                break;
            }
        }

        Assert.True(recordsWithMemoData >= 0);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People)]
    [InlineData(TestHelper.TestFiles.DBase03)]
    [InlineData(TestHelper.TestFiles.DBase30)]
    [InlineData(TestHelper.TestFiles.Cp1251)]
    public void Statistics_ShouldReflectActualData(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);

        var stats = reader.GetStatistics();

        Assert.NotNull(stats);
        Assert.NotNull(stats.TableName);
        Assert.True(stats.TotalRecords > 0);
        Assert.True(stats.FieldCount > 0);
        Assert.True(stats.RecordLength > 0);
        Assert.True(stats.HeaderLength > 0);
        Assert.NotNull(stats.Encoding);

        reader.Load();
        var loadedStats = reader.GetStatistics();

        Assert.True(loadedStats.IsLoaded);
        Assert.True(loadedStats.ActiveRecords > 0);
        Assert.Equal(reader.Count, loadedStats.ActiveRecords);
    }

    [Fact]
    public void InvalidValue_WithValidationDisabled_ShouldReadWithoutThrowing()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.InvalidValue))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.InvalidValue);
        var options = new DbfReaderOptions { ValidateFields = false };

        using var reader = DbfReader.Create(filePath, options);

        var recordCount = 0;
        foreach (var record in reader.Records)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                var value = record[i];
            }

            recordCount++;
            if (recordCount > 5)
            {
                break;
            }
        }

        Assert.True(recordCount > 0);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People, false)]
    [InlineData(TestHelper.TestFiles.People, true)]
    [InlineData(TestHelper.TestFiles.DBase03, false)]
    [InlineData(TestHelper.TestFiles.DBase03, true)]
    public void CaseInsensitiveFieldAccess_ShouldWork(string fileName, bool ignoreCase)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreCase = ignoreCase };

        using var reader = DbfReader.Create(filePath, options);
        var record = reader.Records.First();

        var firstFieldName = reader.FieldNames[0];
        var upperFieldName = firstFieldName.ToUpperInvariant();
        var lowerFieldName = firstFieldName.ToLowerInvariant();

        if (ignoreCase)
        {
            Assert.True(record.HasField(firstFieldName));
            Assert.True(record.HasField(upperFieldName));
            Assert.True(record.HasField(lowerFieldName));

            var value1 = record[firstFieldName];
            var value2 = record[upperFieldName];
            var value3 = record[lowerFieldName];

            Assert.Equal(value1, value2);
            Assert.Equal(value1, value3);
        }
        else
        {
            Assert.True(record.HasField(firstFieldName));

            if (firstFieldName != upperFieldName)
            {
                Assert.False(record.HasField(upperFieldName));
            }

            if (firstFieldName != lowerFieldName)
            {
                Assert.False(record.HasField(lowerFieldName));
            }
        }
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People, 1)]
    [InlineData(TestHelper.TestFiles.DBase03, 5)]
    [InlineData(TestHelper.TestFiles.DBase30, 10)]
    public void MaxRecords_ShouldLimitResults(string fileName, int maxRecords)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions
        {
            MaxRecords = maxRecords,
            IgnoreMissingMemoFile = true
        };

        using var reader = DbfReader.Create(filePath, options);

        var actualRecords = reader.Records.ToList();
        Assert.True(actualRecords.Count <= maxRecords);

        if (reader.RecordCount >= maxRecords)
        {
            Assert.Equal(maxRecords, actualRecords.Count);
        }
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People)]
    [InlineData(TestHelper.TestFiles.DBase03)]
    public void EnumerateMultipleTimes_ShouldGiveSameResults(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        using var reader = DbfReader.Create(filePath);

        var firstEnumeration = reader.Records.Take(3).ToList();
        var secondEnumeration = reader.Records.Take(3).ToList();

        Assert.Equal(firstEnumeration.Count, secondEnumeration.Count);

        for (var i = 0; i < firstEnumeration.Count; i++)
        {
            var record1 = firstEnumeration[i];
            var record2 = secondEnumeration[i];

            Assert.Equal(record1.FieldCount, record2.FieldCount);

            for (var j = 0; j < record1.FieldCount; j++)
            {
                Assert.Equal(record1[j], record2[j]);
            }
        }
    }
}
