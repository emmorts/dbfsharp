using DbfSharp.Core;
using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Shapefile;

public class ShapefileIndexTests
{
    [Fact]
    public void RecordInfo_Constructor_ShouldSetPropertiesCorrectly()
    {
        var recordInfo = new ShapefileIndex.RecordInfo(100, 50);
        Assert.Equal(100, recordInfo.Offset);
        Assert.Equal(50, recordInfo.ContentLength);
        Assert.Equal(200, recordInfo.ByteOffset); // 100 * 2
        Assert.Equal(100, recordInfo.ContentLengthInBytes); // 50 * 2
        Assert.Equal(108, recordInfo.TotalRecordLengthInBytes); // 100 + 8 byte header
        Assert.True(recordInfo.HasContent);
    }

    [Fact]
    public void RecordInfo_WithZeroLength_ShouldNotHaveContent()
    {
        var recordInfo = new ShapefileIndex.RecordInfo(100, 0);
        Assert.False(recordInfo.HasContent);
        Assert.Equal(0, recordInfo.ContentLengthInBytes);
        Assert.Equal(8, recordInfo.TotalRecordLengthInBytes); // Just the header
    }

    [Fact]
    public void RecordInfo_ToString_ShouldFormatCorrectly()
    {
        var recordInfo = new ShapefileIndex.RecordInfo(100, 50);
        var result = recordInfo.ToString();
        Assert.Equal("Record at offset 200, content length 100 bytes", result);
    }

    [Fact]
    public void Read_ValidStream_ShouldCreateIndex()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        Assert.Equal(2, index.RecordCount);
        Assert.Equal(ShapefileHeader.ExpectedFileCode, index.Header.FileCode);
        Assert.Equal(ShapeType.Point, index.Header.ShapeType);
    }

    [Fact]
    public void Read_NullStream_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShapefileIndex.Read((Stream)null!));
    }

    [Fact]
    public void Read_StreamTooShort_ShouldThrowEndOfStreamException()
    {
        var shortData = new byte[50]; // Less than required 100 bytes for header
        using var stream = new MemoryStream(shortData);
        Assert.Throws<EndOfStreamException>(() => ShapefileIndex.Read(stream));
    }

    [Fact]
    public void GetRecordInfo_ValidIndex_ShouldReturnCorrectInfo()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        var recordInfo0 = index.GetRecordInfo(0);
        var recordInfo1 = index.GetRecordInfo(1);
        Assert.Equal(50, recordInfo0.Offset);
        Assert.Equal(10, recordInfo0.ContentLength);
        Assert.Equal(70, recordInfo1.Offset);
        Assert.Equal(10, recordInfo1.ContentLength);
    }

    [Fact]
    public void GetRecordInfo_InvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        Assert.Throws<ArgumentOutOfRangeException>(() => index.GetRecordInfo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => index.GetRecordInfo(2));
    }

    [Fact]
    public void GetRecordInfo_DisposedIndex_ShouldThrowObjectDisposedException()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        index.Dispose();
        Assert.Throws<ObjectDisposedException>(() => index.GetRecordInfo(0));
    }

    [Fact]
    public void GetAllRecordInfo_ShouldReturnAllRecords()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        var allRecords = index.GetAllRecordInfo();
        Assert.Equal(2, allRecords.Length);
        Assert.Equal(50, allRecords[0].Offset);
        Assert.Equal(70, allRecords[1].Offset);
    }

    [Fact]
    public void GetAllRecordInfo_DisposedIndex_ShouldThrowObjectDisposedException()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        index.Dispose();
        Assert.Throws<ObjectDisposedException>(() => index.GetAllRecordInfo());
    }

    [Fact]
    public void FindRecordsInRange_ShouldReturnOverlappingRecords()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);

        // Act - Search for records between byte offset 100-150
        var recordsInRange = index.FindRecordsInRange(100, 150).ToList();
        // Record 0: offset 100 (50*2), length 28 (10*2+8) -> range 100-128
        // Record 1: offset 140 (70*2), length 28 (10*2+8) -> range 140-168
        // Both should overlap with range 100-150
        Assert.Equal(2, recordsInRange.Count);
        Assert.Contains(0, recordsInRange);
        Assert.Contains(1, recordsInRange);
    }

    [Fact]
    public void FindRecordsInRange_NoOverlap_ShouldReturnEmpty()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);

        // Act - Search for records in range that doesn't overlap
        var recordsInRange = index.FindRecordsInRange(0, 50).ToList();
        Assert.Empty(recordsInRange);
    }

    [Fact]
    public void FindRecordsInRange_DisposedIndex_ShouldThrowObjectDisposedException()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        index.Dispose();
        Assert.Throws<ObjectDisposedException>(() => index.FindRecordsInRange(0, 100).ToList());
    }

    [Fact]
    public void GetRecordSizeStatistics_ShouldCalculateCorrectly()
    {
        var indexData = CreateVariableSizeIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        var stats = index.GetRecordSizeStatistics();
        Assert.Equal(20, stats.MinSize); // 10 * 2
        Assert.Equal(40, stats.MaxSize); // 20 * 2
        Assert.Equal(26.666666666666668, stats.AverageSize, precision: 10); // (20+20+40)/3
        Assert.Equal(20, stats.MedianSize);
        Assert.Equal(80, stats.TotalSize); // 20+20+40
    }

    [Fact]
    public void GetRecordSizeStatistics_EmptyIndex_ShouldReturnZeros()
    {
        var indexData = CreateEmptyIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        var stats = index.GetRecordSizeStatistics();
        Assert.Equal(0, stats.MinSize);
        Assert.Equal(0, stats.MaxSize);
        Assert.Equal(0, stats.AverageSize);
        Assert.Equal(0, stats.MedianSize);
        Assert.Equal(0, stats.TotalSize);
    }

    [Fact]
    public void GetRecordSizeStatistics_DisposedIndex_ShouldThrowObjectDisposedException()
    {
        var indexData = CreateValidIndexBytes();
        using var stream = new MemoryStream(indexData);
        var index = ShapefileIndex.Read(stream);
        index.Dispose();
        Assert.Throws<ObjectDisposedException>(() => index.GetRecordSizeStatistics());
    }

    [Fact]
    public void RecordSizeStatistics_ToString_ShouldFormatCorrectly()
    {
        var stats = new ShapefileIndex.RecordSizeStatistics(10, 50, 25.5, 20.0, 255);
        var result = stats.ToString();
        Assert.Equal(
            "Record sizes: Min=10, Max=50, Avg=25.5, Median=20.0, Total=255 bytes",
            result
        );
    }

    [Fact]
    public void Read_RealShapefileIndex_ShouldParseCorrectly()
    {
        var indexPath = Path.Combine("DbfSharp.Tests", "Resources", "shp", "trAirHeli.shx");

        // Skip test if file doesn't exist
        if (!File.Exists(indexPath))
        {
            return;
        }

        using var stream = File.OpenRead(indexPath);
        using var index = ShapefileIndex.Read(stream);
        Assert.True(index.RecordCount >= 0);
        Assert.Equal(ShapefileHeader.ExpectedFileCode, index.Header.FileCode);
        Assert.Equal(ShapefileHeader.ExpectedVersion, index.Header.Version);
        Assert.True(index.Header.IsValid);

        // Test accessing first record if any exist
        if (index.RecordCount > 0)
        {
            var firstRecord = index.GetRecordInfo(0);
            Assert.True(firstRecord.Offset > 0);
            Assert.True(firstRecord.ContentLength >= 0);
        }
    }

    private static byte[] CreateValidIndexBytes()
    {
        // Create index with header + 2 records
        var data = new byte[100 + 2 * 8]; // Header + 2 records * 8 bytes each
        var span = data.AsSpan();

        // Write header (similar to ShapefileHeader)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], (100 + 16) / 2); // File length in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[28..32], 1000); // Version
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[32..36],
            (int)ShapeType.Point
        ); // Shape type

        // Bounding box
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[36..44], 0.0); // XMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[44..52], 0.0); // YMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[52..60], 1.0); // XMax
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[60..68], 1.0); // YMax

        // Record 1: offset=50, content length=10
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[100..104], 50); // Offset in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[104..108], 10); // Content length in words

        // Record 2: offset=70, content length=10
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[108..112], 70); // Offset in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[112..116], 10); // Content length in words

        return data;
    }

    private static byte[] CreateVariableSizeIndexBytes()
    {
        // Create index with header + 3 records of different sizes
        var data = new byte[100 + 3 * 8]; // Header + 3 records * 8 bytes each
        var span = data.AsSpan();

        // Write header
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], (100 + 24) / 2); // File length in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[28..32], 1000); // Version
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[32..36],
            (int)ShapeType.Point
        ); // Shape type

        // Record 1: offset=50, content length=10 (20 bytes)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[100..104], 50);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[104..108], 10);

        // Record 2: offset=70, content length=10 (20 bytes)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[108..112], 70);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[112..116], 10);

        // Record 3: offset=90, content length=20 (40 bytes)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[116..120], 90);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[120..124], 20);

        return data;
    }

    private static byte[] CreateEmptyIndexBytes()
    {
        // Create index with header but no records
        var data = new byte[100]; // Just the header
        var span = data.AsSpan();

        // Write header
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], 50); // File length in words (header only)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[28..32], 1000); // Version
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[32..36],
            (int)ShapeType.Point
        ); // Shape type

        return data;
    }
}
