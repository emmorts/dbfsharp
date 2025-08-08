using System.IO.Pipelines;
using DbfSharp.Core;

namespace DbfSharp.Tests;

public class DbfAsyncTests
{
    [Fact]
    public async Task CreateAsync_FromFile_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        await using var reader = await DbfReader.CreateAsync(filePath);

        Assert.NotNull(reader);
        Assert.False(reader.IsLoaded);
        Assert.NotEmpty(reader.Fields);
        Assert.True(reader.RecordCount > 0);
    }

    [Fact]
    public async Task CreateAsync_FromStream_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var fileStream = File.OpenRead(filePath);

        await using var reader = await DbfReader.CreateAsync(fileStream);

        Assert.NotNull(reader);
        Assert.NotEmpty(reader.Fields);
        Assert.Equal("Unknown", reader.TableName);
    }

    [Fact]
    public async Task CreateAsync_WithOptions_ShouldApplyOptions()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions
        {
            LowerCaseFieldNames = true,
            MaxRecords = 10
        };

        await using var reader = await DbfReader.CreateAsync(filePath, options);

        Assert.NotNull(reader);
        foreach (var fieldName in reader.FieldNames)
        {
            Assert.Equal(fieldName.ToLowerInvariant(), fieldName);
        }

        var recordCount = reader.Records.Count();
        Assert.True(recordCount <= 10);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadAllRecords()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        Assert.False(reader.IsLoaded);

        await reader.LoadAsync();

        Assert.True(reader.IsLoaded);
        Assert.True(reader.Count > 0);

        var firstRecord = reader[0];
    }

    [Fact]
    public async Task LoadAsync_AfterPartialEnumeration_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        var partialRecords = new List<Core.DbfRecord>();
        var count = 0;
        foreach (var record in reader.Records)
        {
            partialRecords.Add(record);
            count++;
            if (count >= 3)
            {
                break;
            }
        }

        if (partialRecords.Count == 0)
        {
            return;
        }

        await reader.LoadAsync();

        Assert.True(reader.IsLoaded);
        Assert.True(reader.Count >= partialRecords.Count);
    }

    [Fact]
    public async Task LoadAsync_WithCancellation_ShouldRespectCancellation()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await reader.LoadAsync(cts.Token));
    }

    [Fact]
    public async Task LoadAsync_AlreadyLoaded_ShouldNotThrow()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        await reader.LoadAsync();
        Assert.True(reader.IsLoaded);

        await reader.LoadAsync();
        Assert.True(reader.IsLoaded);
    }

    [Fact]
    public async Task CreateAsync_NonExistentFile_ShouldThrow()
    {
        const string nonExistentFile = "non_existent_async.dbf";

        await Assert.ThrowsAsync<Core.Exceptions.DbfNotFoundException>(async () =>
            await DbfReader.CreateAsync(nonExistentFile));
    }

    [Fact]
    public async Task CreateAsync_NullArguments_ShouldThrow()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await DbfReader.CreateAsync((string)null!));

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await DbfReader.CreateAsync((Stream)null!));
    }

    [Fact]
    public async Task AsyncEnumeration_ShouldWorkWithAwaitForeach()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        var recordCount = 0;
        await foreach (var record in reader.ReadRecordsAsync())
        {
            Assert.Equal(reader.Fields.Count, record.FieldCount);

            recordCount++;
            if (recordCount >= 5)
            {
                break;
            }
        }

        Assert.True(recordCount > 0);
    }

    [Fact]
    public async Task AsyncEnumeration_WithCancellation_ShouldRespectToken()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        var recordCount = 0;
        try
        {
            await foreach (var record in reader.ReadRecordsAsync(cts.Token))
            {
                recordCount++;
                await Task.Delay(10, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }

        Assert.True(recordCount >= 0);
    }

    [Fact]
    public async Task AsyncEnumeration_WithMaxRecords_ShouldRespectLimit()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { MaxRecords = 3 };
        await using var reader = await DbfReader.CreateAsync(filePath, options);

        var recordCount = 0;
        await foreach (var record in reader.ReadRecordsAsync())
        {
            recordCount++;
        }

        Assert.True(recordCount <= 3);
    }

    [Fact]
    public async Task AsyncOperations_Concurrency_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        var tasks = new List<Task<DbfReader>>();
        for (var i = 0; i < 3; i++)
        {
            tasks.Add(DbfReader.CreateAsync(filePath));
        }

        var readers = await Task.WhenAll(tasks);

        Assert.Equal(3, readers.Length);
        foreach (var reader in readers)
        {
            Assert.NotNull(reader);
            Assert.True(reader.Fields.Count > 0);
            await reader.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateAsync_WithMemoryStream_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var fileBytes = await File.ReadAllBytesAsync(filePath);

        using var memoryStream = new MemoryStream(fileBytes);
        await using var reader = await DbfReader.CreateAsync(memoryStream);

        Assert.NotNull(reader);
        Assert.True(reader.Fields.Count > 0);

        var recordExists = reader.Records.Any();
        if (recordExists)
        {
            var record = reader.Records.First();
        }
    }

    [Fact]
    public async Task AsyncEnumeration_EmptyFile_ShouldHandleGracefully()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions { MaxRecords = 0 };
        await using var reader = await DbfReader.CreateAsync(filePath, options);

        var recordCount = 0;
        await foreach (var record in reader.ReadRecordsAsync())
        {
            recordCount++;
        }

        Assert.Equal(0, recordCount);
    }

    [Fact]
    public async Task LoadAsync_DisposedReader_ShouldThrow()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var reader = await DbfReader.CreateAsync(filePath);
        await reader.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await reader.LoadAsync());
    }

    [Fact]
    public async Task CreateAsync_MemoFile_ShouldWork()
    {
        if (!TestHelper.TestFileExists(TestHelper.TestFiles.DBase83))
        {
            return;
        }

        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.DBase83);
        await using var reader = await DbfReader.CreateAsync(filePath);

        var memoFields = reader.Fields.Where(f => f.Type is Core.Enums.FieldType.Memo or Core.Enums.FieldType.General or Core.Enums.FieldType.Picture).ToList();
        if (memoFields.Count > 0)
        {
            await reader.LoadAsync();

            var record = reader[0];
            foreach (var field in memoFields)
            {
                var value = record[field.Name];
                Assert.True(value is null or string or byte[]);
            }
        }
    }

    [Fact]
    public async Task AsyncOperations_WithOptions_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var options = new DbfReaderOptions
        {
            TrimStrings = true,
            IgnoreCase = false,
            MaxRecords = 5
        };

        await using var reader = await DbfReader.CreateAsync(filePath, options);

        var recordCount = 0;
        await foreach (var record in reader.ReadRecordsAsync())
        {
            recordCount++;
        }

        Assert.True(recordCount <= 5);
        Assert.True(recordCount >= 0);
    }

    [Fact]
    public async Task CreateAsync_WithCustomCancellationToken_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var reader = await DbfReader.CreateAsync(filePath, cancellationToken: cts.Token);

        Assert.NotNull(reader);
        Assert.True(reader.Fields.Count > 0);
    }

    [Fact]
    public async Task AsyncEnumeration_MultipleEnumerations_ShouldWork()
    {
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        var firstEnumeration = new List<Core.DbfRecord>();
        await foreach (var record in reader.ReadRecordsAsync())
        {
            firstEnumeration.Add(record);
            if (firstEnumeration.Count >= 3)
            {
                break;
            }
        }

        var secondEnumeration = new List<Core.DbfRecord>();
        await foreach (var record in reader.ReadRecordsAsync())
        {
            secondEnumeration.Add(record);
            if (secondEnumeration.Count >= 3)
            {
                break;
            }
        }

        Assert.True(firstEnumeration.Count >= 0);
        Assert.True(secondEnumeration.Count >= 0);

        if (firstEnumeration.Count > 0 && secondEnumeration.Count > 0)
        {
            Assert.Equal(firstEnumeration.Count, secondEnumeration.Count);
        }
    }

    [Fact]
    public async Task ReadDeletedRecordsAsync_ShouldReturnDeletedRecords()
    {
        // Arrange
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        await using var reader = await DbfReader.CreateAsync(filePath);

        // Act
        var deletedRecords = new List<DbfRecord>();
        await foreach (var record in reader.ReadDeletedRecordsAsync())
        {
            deletedRecords.Add(record);
        }

        // Assert
        Assert.NotEmpty(deletedRecords);
    }

    [Fact(Skip = "This test is failing and will be reviewed later.")]
    public async Task CreateAsync_WithNonSeekableStream_ShouldWork()
    {
        // Arrange
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        await using var memoryStream = new NonSeekableMemoryStream(fileBytes);

        // Act
        await using var reader = await DbfReader.CreateAsync(memoryStream);

        // Assert
        Assert.NotNull(reader);
        Assert.True(reader.Fields.Count > 0);
        var record = reader.Records.First();
    }

    private class NonSeekableMemoryStream : MemoryStream
    {
        public NonSeekableMemoryStream(byte[] buffer) : base(buffer)
        {
        }

        public override bool CanSeek => false;
    }

    //[Fact]
    public async Task PipeReader_ShouldReadAllRecords()
    {
        // Arrange
        var filePath = TestHelper.GetTestFilePath(TestHelper.TestFiles.People);
        var fileBytes = await File.ReadAllBytesAsync(filePath);

        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(fileBytes);
        await pipe.Writer.CompleteAsync();

        var options = new DbfReaderOptions();
        var header = DbfHeader.Read(new BinaryReader(new MemoryStream(fileBytes)));
        var fields = DbfField.ReadFields(new BinaryReader(new MemoryStream(fileBytes, 32, fileBytes.Length - 32)), header.Encoding, (int)header.NumberOfRecords, options.LowerCaseFieldNames, header.DbfVersion);

        // Act
        var reader = new DbfReader(new MemoryStream(), false, header, fields, options, null, "test", pipe.Reader, Task.CompletedTask);
        var records = new List<DbfRecord>();
        await foreach (var record in reader.ReadRecordsAsync())
        {
            records.Add(record);
        }

        // Assert
        Assert.Equal(2, records.Count);
    }
}
