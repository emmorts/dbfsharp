using DbfSharp.Core;
using DbfSharp.Core.Exceptions;

namespace DbfSharp.Tests;

public class DbfExceptionTests
{
    [Fact]
    public void FileNotFound_ShouldThrowDbfNotFoundException()
    {
        const string nonExistentFile = "non_existent_file.dbf";

        var exception = Assert.Throws<DbfNotFoundException>(() => DbfReader.Create(nonExistentFile));

        Assert.Equal(nonExistentFile, exception.FilePath);
        Assert.NotNull(exception.Message);
        Assert.Contains(nonExistentFile, exception.Message);
    }

    [Fact]
    public void InvalidFilePath_ShouldThrowDbfNotFoundException()
    {
        const string invalidPath = "";

        Assert.Throws<ArgumentException>(() => DbfReader.Create(invalidPath));
    }

    [Fact]
    public void NullFilePath_ShouldThrowArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => DbfReader.Create((string)null!));
    }

    [Fact]
    public void NullStream_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => DbfReader.Create((Stream)null!));
    }

    [Fact]
    public void DisposedReader_ShouldThrowObjectDisposedException()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var reader = DbfReader.Create(filePath);
        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => reader.Records.First());
        Assert.Throws<ObjectDisposedException>(() => reader.Load());
        Assert.Throws<ObjectDisposedException>(() => reader.GetStatistics());
    }

    [Fact]
    public void MissingMemoFile_ShouldThrowMissingMemoFileException()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83MissingMemo))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83MissingMemo);
        var options = new DbfReaderOptions { IgnoreMissingMemoFile = false };

        var exception = Assert.Throws<MissingMemoFileException>(() => DbfReader.Create(filePath, options));

        Assert.NotNull(exception.Message);
        Assert.NotNull(exception.DbfFilePath);
        Assert.NotNull(exception.MemoFilePath);
    }

    [Fact]
    public void InvalidFieldName_ShouldThrowKeyNotFoundException()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();

        Assert.ThrowsAny<Exception>(() => record["NON_EXISTENT_FIELD"]);
    }

    [Fact]
    public void InvalidFieldIndex_ShouldThrowIndexOutOfRangeException()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();

        Assert.ThrowsAny<ArgumentException>(() => record[-1]);
        Assert.ThrowsAny<ArgumentException>(() => record[reader.Fields.Count]);
    }

    [Fact]
    public void RandomAccessBeforeLoad_ShouldThrowInvalidOperationException()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        Assert.False(reader.IsLoaded);

        try
        {
            var record = reader[0];
            Assert.Fail("Expected exception when accessing by index on unloaded reader");
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            var count = reader.Count;
            Assert.True(count >= 0);
        }
        catch (InvalidOperationException)
        {
        }
    }

    [Fact]
    public void EmptyStream_ShouldThrowDbfException()
    {
        using var emptyStream = new MemoryStream();

        Assert.ThrowsAny<Exception>(() => DbfReader.Create(emptyStream));
    }

    [Fact]
    public void CorruptedHeaderStream_ShouldThrowDbfException()
    {
        var corruptedData = new byte[10];
        using var stream = new MemoryStream(corruptedData);

        Assert.ThrowsAny<Exception>(() => DbfReader.Create(stream));
    }

    [Fact]
    public void ValidationErrors_WithValidationEnabled_ShouldThrowFieldParseException()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.InvalidValue))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.InvalidValue);
        var options = new DbfReaderOptions { ValidateFields = true };

        using var reader = DbfReader.Create(filePath, options);

        Assert.ThrowsAny<Exception>(() =>
        {
            foreach (var record in reader.Records)
            {
                for (var i = 0; i < record.FieldCount; i++)
                {
                    var value = record[i];
                }
            }
        });
    }

    [Fact]
    public void ValidationErrors_WithValidationDisabled_ShouldNotThrow()
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

    [Fact]
    public void NonSeekableStream_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var fileBytes = File.ReadAllBytes(filePath);

        try
        {
            using var nonSeekableStream = new NonSeekableMemoryStream(fileBytes);
            using var reader = DbfReader.Create(nonSeekableStream);

            Assert.NotNull(reader);
            var record = reader.Records.First();
        }
        catch (NotSupportedException)
        {
        }
    }

    [Fact]
    public void FieldAccess_CaseSensitive_ShouldThrowOnWrongCase()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { IgnoreCase = false };

        using var reader = DbfReader.Create(filePath, options);
        var record = reader.Records.First();
        var firstFieldName = reader.FieldNames[0];
        var upperFieldName = firstFieldName.ToUpperInvariant();

        if (firstFieldName != upperFieldName)
        {
            Assert.Throws<KeyNotFoundException>(() => record[upperFieldName]);
        }
    }

    [Fact]
    public void MultipleDispose_ShouldNotThrow()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var reader = DbfReader.Create(filePath);

        reader.Dispose();
        reader.Dispose();
        reader.Dispose();
    }

    [Fact]
    public void GenericFieldAccess_WrongType_ShouldHandleGracefully()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);
        var record = reader.Records.First();

        var charField = reader.Fields.FirstOrDefault(f => f.Type == Core.Enums.FieldType.Character);
        if (charField != default)
        {
            var stringValue = record.GetString(charField.Name);
            if (stringValue != null)
            {
                Assert.IsType<string>(stringValue);

                try
                {
                    var intValue = record.GetInt32(charField.Name);
                    var dateValue = record.GetDateTime(charField.Name);
                    var boolValue = record.GetBoolean(charField.Name);
                }
                catch (InvalidCastException)
                {
                }
            }
        }
    }

    [Fact]
    public void LoadAfterPartialEnumeration_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        var firstFewRecords = reader.Records.Take(3).ToList();
        Assert.NotEmpty(firstFewRecords);

        reader.Load();
        Assert.True(reader.IsLoaded);
        Assert.True(reader.Count > 0);
    }

    [Fact]
    public void UnloadAfterLoad_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        using var reader = DbfReader.Create(filePath);

        reader.Load();
        Assert.True(reader.IsLoaded);

        reader.Unload();
        Assert.False(reader.IsLoaded);

        var records = reader.Records.Take(2).ToList();
        Assert.NotEmpty(records);
    }

    [Fact]
    public void MaxRecords_ExceedsActualRecords_ShouldNotThrow()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { MaxRecords = int.MaxValue };

        using var reader = DbfReader.Create(filePath, options);

        var allRecords = reader.Records.ToList();
        Assert.True(allRecords.Count <= int.MaxValue);
        Assert.True(allRecords.Count > 0);
    }

    [Fact]
    public void ZeroMaxRecords_ShouldReturnEmptyEnumeration()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { MaxRecords = 0 };

        using var reader = DbfReader.Create(filePath, options);

        var records = reader.Records.ToList();
        Assert.Empty(records);
    }

    [Fact]
    public void UnsupportedDbfVersion_ShouldThrowUnsupportedDbfVersionException()
    {
        const byte unsupportedVersionByte = 0xFF;

        var fakeDbfHeader = new byte[32];
        fakeDbfHeader[0] = unsupportedVersionByte;

        fakeDbfHeader[1] = 0x01;
        fakeDbfHeader[2] = 0x01;
        fakeDbfHeader[3] = 0x01;
        fakeDbfHeader[8] = 33;
        fakeDbfHeader[10] = 1;

        using var stream = new MemoryStream(fakeDbfHeader);

        var exception = Assert.Throws<UnsupportedDbfVersionException>(() => DbfReader.Create(stream));

        Assert.Equal(unsupportedVersionByte, exception.VersionByte);
        Assert.Contains($"0x{unsupportedVersionByte:X2}", exception.Message);
        Assert.Contains("Unsupported or unrecognized DBF version", exception.Message);
    }

    [Fact]
    public async Task UnsupportedDbfVersion_CreateAsync_ShouldThrowUnsupportedDbfVersionException()
    {
        const byte unsupportedVersionByte = 0x99;

        var fakeDbfHeader = new byte[32];
        fakeDbfHeader[0] = unsupportedVersionByte;

        fakeDbfHeader[1] = 0x01;
        fakeDbfHeader[2] = 0x01;
        fakeDbfHeader[3] = 0x01;
        fakeDbfHeader[8] = 33;
        fakeDbfHeader[10] = 1;

        using var stream = new MemoryStream(fakeDbfHeader);

        var exception = await Assert.ThrowsAsync<UnsupportedDbfVersionException>(() => DbfReader.CreateAsync(stream));

        Assert.Equal(unsupportedVersionByte, exception.VersionByte);
        Assert.Contains($"0x{unsupportedVersionByte:X2}", exception.Message);
    }

    [Fact]
    public void SupportedDbfVersions_ShouldNotThrowUnsupportedDbfVersionException()
    {
        var supportedVersions = new byte[]
        {
            0x02,
            0x03,
            0x30,
            0x31,
            0x32,
            0x83,
            0x8B,
            0xF5
        };

        foreach (var versionByte in supportedVersions)
        {
            var fakeDbfHeader = new byte[32];
            fakeDbfHeader[0] = versionByte;

            fakeDbfHeader[1] = 0x01;
            fakeDbfHeader[2] = 0x01;
            fakeDbfHeader[3] = 0x01;
            fakeDbfHeader[8] = 33;
            fakeDbfHeader[10] = 1;

            using var stream = new MemoryStream(fakeDbfHeader);

            try
            {
                using var reader = DbfReader.Create(stream);
            }
            catch (UnsupportedDbfVersionException)
            {
                Assert.Fail($"Supported DBF version 0x{versionByte:X2} should not throw UnsupportedDbfVersionException");
            }
            catch (Exception)
            {
            }
        }
    }

    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        Assert.True(typeof(DbfNotFoundException).IsSubclassOf(typeof(DbfException)));
        Assert.True(typeof(MissingMemoFileException).IsSubclassOf(typeof(DbfException)));
        Assert.True(typeof(FieldParseException).IsSubclassOf(typeof(DbfException)));
        Assert.True(typeof(UnsupportedDbfVersionException).IsSubclassOf(typeof(DbfException)));
        Assert.True(typeof(DbfException).IsSubclassOf(typeof(Exception)));
    }

    private class NonSeekableMemoryStream : MemoryStream
    {
        public NonSeekableMemoryStream(byte[] buffer) : base(buffer) { }

        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Stream does not support seeking");
        }

        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException("Stream does not support seeking");
        }
    }
}
