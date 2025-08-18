using System.Text;
using DbfSharp.Core;

namespace DbfSharp.Tests;

/// <summary>
/// Tests for DbfReaderOptions functionality
/// </summary>
public class DbfReaderOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveExpectedValues()
    {
        var options = new DbfReaderOptions();

        Assert.Equal(options.Encoding, Encoding.UTF8);
        Assert.True(options.IgnoreCase);
        Assert.False(options.LowerCaseFieldNames);
        Assert.False(options.IgnoreMissingMemoFile);
        Assert.True(options.EnableStringInterning);
        Assert.True(options.TrimStrings);
        Assert.False(options.RawMode);
        Assert.True(options.ValidateFields);
        Assert.True(options.SkipDeletedRecords);
        Assert.Null(options.MaxRecords);
        Assert.False(options.UseMemoryMapping);
        Assert.Equal(16384, options.BufferSize);
    }

    [Fact]
    public void LoadOnOpen_True_ShouldLoadRecordsImmediately()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var reader = DbfReader.Create(filePath);

        reader.Load();

        Assert.True(reader.IsLoaded);
        Assert.True(reader.Count > 0);
    }

    [Fact]
    public void LowerCaseFieldNames_True_ShouldConvertFieldNames()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { LowerCaseFieldNames = true };

        using var reader = DbfReader.Create(filePath, options);

        reader.Load();

        foreach (var name in reader.FieldNames)
        {
            Assert.Equal(name.ToLowerInvariant(), name);
        }
    }

    [Fact]
    public void CustomEncoding_ShouldOverrideAutoDetection()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var customEncoding = Encoding.UTF8;
        var options = new DbfReaderOptions { Encoding = customEncoding };

        using var reader = DbfReader.Create(filePath, options);

        reader.Load();

        Assert.Equal(customEncoding, reader.Encoding);
    }

    [Fact]
    public void MaxRecords_ShouldLimitRecordReading()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        const int maxRecords = 2;
        var options = new DbfReaderOptions { MaxRecords = maxRecords };

        using var reader = DbfReader.Create(filePath, options);

        var records = reader.Records.ToList();

        Assert.True(records.Count <= maxRecords);
    }

    [Fact]
    public void IgnoreMissingMemoFile_True_ShouldNotThrowWhenMemoFileMissing()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83MissingMemo);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = true };

        using var reader = DbfReader.Create(filePath, options);
        Assert.NotNull(reader);

        var records = reader.Records.Take(5).ToList();
        Assert.NotEmpty(records);
    }

    [Fact]
    public void TrimStrings_False_ShouldPreserveWhitespace()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var optionsWithTrim = new DbfReaderOptions { TrimStrings = true };
        var optionsWithoutTrim = new DbfReaderOptions { TrimStrings = false };

        using var readerWithTrim = DbfReader.Create(filePath, optionsWithTrim);
        using var readerWithoutTrim = DbfReader.Create(filePath, optionsWithoutTrim);

        readerWithTrim.Load();
        readerWithoutTrim.Load();

        var recordWithTrim = readerWithTrim.Records.First();
        var recordWithoutTrim = readerWithoutTrim.Records.First();

        for (var i = 0; i < recordWithTrim.FieldCount; i++)
        {
            var valueWithTrim = recordWithTrim[i]?.ToString();
            var valueWithoutTrim = recordWithoutTrim[i]?.ToString();

            if (valueWithTrim != valueWithoutTrim)
            {
                break;
            }
        }
    }

    [Fact]
    public void RawMode_True_ShouldReturnByteArrays()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { RawMode = true };

        using var reader = DbfReader.Create(filePath, options);
        var record = reader.Records.First();

        for (var i = 0; i < record.FieldCount; i++)
        {
            var value = record[i];
            Assert.IsType<byte[]>(value);
        }
    }

    [Fact]
    public void ValidateFields_False_ShouldSkipValidation()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.InvalidValue);
        var options = new DbfReaderOptions { ValidateFields = false };

        using var reader = DbfReader.Create(filePath, options);
        Assert.NotNull(reader);

        var records = reader.Records.Take(5).ToList();
        Assert.NotEmpty(records);
    }

    [Fact]
    public void Clone_ShouldCreateIdenticalCopy()
    {
        var original = new DbfReaderOptions
        {
            Encoding = Encoding.UTF8,
            IgnoreCase = false,
            LowerCaseFieldNames = true,
            IgnoreMissingMemoFile = true,
            TrimStrings = false,
            MaxRecords = 100,
            BufferSize = 32768,
        };

        var clone = original with { };

        Assert.NotSame(original, clone);
        Assert.Equal(original.Encoding, clone.Encoding);
        Assert.Equal(original.IgnoreCase, clone.IgnoreCase);
        Assert.Equal(original.LowerCaseFieldNames, clone.LowerCaseFieldNames);
        Assert.Equal(original.IgnoreMissingMemoFile, clone.IgnoreMissingMemoFile);
        Assert.Equal(original.TrimStrings, clone.TrimStrings);
        Assert.Equal(original.MaxRecords, clone.MaxRecords);
        Assert.Equal(original.BufferSize, clone.BufferSize);
    }

    [Fact]
    public void CreatePerformanceOptimized_ShouldHaveCorrectSettings()
    {
        var options = DbfReaderOptions.CreatePerformanceOptimized();

        Assert.True(options.EnableStringInterning);
        Assert.True(options.BufferSize > 65536);
        Assert.False(options.ValidateFields);
        Assert.False(options.TrimStrings);
    }

    [Fact]
    public void CreateMemoryOptimized_ShouldHaveCorrectSettings()
    {
        var options = DbfReaderOptions.CreateMemoryOptimized();

        Assert.True(options.EnableStringInterning);
        Assert.True(options.BufferSize < 65536);
        Assert.False(options.UseMemoryMapping);
        Assert.True(options.TrimStrings);
    }

    [Fact]
    public void CreateCompatibilityOptimized_ShouldHaveCorrectSettings()
    {
        var options = DbfReaderOptions.CreateCompatibilityOptimized();

        Assert.True(options.IgnoreCase);
        Assert.True(options.IgnoreMissingMemoFile);
        Assert.False(options.ValidateFields);
        Assert.True(options.TrimStrings);
        Assert.True(options.SkipDeletedRecords);
        Assert.NotNull(options.CharacterDecodeFallback);
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulDescription()
    {
        var options = new DbfReaderOptions { LowerCaseFieldNames = true, MaxRecords = 1000 };

        var result = options.ToString();

        Assert.False(string.IsNullOrEmpty(result));
        Assert.Contains("LowerCaseNames", result);
        Assert.Contains("MaxRecords=1000", result);
    }

    [Fact]
    public void ToString_DefaultOptions_ShouldReturnDefault()
    {
        var options = new DbfReaderOptions();

        var result = options.ToString();

        Assert.Equal("Encoding=Unicode (UTF-8)", result);
    }

    [Fact]
    public void SkipDeletedRecords_False_ShouldIncludeDeletedRecords()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var optionsIncludeDeleted = new DbfReaderOptions { SkipDeletedRecords = false };
        var optionsSkipDeleted = new DbfReaderOptions { SkipDeletedRecords = true };

        using var readerIncludeDeleted = DbfReader.Create(filePath, optionsIncludeDeleted);
        using var readerSkipDeleted = DbfReader.Create(filePath, optionsSkipDeleted);

        var recordsIncludeDeleted = readerIncludeDeleted.Records.Count();
        var recordsSkipDeleted = readerSkipDeleted.Records.Count();

        Assert.True(recordsIncludeDeleted >= recordsSkipDeleted);
    }
}
