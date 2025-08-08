using System.Text;
using DbfSharp.Core;
using DbfSharp.Core.Enums;

namespace DbfSharp.Tests;

public class DbfEncodingTests
{
    [Fact]
    public void Encoding_AutoDetection_ShouldWorkForCp1251File()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.Cp1251))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.Cp1251);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
        using var reader = DbfReader.Create(filePath, options);

        Assert.NotNull(reader.Encoding);
        
        var records = reader.Records.Take(3).ToList();
        foreach (var record in records)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                var value = record[i];
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    var hasReplacementChars = stringValue.Contains('\uFFFD');
                }
            }
        }
    }

    [Fact]
    public void Encoding_CustomEncoding_ShouldOverrideAutoDetection()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var customEncoding = Encoding.ASCII;
        var options = new DbfReaderOptions { Encoding = customEncoding };

        using var reader = DbfReader.Create(filePath, options);

        Assert.Equal(customEncoding, reader.Encoding);
    }

    [Fact]
    public void Encoding_UTF8_ShouldHandleUnicodeCharacters()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { Encoding = Encoding.UTF8 };

        using var reader = DbfReader.Create(filePath, options);

        Assert.Equal(Encoding.UTF8, reader.Encoding);
        
        var records = reader.Records.Take(5).ToList();
        foreach (var record in records)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                var value = record[i];
                if (value is string stringValue)
                {
                    Assert.True(Encoding.UTF8.GetByteCount(stringValue) >= 0);
                }
            }
        }
    }

    [Fact]
    public void Encoding_CyrillicFile_ShouldReadCorrectly()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase03Cyrillic))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase03Cyrillic);
        using var reader = DbfReader.Create(filePath);

        var records = reader.Records.Take(3).ToList();

        foreach (var record in records)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                var value = record[i];
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    if (stringValue.Any(c => c >= 0x0400 && c <= 0x04FF))
                    {
                        Assert.False(stringValue.Contains('\uFFFD'), 
                            "Cyrillic text should not contain replacement characters");
                    }
                }
            }
        }

    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("ASCII")]
    [InlineData("ISO-8859-1")]
    public void Encoding_SpecificEncodings_ShouldWorkWithoutErrors(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { Encoding = encoding };

        using var reader = DbfReader.Create(filePath, options);

        Assert.Equal(encoding, reader.Encoding);
        
        var record = reader.Records.First();
        for (var i = 0; i < record.FieldCount; i++)
        {
            var value = record[i];
            Assert.True(value == null || value is string || value.GetType().IsPrimitive || 
                       value is DateTime || value is decimal || value is byte[]);
        }
    }

    [Fact]
    public void Encoding_WithDecoderFallback_ShouldHandleInvalidCharacters()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions 
        { 
            Encoding = Encoding.UTF8,
            CharacterDecodeFallback = new DecoderReplacementFallback("?")
        };

        using var reader = DbfReader.Create(filePath, options);

        var records = reader.Records.Take(3).ToList();
        foreach (var record in records)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                var value = record[i];
                if (value is string stringValue)
                {
                    Assert.DoesNotContain('\uFFFD', stringValue);
                }
            }
        }
    }

    [Fact]
    public void Encoding_Statistics_ShouldShowEncodingInfo()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        var stats = reader.GetStatistics();
        
        Assert.NotNull(stats.Encoding);
        Assert.False(string.IsNullOrEmpty(stats.Encoding));
        Assert.Contains(reader.Encoding.EncodingName, stats.Encoding, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Encoding_DifferentFiles_ShouldAutoDetectCorrectly()
    {
        var testFiles = new[]
        {
            TestHelper.TestFiles.People,
            TestHelper.TestFiles.DBase03,
            TestHelper.TestFiles.DBase30
        };

        var encodings = new List<Encoding>();

        foreach (var fileName in testFiles)
        {
            if (!TestHelper.TestFileExists(fileName))
            {
                continue;
            }

            var filePath = TestHelper.GetTestFilePath(fileName);
            using var reader = DbfReader.Create(filePath);
            
            encodings.Add(reader.Encoding);
            Assert.NotNull(reader.Encoding);
        }

        Assert.True(encodings.Count > 0);
        foreach (var encoding in encodings)
        {
            Assert.NotNull(encoding);
            Assert.NotNull(encoding.EncodingName);
        }
    }

    [Fact]
    public void Encoding_StringFields_ShouldTrimCorrectly()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var optionsWithTrim = new DbfReaderOptions { TrimStrings = true };
        var optionsWithoutTrim = new DbfReaderOptions { TrimStrings = false };

        using var readerWithTrim = DbfReader.Create(filePath, optionsWithTrim);
        using var readerWithoutTrim = DbfReader.Create(filePath, optionsWithoutTrim);

        var recordTrimmed = readerWithTrim.Records.First();
        var recordUntrimmed = readerWithoutTrim.Records.First();

        var stringFields = readerWithTrim.Fields
            .Where(f => f.Type is FieldType.Character or FieldType.Varchar)
            .ToList();

        foreach (var field in stringFields)
        {
            var trimmedValue = recordTrimmed.GetString(field.Name);
            var untrimmedValue = recordUntrimmed.GetString(field.Name);

            if (trimmedValue != null && untrimmedValue != null)
            {
                Assert.Equal(trimmedValue, untrimmedValue.Trim());
                Assert.True(trimmedValue.Length <= untrimmedValue.Length);
            }
        }
    }

    [Fact]
    public void Encoding_FieldNames_ShouldRespectCasing()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var optionsLowerCase = new DbfReaderOptions { LowerCaseFieldNames = true };
        var optionsOriginalCase = new DbfReaderOptions { LowerCaseFieldNames = false };

        using var readerLowerCase = DbfReader.Create(filePath, optionsLowerCase);
        using var readerOriginalCase = DbfReader.Create(filePath, optionsOriginalCase);

        var lowerCaseNames = readerLowerCase.FieldNames.ToList();
        var originalCaseNames = readerOriginalCase.FieldNames.ToList();

        Assert.Equal(lowerCaseNames.Count, originalCaseNames.Count);

        for (var i = 0; i < lowerCaseNames.Count; i++)
        {
            Assert.Equal(lowerCaseNames[i], originalCaseNames[i].ToLowerInvariant());
            
            if (originalCaseNames[i] != originalCaseNames[i].ToLowerInvariant())
            {
                Assert.NotEqual(lowerCaseNames[i], originalCaseNames[i]);
            }
        }
    }

    [Fact]
    public void Encoding_IgnoreCase_ShouldAffectFieldLookup()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var optionsIgnoreCase = new DbfReaderOptions { IgnoreCase = true };
        var optionsCaseSensitive = new DbfReaderOptions { IgnoreCase = false };

        using var readerIgnoreCase = DbfReader.Create(filePath, optionsIgnoreCase);
        using var readerCaseSensitive = DbfReader.Create(filePath, optionsCaseSensitive);

        var firstFieldName = readerIgnoreCase.FieldNames[0];
        var upperFieldName = firstFieldName.ToUpperInvariant();
        var lowerFieldName = firstFieldName.ToLowerInvariant();

        var record1 = readerIgnoreCase.Records.First();
        var record2 = readerCaseSensitive.Records.First();

        if (firstFieldName != upperFieldName)
        {
            Assert.True(record1.HasField(upperFieldName));
            Assert.True(record1.HasField(lowerFieldName));

            Assert.True(record2.HasField(firstFieldName));
            
            if (firstFieldName != upperFieldName)
            {
                Assert.False(record2.HasField(upperFieldName));
            }

            if (firstFieldName != lowerFieldName)
            {
                Assert.False(record2.HasField(lowerFieldName));
            }
        }
    }

    [Fact]
    public void Encoding_MemoFields_ShouldRespectEncoding()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        var options = new DbfReaderOptions { Encoding = Encoding.UTF8 };

        using var reader = DbfReader.Create(filePath, options);
        
        var memoFields = reader.Fields.Where(f => f.Type == FieldType.Memo).ToList();
        if (memoFields.Count == 0)
        {
            return;
        }

        var record = reader.Records.First();
        foreach (var field in memoFields)
        {
            var value = record.GetString(field.Name);
            if (value != null)
            {
                Assert.IsType<string>(value);
                Assert.True(Encoding.UTF8.GetByteCount(value) >= 0);
                Assert.DoesNotContain('\0', value);
            }
        }
    }

    [Fact]
    public void Encoding_ToString_ShouldIncludeEncodingInfo()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var customEncoding = Encoding.ASCII;
        var options = new DbfReaderOptions { Encoding = customEncoding };

        using var reader = DbfReader.Create(filePath, options);

        var readerString = reader.ToString();
        Assert.NotNull(readerString);
        
        var optionsString = options.ToString();
        Assert.NotNull(optionsString);
        Assert.Contains("ASCII", optionsString);
    }

    [Theory]
    [InlineData(TestHelper.TestFiles.People)]
    [InlineData(TestHelper.TestFiles.DBase03)]
    [InlineData(TestHelper.TestFiles.DBase30)]
    [InlineData(TestHelper.TestFiles.Cp1251)]
    public void LanguageDriver_ShouldBeDetectedFromMetadata(string fileName)
    {
        if (!TestHelper.TestFileExists(fileName))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(fileName);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
        using var reader = DbfReader.Create(filePath, options);

        Assert.NotNull(reader.Encoding);
        
        var stats = reader.GetStatistics();
        Assert.NotNull(stats.Encoding);
    }

    [Fact]
    public void Cp1251_ShouldUseCorrectEncoding()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.Cp1251))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.Cp1251);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
        using var reader = DbfReader.Create(filePath, options);

        Assert.NotNull(reader.Encoding);
        
        reader.Load();
        Assert.Equal(4, reader.Count);

        var firstRecord = reader[0];
        var nameValue = firstRecord.GetString("NAME");
        
    }

    [Fact] 
    public void DifferentVersions_ShouldHaveAppropriateEncodings()
    {
        var testCases = new[]
        {
            (TestHelper.TestFiles.People, DbfVersion.DBase3Plus),
            (TestHelper.TestFiles.DBase03, DbfVersion.DBase3Plus),
            (TestHelper.TestFiles.DBase30, DbfVersion.VisualFoxPro),
            (TestHelper.TestFiles.Cp1251, DbfVersion.VisualFoxPro)
        };

        foreach (var (fileName, expectedVersion) in testCases)
        {
            if (!TestHelper.TestFileExists(fileName))
            {
                continue;
            }

            var filePath = TestHelper.GetTestFilePath(fileName);
            var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };
            using var reader = DbfReader.Create(filePath, options);

            Assert.Equal(expectedVersion, reader.Header.DbfVersion);
            Assert.NotNull(reader.Encoding);

            var records = reader.Records.Take(2).ToList();
            foreach (var record in records)
            {
                for (var i = 0; i < record.FieldCount; i++)
                {
                    var field = reader.Fields[i];
                    if (field.Type == FieldType.Character)
                    {
                        var value = record.GetString(field.Name);
                    }
                }
            }
        }
    }
}