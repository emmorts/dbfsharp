using DbfSharp.Core;
using DbfSharp.Core.Enums;

namespace DbfSharp.Tests;

public class DbfFieldTypeTests
{
    [Theory]
    [InlineData(FieldType.Character, typeof(string), true)]
    [InlineData(FieldType.Date, typeof(DateTime?), true)]
    [InlineData(FieldType.Float, typeof(float?), true)]
    [InlineData(FieldType.Integer, typeof(int), false)]
    [InlineData(FieldType.Logical, typeof(bool?), true)]
    [InlineData(FieldType.Memo, typeof(string), true)]
    [InlineData(FieldType.Numeric, typeof(decimal?), true)]
    [InlineData(FieldType.Double, typeof(double), false)]
    [InlineData(FieldType.Currency, typeof(decimal), false)]
    [InlineData(FieldType.Autoincrement, typeof(int), false)]
    public void GetExpectedNetType_ShouldReturnCorrectType(FieldType fieldType, Type expectedType, bool supportsNull)
    {
        var actualType = fieldType.GetExpectedNetType();
        var actualSupportsNull = fieldType.SupportsNull();

        Assert.Equal(expectedType, actualType);
        Assert.Equal(supportsNull, actualSupportsNull);
    }

    [Theory]
    [InlineData(FieldType.Memo, true)]
    [InlineData(FieldType.General, true)]
    [InlineData(FieldType.Picture, true)]
    [InlineData(FieldType.Binary, true)]
    [InlineData(FieldType.Character, false)]
    [InlineData(FieldType.Numeric, false)]
    [InlineData(FieldType.Date, false)]
    public void UsesMemoFile_ShouldReturnCorrectValue(FieldType fieldType, bool expectedUsesMemo)
    {
        var actualUsesMemo = fieldType.UsesMemoFile();
        Assert.Equal(expectedUsesMemo, actualUsesMemo);
    }

    [Theory]
    [InlineData('C', FieldType.Character)]
    [InlineData('D', FieldType.Date)]
    [InlineData('F', FieldType.Float)]
    [InlineData('I', FieldType.Integer)]
    [InlineData('L', FieldType.Logical)]
    [InlineData('M', FieldType.Memo)]
    [InlineData('N', FieldType.Numeric)]
    [InlineData('+', FieldType.Autoincrement)]
    [InlineData('@', FieldType.TimestampAlternate)]
    public void FromChar_ShouldReturnCorrectFieldType(char c, FieldType expectedFieldType)
    {
        var actualFieldType = FieldTypeExtensions.FromChar(c);
        Assert.Equal(expectedFieldType, actualFieldType);
    }

    [Fact]
    public void FromChar_InvalidCharacter_ShouldReturnNull()
    {
        var result = FieldTypeExtensions.FromChar('X');
        Assert.Null(result);
    }

    [Theory]
    [InlineData(FieldType.Character, "Character")]
    [InlineData(FieldType.Date, "Date")]
    [InlineData(FieldType.Memo, "Memo")]
    [InlineData(FieldType.General, "General/OLE")]
    [InlineData(FieldType.TimestampAlternate, "Timestamp (Alt)")]
    public void GetDescription_ShouldReturnCorrectDescription(FieldType fieldType, string expectedDescription)
    {
        var description = fieldType.GetDescription();
        Assert.Equal(expectedDescription, description);
    }

    [Fact]
    public void FieldType_CharacterField_ShouldReadCorrectly()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        var characterFields = reader.Fields.Where(f => f.Type == FieldType.Character).ToList();
        Assert.NotEmpty(characterFields);

        var record = reader.Records.First();
        foreach (var field in characterFields)
        {
            var value = record[field.Name];
            if (value != null)
            {
                Assert.IsType<string>(value);
            }
        }
    }

    [Fact]
    public void FieldType_NumericField_ShouldReadCorrectly()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);

        var numericFields = reader.Fields.Where(f => f.Type == FieldType.Numeric).ToList();
        if (numericFields.Count > 0)
        {
            var record = reader.Records.First();
            foreach (var field in numericFields)
            {
                var value = record[field.Name];
                if (value != null)
                {
                    Assert.True(value is int or decimal or double or float,
                        $"Expected numeric type but got {value.GetType()} for field {field.Name}");
                }
            }
        }
    }

    [Fact]
    public void FieldType_DateField_ShouldReadCorrectly()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        var dateFields = reader.Fields.Where(f => f.Type == FieldType.Date).ToList();
        if (dateFields.Count > 0)
        {
            var record = reader.Records.First();
            foreach (var field in dateFields)
            {
                var value = record[field.Name];
                if (value != null)
                {
                    Assert.IsType<DateTime>(value);
                }
            }
        }
    }

    [Fact]
    public void FieldType_LogicalField_ShouldReadCorrectly()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);

        var logicalFields = reader.Fields.Where(f => f.Type == FieldType.Logical).ToList();
        if (logicalFields.Count > 0)
        {
            var record = reader.Records.First();
            foreach (var field in logicalFields)
            {
                var value = record[field.Name];
                if (value != null)
                {
                    Assert.IsType<bool>(value);
                }
            }
        }
    }

    [Fact]
    public void FieldType_FloatField_ShouldReadCorrectly()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);

        var floatFields = reader.Fields.Where(f => f.Type == FieldType.Float).ToList();
        if (floatFields.Count > 0)
        {
            var record = reader.Records.First();
            foreach (var field in floatFields)
            {
                var value = record[field.Name];
                if (value != null)
                {
                    Assert.True(value is float or double,
                        $"Expected float/double type but got {value.GetType()} for field {field.Name}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.GetAllValidTestFilesTheoryData), MemberType = typeof(TestHelper))]
    public void FieldType_AllFieldTypes_ShouldParseWithoutErrors(string fileName)
    {
        var filePath = TestHelper.GetTestFilePath(fileName);
        using var reader = DbfReader.Create(filePath, new DbfReaderOptions
        {
            IgnoreMissingMemoFile = true,
            ValidateFields = true
        });

        foreach (var field in reader.Fields)
        {
            Assert.NotEqual(default(FieldType), field.Type);
            Assert.NotNull(field.Type.GetDescription());
            Assert.NotNull(field.Type.GetExpectedNetType());
        }

        var recordsProcessed = 0;
        foreach (var record in reader.Records)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                var field = reader.Fields[i];
                var value = record[i];

                if (value != null)
                {
                    var expectedType = field.Type.GetExpectedNetType();
                    if (expectedType != typeof(object))
                    {
                        if (field.Type == FieldType.Numeric)
                        {
                            Assert.True(value is int or decimal or double or float,
                                $"Field {field.Name} of type {field.Type} returned {value.GetType()} but expected numeric type");
                        }
                        else
                        {
                            Assert.True(expectedType.IsAssignableFrom(value.GetType()) ||
                                       (expectedType.IsGenericType &&
                                        expectedType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                                        Nullable.GetUnderlyingType(expectedType)!.IsAssignableFrom(value.GetType())),
                                $"Field {field.Name} of type {field.Type} returned {value.GetType()} but expected {expectedType}");
                        }
                    }
                }
            }

            recordsProcessed++;
            if (recordsProcessed > 10)
            {
                break;
            }
        }
    }

    [Fact]
    public void FieldProperties_ShouldBeConsistent()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        foreach (var field in reader.Fields)
        {
            Assert.False(string.IsNullOrEmpty(field.Name));
            Assert.True(field.Length > 0);
            Assert.True(field.ActualLength > 0);
            Assert.True(field.DecimalCount >= 0);
            Assert.NotEqual(default(FieldType), field.Type);

            if (field.Type == FieldType.Memo)
            {
                Assert.True(field.Length <= 10);
            }
        }
    }

    [Fact]
    public void People_ShouldHaveExpectedFields()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

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
    }

    [Fact]
    public void DBase03_ShouldHaveExpectedFieldStructure()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

        Assert.Equal(31, reader.Fields.Count);

        var pointIdField = reader.FindField("Point_ID");
        Assert.NotNull(pointIdField);
        Assert.Equal(FieldType.Character, pointIdField.Value.Type);
        Assert.Equal(12, pointIdField.Value.Length);
        Assert.Equal(0, pointIdField.Value.DecimalCount);

        var typeField = reader.FindField("Type");
        Assert.NotNull(typeField);
        Assert.Equal(FieldType.Character, typeField.Value.Type);
        Assert.Equal(20, typeField.Value.Length);

        var dateVisitField = reader.FindField("Date_Visit");
        Assert.NotNull(dateVisitField);
        Assert.Equal(FieldType.Date, dateVisitField.Value.Type);
        Assert.Equal(8, dateVisitField.Value.Length);

        var maxPdopField = reader.FindField("Max_PDOP");
        Assert.NotNull(maxPdopField);
        Assert.Equal(FieldType.Numeric, maxPdopField.Value.Type);
        Assert.Equal(5, maxPdopField.Value.Length);
        Assert.Equal(1, maxPdopField.Value.DecimalCount);

        var gpsSecondField = reader.FindField("GPS_Second");
        Assert.NotNull(gpsSecondField);
        Assert.Equal(FieldType.Numeric, gpsSecondField.Value.Type);
        Assert.Equal(12, gpsSecondField.Value.Length);
        Assert.Equal(3, gpsSecondField.Value.DecimalCount);

        var unfiltPosField = reader.FindField("Unfilt_Pos");
        Assert.NotNull(unfiltPosField);
        Assert.Equal(FieldType.Numeric, unfiltPosField.Value.Type);
        Assert.Equal(10, unfiltPosField.Value.Length);
        Assert.Equal(0, unfiltPosField.Value.DecimalCount);
    }

    [Fact]
    public async Task DBase30_ShouldHaveExpectedFieldStructure()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        await using var reader = await DbfReader.CreateAsync(filePath);

        Assert.Equal(145, reader.Fields.Count);

        var objectIdField = reader.FindField("OBJECTID");
        Assert.NotNull(objectIdField);
        Assert.Equal("OBJECTID", objectIdField.Value.Name);

        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        Assert.True(memoFields.Count > 0);

        foreach (var memoField in memoFields.Take(5))
        {
            Assert.Equal(4, memoField.Length);
            Assert.Equal(0, memoField.DecimalCount);
        }

        var accessNoField = reader.FindField("ACCESSNO");
        Assert.NotNull(accessNoField);
        Assert.Equal(FieldType.Character, accessNoField.Value.Type);
        Assert.Equal(15, accessNoField.Value.Length);

        var acqValueField = reader.FindField("ACQVALUE");
        Assert.NotNull(acqValueField);
        Assert.Equal(FieldType.Numeric, acqValueField.Value.Type);
        Assert.Equal(12, acqValueField.Value.Length);
        Assert.Equal(2, acqValueField.Value.DecimalCount);

        var flagDateField = reader.FindField("FLAGDATE");
        Assert.NotNull(flagDateField);
        Assert.Equal(FieldType.Timestamp, flagDateField.Value.Type);
        Assert.Equal(8, flagDateField.Value.Length);

        var earlyDateField = reader.FindField("EARLYDATE");
        Assert.NotNull(earlyDateField);
        Assert.Equal(FieldType.Numeric, earlyDateField.Value.Type);
        Assert.Equal(4, earlyDateField.Value.Length);
        Assert.Equal(0, earlyDateField.Value.DecimalCount);
    }

    [Fact]
    public void People_ShouldHaveExpectedSampleData()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { SkipDeletedRecords = false };
        using var reader = DbfReader.Create(filePath, options);

        var allRecords = reader.Records.ToList();
        Assert.Equal(3, allRecords.Count);

        var record1 = allRecords[0];
        var name1 = record1.GetString("NAME")?.Trim();
        var birthdate1 = record1.GetDateTime("BIRTHDATE");

        Assert.Equal("Alice", name1);
        Assert.Equal(new DateTime(1987, 3, 1), birthdate1);

        var record2 = allRecords[1];
        var name2 = record2.GetString("NAME")?.Trim();
        var birthdate2 = record2.GetDateTime("BIRTHDATE");

        Assert.Equal("Bob", name2);
        Assert.Equal(new DateTime(1980, 11, 12), birthdate2);

        var record3 = allRecords[2];
        var name3 = record3.GetString("NAME")?.Trim();
        var birthdate3 = record3.GetDateTime("BIRTHDATE");

        Assert.Equal("Deleted Guy", name3);
        Assert.Equal(new DateTime(1979, 12, 22), birthdate3);
    }

    [Fact]
    public void DBase03_ShouldHaveExpectedSampleData()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03);
        using var reader = DbfReader.Create(filePath);

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

        for (var i = 0; i < Math.Min(3, reader.Count); i++)
        {
            var record = reader[i];
            var recordPointId = record.GetString("Point_ID")?.Trim();
            var recordType = record.GetString("Type")?.Trim();
            var recordShape = record.GetString("Shape")?.Trim();

            Assert.False(string.IsNullOrEmpty(recordPointId));
            Assert.Equal("CMP", recordType);
            Assert.Equal("circular", recordShape);
        }
    }

    [Fact]
    public void FieldAccess_ByIndex_ShouldMatchByName()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();

        for (var i = 0; i < record.FieldCount; i++)
        {
            var fieldName = reader.Fields[i].Name;
            var valueByIndex = record[i];
            var valueByName = record[fieldName];

            Assert.Equal(valueByIndex, valueByName);
        }
    }

    [Fact]
    public void GenericFieldAccess_ShouldWorkForAllTypes()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase30);
        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();

        foreach (var field in reader.Fields)
        {
            switch (field.Type)
            {
                case FieldType.Character:
                case FieldType.Varchar:
                case FieldType.Memo:
                    var stringVal = record.GetString(field.Name);
                    if (stringVal != null)
                    {
                        Assert.IsType<string>(stringVal);
                    }

                    break;

                case FieldType.Date:
                case FieldType.Timestamp:
                case FieldType.TimestampAlternate:
                    var dateVal = record.GetDateTime(field.Name);
                    if (dateVal != null)
                    {
                        Assert.IsType<DateTime>(dateVal);
                    }

                    break;

                case FieldType.Logical:
                    var boolVal = record.GetBoolean(field.Name);
                    if (boolVal != null)
                    {
                        Assert.IsType<bool>(boolVal);
                    }

                    break;

                case FieldType.Integer:
                case FieldType.Autoincrement:
                    var intVal = record.GetInt32(field.Name);
                    if (intVal != null)
                    {
                        Assert.IsType<int>(intVal);
                    }

                    break;

                case FieldType.Float:
                case FieldType.Double:
                case FieldType.Numeric:
                    var numVal = record[field.Name];
                    if (numVal != null)
                    {
                        Assert.True(numVal is int or decimal or double or float);
                    }

                    break;
            }
        }
    }
}
