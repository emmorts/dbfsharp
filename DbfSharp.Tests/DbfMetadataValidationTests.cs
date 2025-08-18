using DbfSharp.Core;
using DbfSharp.Core.Enums;

namespace DbfSharp.Tests;

public class DbfMetadataValidationTests
{
    [Fact]
    public void People_ShouldMatchExpectedMetadata()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        Assert.Equal(DbfVersion.DBase3Plus, reader.Header.DbfVersion);
        Assert.Equal(3u, reader.Header.NumberOfRecords);
        Assert.Equal((ushort)97, reader.Header.HeaderLength);
        Assert.Equal((ushort)25, reader.Header.RecordLength);

        Assert.Equal(2, reader.Fields.Count);

        var nameField = reader.Fields[0];
        Assert.Equal("NAME", nameField.Name);
        Assert.Equal(FieldType.Character, nameField.Type);
        Assert.Equal(16, nameField.Length);
        Assert.Equal(0, nameField.DecimalCount);

        var birthdateField = reader.Fields[1];
        Assert.Equal("BIRTHDATE", birthdateField.Name);
        Assert.Equal(FieldType.Date, birthdateField.Type);
        Assert.Equal(8, birthdateField.Length);
        Assert.Equal(0, birthdateField.DecimalCount);

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
    public void DBase03_ShouldMatchExpectedMetadata()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

        Assert.Equal(DbfVersion.DBase3Plus, reader.Header.DbfVersion);
        Assert.Equal(14u, reader.Header.NumberOfRecords);
        Assert.Equal((ushort)1025, reader.Header.HeaderLength);
        Assert.Equal((ushort)590, reader.Header.RecordLength);

        Assert.Equal(31, reader.Fields.Count);

        var pointIdField = reader.Fields[0];
        Assert.Equal("Point_ID", pointIdField.Name);
        Assert.Equal(FieldType.Character, pointIdField.Type);
        Assert.Equal(12, pointIdField.Length);

        var typeField = reader.FindField("Type");
        Assert.NotNull(typeField);
        Assert.Equal(FieldType.Character, typeField.Value.Type);
        Assert.Equal(20, typeField.Value.Length);

        var dateVisitField = reader.FindField("Date_Visit");
        Assert.NotNull(dateVisitField);
        Assert.Equal(FieldType.Date, dateVisitField.Value.Type);
        Assert.Equal(8, dateVisitField.Value.Length);

        var unfiltPosField = reader.FindField("Unfilt_Pos");
        Assert.NotNull(unfiltPosField);
        Assert.Equal(FieldType.Numeric, unfiltPosField.Value.Type);
        Assert.Equal(10, unfiltPosField.Value.Length);
        Assert.Equal(0, unfiltPosField.Value.DecimalCount);

        var maxPdopField = reader.FindField("Max_PDOP");
        Assert.NotNull(maxPdopField);
        Assert.Equal(FieldType.Numeric, maxPdopField.Value.Type);
        Assert.Equal(5, maxPdopField.Value.Length);
        Assert.Equal(1, maxPdopField.Value.DecimalCount);

        reader.Load();
        Assert.True(reader.Count > 0);

        var firstRecord = reader[0];
        var pointId = firstRecord.GetString("Point_ID")?.Trim();

        var type = firstRecord.GetString("Type")?.Trim();
        var shape = firstRecord.GetString("Shape")?.Trim();
        var circularD = firstRecord.GetString("Circular_D")?.Trim();

        Assert.Equal("0507121", pointId);
        Assert.Equal("CMP", type);
        Assert.Equal("circular", shape);
        Assert.Equal("12", circularD);
    }

    [Fact]
    public void DBase30_ShouldMatchExpectedMetadata()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);

        Assert.Equal(DbfVersion.VisualFoxPro, reader.Header.DbfVersion);
        Assert.Equal(34u, reader.Header.NumberOfRecords);
        Assert.Equal((ushort)4936, reader.Header.HeaderLength);
        Assert.Equal((ushort)3907, reader.Header.RecordLength);

        Assert.Equal(145, reader.Fields.Count);

        var accessNoField = reader.FindField("ACCESSNO");
        Assert.NotNull(accessNoField);
        Assert.Equal(FieldType.Character, accessNoField.Value.Type);
        Assert.Equal(15, accessNoField.Value.Length);

        var acqValueField = reader.FindField("ACQVALUE");
        Assert.NotNull(acqValueField);
        Assert.Equal(FieldType.Numeric, acqValueField.Value.Type);
        Assert.Equal(12, acqValueField.Value.Length);
        Assert.Equal(2, acqValueField.Value.DecimalCount);

        var appNotesField = reader.FindField("APPNOTES");
        Assert.NotNull(appNotesField);
        Assert.Equal(FieldType.Memo, appNotesField.Value.Type);
        Assert.Equal(4, appNotesField.Value.Length);

        var catDateField = reader.FindField("CATDATE");
        Assert.NotNull(catDateField);
        Assert.Equal(FieldType.Date, catDateField.Value.Type);
        Assert.Equal(8, catDateField.Value.Length);

        var flagDateField = reader.FindField("FLAGDATE");
        Assert.NotNull(flagDateField);
        Assert.Equal(FieldType.Timestamp, flagDateField.Value.Type);
        Assert.Equal(8, flagDateField.Value.Length);

        var earlyDateField = reader.FindField("EARLYDATE");
        Assert.NotNull(earlyDateField);
        Assert.Equal(FieldType.Numeric, earlyDateField.Value.Type);
        Assert.Equal(4, earlyDateField.Value.Length);
        Assert.Equal(0, earlyDateField.Value.DecimalCount);

        reader.Load();
        Assert.Equal(34, reader.Count);

        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        Assert.True(memoFields.Count > 0);
    }

    [Fact]
    public void DBase83_ShouldMatchExpectedMetadata()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        using var reader = DbfReader.Create(filePath);

        Assert.Equal(DbfVersion.DBase3PlusWithMemo, reader.Header.DbfVersion);
        Assert.Equal(67u, reader.Header.NumberOfRecords);
        Assert.Equal((ushort)513, reader.Header.HeaderLength);
        Assert.Equal((ushort)805, reader.Header.RecordLength);

        Assert.Equal(15, reader.Fields.Count);

        var idField = reader.FindField("ID");
        Assert.NotNull(idField);
        Assert.Equal(FieldType.Numeric, idField.Value.Type);
        Assert.Equal(19, idField.Value.Length);
        Assert.Equal(0, idField.Value.DecimalCount);

        var catCountField = reader.FindField("CATCOUNT");
        Assert.NotNull(catCountField);
        Assert.Equal(FieldType.Numeric, catCountField.Value.Type);
        Assert.Equal(19, catCountField.Value.Length);
        Assert.Equal(0, catCountField.Value.DecimalCount);

        var codeField = reader.FindField("CODE");
        Assert.NotNull(codeField);
        Assert.Equal(FieldType.Character, codeField.Value.Type);
        Assert.Equal(50, codeField.Value.Length);

        var nameField = reader.FindField("NAME");
        Assert.NotNull(nameField);
        Assert.Equal(FieldType.Character, nameField.Value.Type);
        Assert.Equal(100, nameField.Value.Length);

        var thumbnailField = reader.FindField("THUMBNAIL");
        Assert.NotNull(thumbnailField);
        Assert.Equal(FieldType.Character, thumbnailField.Value.Type);
        Assert.Equal(254, thumbnailField.Value.Length);

        var imageField = reader.FindField("IMAGE");
        Assert.NotNull(imageField);
        Assert.Equal(FieldType.Character, imageField.Value.Type);
        Assert.Equal(254, imageField.Value.Length);

        reader.Load();
        Assert.Equal(67, reader.Count);
    }

    [Fact]
    public void Cp1251_ShouldMatchExpectedMetadata()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.Cp1251))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.Cp1251);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
        using var reader = DbfReader.Create(filePath, options);

        Assert.Equal(DbfVersion.VisualFoxPro, reader.Header.DbfVersion);
        Assert.Equal(4u, reader.Header.NumberOfRecords);
        Assert.Equal((ushort)360, reader.Header.HeaderLength);
        Assert.Equal((ushort)105, reader.Header.RecordLength);

        Assert.Equal(2, reader.Fields.Count);

        var rnField = reader.FindField("RN");
        Assert.NotNull(rnField);
        Assert.Equal(FieldType.Numeric, rnField.Value.Type);
        Assert.Equal(4, rnField.Value.Length);
        Assert.Equal(0, rnField.Value.DecimalCount);

        var nameField = reader.FindField("NAME");
        Assert.NotNull(nameField);
        Assert.Equal(FieldType.Character, nameField.Value.Type);
        Assert.Equal(100, nameField.Value.Length);

        reader.Load();
        Assert.Equal(4, reader.Count);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People, DbfVersion.DBase3Plus, 3, 97, 25)]
    [InlineData(TestHelper.TestFiles.DBase03, DbfVersion.DBase3Plus, 14, 1025, 590)]
    [InlineData(TestHelper.TestFiles.DBase30, DbfVersion.VisualFoxPro, 34, 4936, 3907)]
    [InlineData(TestHelper.TestFiles.DBase83, DbfVersion.DBase3PlusWithMemo, 67, 513, 805)]
    [InlineData(TestHelper.TestFiles.Cp1251, DbfVersion.VisualFoxPro, 4, 360, 105)]
    public void Headers_ShouldMatchExpectedValues(
        string fileName,
        DbfVersion expectedVersion,
        int expectedRecords,
        int expectedHeaderLength,
        int expectedRecordLength
    )
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
        using var reader = DbfReader.Create(filePath, options);

        Assert.Equal(expectedVersion, reader.Header.DbfVersion);
        Assert.Equal((uint)expectedRecords, reader.Header.NumberOfRecords);
        Assert.Equal((ushort)expectedHeaderLength, reader.Header.HeaderLength);
        Assert.Equal((ushort)expectedRecordLength, reader.Header.RecordLength);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People, 2)]
    [InlineData(TestHelper.TestFiles.DBase03, 31)]
    [InlineData(TestHelper.TestFiles.DBase30, 145)]
    [InlineData(TestHelper.TestFiles.DBase83, 15)]
    [InlineData(TestHelper.TestFiles.Cp1251, 2)]
    public void FieldCounts_ShouldMatchExpected(string fileName, int expectedFieldCount)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
        using var reader = DbfReader.Create(filePath, options);

        Assert.Equal(expectedFieldCount, reader.Fields.Count);
        Assert.Equal(expectedFieldCount, reader.FieldNames.Count);
    }

    [Fact]
    public void FieldTypes_ShouldBeCorrectlyDetected()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);

        // Character fields
        var characterFields = reader.Fields.Where(f => f.Type == FieldType.Character).ToList();
        Assert.True(characterFields.Count > 0);

        foreach (var field in characterFields.Take(5))
        {
            Assert.True(field.Length > 0);
            Assert.Equal(0, field.DecimalCount);
        }

        // Numeric fields
        var numericFields = reader.Fields.Where(f => f.Type == FieldType.Numeric).ToList();
        Assert.True(numericFields.Count > 0);

        foreach (var field in numericFields.Take(5))
        {
            Assert.True(field.Length > 0);
            Assert.True(field.DecimalCount >= 0);
        }

        // Date fields
        var dateFields = reader.Fields.Where(f => f.Type == FieldType.Date).ToList();
        Assert.True(dateFields.Count > 0);

        foreach (var field in dateFields)
        {
            Assert.Equal(8, field.Length);
            Assert.Equal(0, field.DecimalCount);
        }

        // Memo fields
        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        Assert.True(memoFields.Count > 0);

        foreach (var field in memoFields.Take(5))
        {
            Assert.Equal(4, field.Length); // Memo fields store block indices
            Assert.Equal(0, field.DecimalCount);
        }

        // Timestamp fields
        var timestampFields = reader.Fields.Where(f => f.Type == FieldType.Timestamp).ToList();
        if (timestampFields.Count > 0)
        {
            foreach (var field in timestampFields)
            {
                Assert.Equal(8, field.Length);
                Assert.Equal(0, field.DecimalCount);
            }
        }
    }

    [Fact]
    public void RecordCounts_ShouldBeAccessibleBeforeLoad()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        // RecordCount should be accessible without loading
        Assert.Equal(3, reader.RecordCount);
        Assert.False(reader.IsLoaded);

        // After loading, both should be available
        reader.Load();
        Assert.True(reader.IsLoaded);
        Assert.Equal(2, reader.Count); // Active records only
        Assert.Equal(3, reader.RecordCount); // Header count includes deleted
    }

    [Fact]
    public void Statistics_ShouldReflectMetadata()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

        var stats = reader.GetStatistics();

        Assert.Equal("dbase_03", stats.TableName);
        Assert.Equal(14, stats.TotalRecords);
        Assert.Equal(31, stats.FieldCount);
        Assert.Equal(590, stats.RecordLength);
        Assert.Equal(1025, stats.HeaderLength);
        Assert.NotNull(stats.Encoding);
        Assert.False(stats.IsLoaded);

        reader.Load();
        var loadedStats = reader.GetStatistics();
        Assert.True(loadedStats.IsLoaded);
        Assert.Equal(14, loadedStats.ActiveRecords);
    }

    [Fact]
    public void NumericFields_ShouldHandleDecimalPlaces()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

        // Max_PDOP has 1 decimal place
        var maxPdopField = reader.FindField("Max_PDOP");
        Assert.NotNull(maxPdopField);
        Assert.Equal(1, maxPdopField.Value.DecimalCount);

        // GPS_Second has 3 decimal places
        var gpsSecondField = reader.FindField("GPS_Second");
        Assert.NotNull(gpsSecondField);
        Assert.Equal(3, gpsSecondField.Value.DecimalCount);

        // Std_Dev has 6 decimal places
        var stdDevField = reader.FindField("Std_Dev");
        Assert.NotNull(stdDevField);
        Assert.Equal(6, stdDevField.Value.DecimalCount);

        // Unfilt_Pos has 0 decimal places
        var unfiltPosField = reader.FindField("Unfilt_Pos");
        Assert.NotNull(unfiltPosField);
        Assert.Equal(0, unfiltPosField.Value.DecimalCount);
    }

    [Fact]
    public void FieldLengths_ShouldMatchMetadata()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

        // Test various field lengths from metadata
        var validations = new Dictionary<string, int>
        {
            { "Point_ID", 12 },
            { "Type", 20 },
            { "Shape", 20 },
            { "Non_circul", 60 },
            { "Comments", 60 },
            { "Date_Visit", 8 },
            { "Time", 10 },
            { "Max_PDOP", 5 },
            { "GPS_Week", 6 },
            { "GPS_Second", 12 },
            { "GPS_Height", 16 },
            { "Unfilt_Pos", 10 },
        };

        foreach (var validation in validations)
        {
            var field = reader.FindField(validation.Key);
            Assert.NotNull(field);
            Assert.Equal(validation.Value, field.Value.Length);
        }
    }

    [Fact]
    public void AllTestFiles_ShouldHaveConsistentFieldProperties()
    {
        var testFiles = new[]
        {
            TestHelper.TestFiles.People,
            TestHelper.TestFiles.DBase03,
            TestHelper.TestFiles.DBase30,
            TestHelper.TestFiles.DBase83,
            TestHelper.TestFiles.Cp1251,
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

            // All files should have consistent field properties
            foreach (var field in reader.Fields)
            {
                Assert.False(string.IsNullOrEmpty(field.Name));
                Assert.True(field.Length > 0);
                Assert.True(field.ActualLength > 0);
                Assert.True(field.DecimalCount >= 0);
                Assert.NotEqual(FieldType.Character, FieldType.Numeric); // Sanity check

                // Field type specific validations
                switch (field.Type)
                {
                    case FieldType.Date:
                        Assert.Equal(8, field.Length);
                        Assert.Equal(0, field.DecimalCount);
                        break;
                    case FieldType.Memo:
                        Assert.True(
                            field.Length >= 4,
                            $"Memo field '{field.Name}' has unexpected length {field.Length}, expected >= 4"
                        );
                        Assert.Equal(0, field.DecimalCount);
                        break;
                    case FieldType.Timestamp:
                        Assert.Equal(8, field.Length);
                        Assert.Equal(0, field.DecimalCount);
                        break;
                    case FieldType.Character:
                        Assert.Equal(0, field.DecimalCount);
                        break;
                }
            }
        }
    }
}
