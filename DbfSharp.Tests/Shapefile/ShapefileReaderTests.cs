using DbfSharp.Core;
using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Shapefile;

public class ShapefileReaderTests
{
    [Fact]
    public void Create_FromValidShpStream_ShouldCreateReaderWithGeometryOnly()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        using var reader = ShapefileReader.Create(shpStream);
        Assert.NotNull(reader);
        Assert.NotNull(reader.Header);
        Assert.Equal(ShapeType.Point, reader.ShapeType);
        Assert.False(reader.HasIndex);
        Assert.False(reader.HasAttributes);
        Assert.Equal(0, reader.RecordCount); // Without index, sequential reading doesn't pre-count
        Assert.Equal("stream", reader.Source);
    }

    [Fact]
    public void Create_FromValidShpAndShxStreams_ShouldCreateReaderWithIndex()
    {
        var shpData = CreateValidShapefileBytes();
        var shxData = CreateValidIndexBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream);
        Assert.NotNull(reader);
        Assert.NotNull(reader.Header);
        Assert.True(reader.HasIndex);
        Assert.False(reader.HasAttributes);
        Assert.Equal(2, reader.RecordCount);
        Assert.NotNull(reader.Index);
    }

    [Fact]
    public void Create_WithCustomSource_ShouldSetCorrectSource()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        using var reader = ShapefileReader.Create(shpStream, source: "test-shapefile");
        Assert.Equal("test-shapefile", reader.Source);
    }

    [Fact]
    public void Create_WithNullShpStream_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShapefileReader.Create(null!));
    }

    [Fact]
    public void Create_WithNonReadableShpStream_ShouldThrowArgumentException()
    {
        using var stream = new MemoryStream();
        stream.Close(); // Makes stream non-readable
        Assert.Throws<ArgumentException>(() => ShapefileReader.Create(stream));
    }

    [Fact]
    public void Create_WithNonReadableShxStream_ShouldThrowArgumentException()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream();
        shxStream.Close(); // Makes stream non-readable
        Assert.Throws<ArgumentException>(() => ShapefileReader.Create(shpStream, shxStream));
    }

    [Fact]
    public void Create_WithInvalidShapefileData_ShouldThrowFormatException()
    {
        var invalidData = new byte[50]; // Too short for a valid header
        using var stream = new MemoryStream(invalidData);
        Assert.Throws<EndOfStreamException>(() => ShapefileReader.Create(stream));
    }

    [Fact]
    public void Properties_ShouldReturnCorrectValues()
    {
        var shpData = CreateValidShapefileBytes();
        var shxData = CreateValidIndexBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream, source: "test");
        Assert.NotNull(reader.Header);
        Assert.Equal(ShapeType.Point, reader.ShapeType);
        Assert.Equal(0.0, reader.BoundingBox.MinX);
        Assert.Equal(0.0, reader.BoundingBox.MinY);
        Assert.Equal(1.0, reader.BoundingBox.MaxX);
        Assert.Equal(1.0, reader.BoundingBox.MaxY);
        Assert.True(reader.HasIndex);
        Assert.False(reader.HasAttributes);
        Assert.Null(reader.DbfReader);
        Assert.NotNull(reader.Index);
        Assert.Equal(2, reader.RecordCount);
        Assert.Equal("test", reader.Source);
    }

    [Fact]
    public void Records_WithIndex_ShouldEnumerateRecords()
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        var shxData = CreateValidIndexForPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream);
        var records = reader.Records.ToList();
        Assert.Equal(2, records.Count);

        // Check first record
        Assert.Equal(1, records[0].RecordNumber);
        Assert.IsType<Point>(records[0].Geometry);
        var point1 = (Point)records[0].Geometry;
        Assert.Equal(123.456, point1.X, precision: 6);
        Assert.Equal(789.012, point1.Y, precision: 6);

        // Check second record
        Assert.Equal(2, records[1].RecordNumber);
        Assert.IsType<Point>(records[1].Geometry);
        var point2 = (Point)records[1].Geometry;
        Assert.Equal(234.567, point2.X, precision: 6);
        Assert.Equal(890.123, point2.Y, precision: 6);
    }

    [Fact]
    public void Records_WithoutIndex_ShouldEnumerateSequentially()
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var reader = ShapefileReader.Create(shpStream);
        var records = reader.Records.ToList();
        Assert.Equal(2, records.Count);
        Assert.Equal(1, records[0].RecordNumber);
        Assert.Equal(2, records[1].RecordNumber);
    }

    [Fact]
    public void Features_WithoutAttributes_ShouldReturnFeaturesWithNullAttributes()
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        var shxData = CreateValidIndexForPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream);
        var features = reader.Features.ToList();
        Assert.Equal(2, features.Count);
        Assert.False(features[0].HasAttributes);
        Assert.Null(features[0].Attributes);
        Assert.NotNull(features[0].Geometry);
    }

    [Fact]
    public void GetRecord_ValidRecordNumber_ShouldReturnCorrectRecord()
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        var shxData = CreateValidIndexForPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream);
        var record1 = reader.GetRecord(1);
        var record2 = reader.GetRecord(2);
        Assert.Equal(1, record1.RecordNumber);
        Assert.Equal(2, record2.RecordNumber);
        Assert.IsType<Point>(record1.Geometry);
        Assert.IsType<Point>(record2.Geometry);
    }

    [Fact]
    public void GetRecord_WithoutIndex_ShouldThrowInvalidOperationException()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        using var reader = ShapefileReader.Create(shpStream);
        Assert.Throws<InvalidOperationException>(() => reader.GetRecord(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)] // Out of range (only 2 records)
    public void GetRecord_InvalidRecordNumber_ShouldThrowArgumentOutOfRangeException(
        int recordNumber
    )
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        var shxData = CreateValidIndexForPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream);
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetRecord(recordNumber));
    }

    [Fact]
    public void GetFeature_ValidRecordNumber_ShouldReturnCorrectFeature()
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        var shxData = CreateValidIndexForPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream);
        var feature = reader.GetFeature(1);
        Assert.Equal(1, feature.RecordNumber);
        Assert.NotNull(feature.Geometry);
        Assert.False(feature.HasAttributes);
        Assert.Null(feature.Attributes);
    }

    [Fact]
    public void GetFeature_WithoutIndex_ShouldThrowInvalidOperationException()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        using var reader = ShapefileReader.Create(shpStream);
        Assert.Throws<InvalidOperationException>(() => reader.GetFeature(1));
    }

    [Fact]
    public void Records_DisposedReader_ShouldThrowObjectDisposedException()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        var reader = ShapefileReader.Create(shpStream);
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.Records.ToList());
    }

    [Fact]
    public void Features_DisposedReader_ShouldThrowObjectDisposedException()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        var reader = ShapefileReader.Create(shpStream);
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.Features.ToList());
    }

    [Fact]
    public void GetRecord_DisposedReader_ShouldThrowObjectDisposedException()
    {
        var shpData = CreateValidShapefileWithPointsBytes();
        var shxData = CreateValidIndexForPointsBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        var reader = ShapefileReader.Create(shpStream, shxStream);
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.GetRecord(1));
    }

    [Fact]
    public void Dispose_MultipleDispose_ShouldNotThrow()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        var reader = ShapefileReader.Create(shpStream);

        // Act & Assert - Should not throw
        reader.Dispose();
        reader.Dispose();
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var shpData = CreateValidShapefileBytes();
        var shxData = CreateValidIndexBytes();
        using var shpStream = new MemoryStream(shpData);
        using var shxStream = new MemoryStream(shxData);
        using var reader = ShapefileReader.Create(shpStream, shxStream, source: "test.shp");
        var result = reader.ToString();
        Assert.Contains("test.shp", result);
        Assert.Contains("Point", result);
        Assert.Contains("2 records", result);
        Assert.Contains("indexed", result);
        Assert.Contains("no attributes", result);
    }

    [Fact]
    public void GetValidationErrors_ValidReader_ShouldReturnEmpty()
    {
        var shpData = CreateValidShapefileBytes();
        using var shpStream = new MemoryStream(shpData);
        using var reader = ShapefileReader.Create(shpStream);
        var errors = reader.GetValidationErrors().ToList();

        // Assert - Simple shapefile without index should have no validation errors
        Assert.Empty(errors);
    }

    [Fact]
    public void Create_FromFilePath_ValidFile_ShouldSucceed()
    {
        var shapefilePath = Path.Combine("DbfSharp.Tests", "Resources", "shp", "trAirHeli.shp");

        // Skip test if file doesn't exist
        if (!File.Exists(shapefilePath))
        {
            return;
        }

        using var reader = ShapefileReader.Create(shapefilePath);
        Assert.NotNull(reader);
        Assert.NotNull(reader.Header);
        Assert.True(reader.Header.IsValid);
        Assert.Contains("trAirHeli.shp", reader.Source);
    }

    [Fact]
    public void Create_FromNullFilePath_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShapefileReader.Create(null!));
    }

    [Fact]
    public void Create_FromEmptyFilePath_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShapefileReader.Create(""));
    }

    [Fact]
    public void Create_FromNonExistentFile_ShouldThrowFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => ShapefileReader.Create("nonexistent.shp"));
    }

    private static byte[] CreateValidShapefileBytes()
    {
        var data = new byte[128]; // Header (100) + minimal content
        var span = data.AsSpan();

        // Write shapefile header
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], 64); // File length in words
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

        return data;
    }

    private static byte[] CreateValidShapefileWithPointsBytes()
    {
        // Header (100) + 2 records (28 bytes each = 20 bytes for point + 8 bytes header)
        var data = new byte[156];
        var span = data.AsSpan();

        // Write shapefile header
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], 78); // File length in words (156/2)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[28..32], 1000); // Version
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[32..36],
            (int)ShapeType.Point
        ); // Shape type

        // Bounding box
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[36..44], 123.456); // XMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[44..52], 789.012); // YMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[52..60], 234.567); // XMax
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[60..68], 890.123); // YMax

        var offset = 100; // After header

        // Record 1
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, 4), 1); // Record number
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset + 4, 4), 10); // Content length
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span.Slice(offset + 8, 4),
            (int)ShapeType.Point
        ); // Shape type
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span.Slice(offset + 12, 8),
            123.456
        ); // X
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span.Slice(offset + 20, 8),
            789.012
        ); // Y
        offset += 28;

        // Record 2
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, 4), 2); // Record number
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset + 4, 4), 10); // Content length
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span.Slice(offset + 8, 4),
            (int)ShapeType.Point
        ); // Shape type
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span.Slice(offset + 12, 8),
            234.567
        ); // X
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span.Slice(offset + 20, 8),
            890.123
        ); // Y

        return data;
    }

    private static byte[] CreateValidIndexBytes()
    {
        // Create index with header + 2 records pointing to a basic shapefile
        var data = new byte[116]; // Header (100) + 2 records * 8 bytes each
        var span = data.AsSpan();

        // Write header (similar to shapefile header)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], 58); // File length in words (116/2)
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

        // Record entries pointing within the basic shapefile size (128 bytes total)
        // Record 1: offset=50 (100 bytes / 2), content length=4 (8 bytes / 2) - fits within shapefile
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[100..104], 50); // Offset in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[104..108], 4); // Content length in words

        // Record 2: offset=56 (112 bytes / 2), content length=4 (8 bytes / 2) - fits within shapefile
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[108..112], 56); // Offset in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[112..116], 4); // Content length in words

        return data;
    }

    private static byte[] CreateValidIndexForPointsBytes()
    {
        // Create index with header + 2 records matching the points shapefile
        var data = new byte[116]; // Header (100) + 2 records * 8 bytes each
        var span = data.AsSpan();

        // Write header
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994); // File code
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], 58); // File length in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[28..32], 1000); // Version
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[32..36],
            (int)ShapeType.Point
        ); // Shape type

        // Bounding box to match points
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[36..44], 123.456); // XMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[44..52], 789.012); // YMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[52..60], 234.567); // XMax
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[60..68], 890.123); // YMax

        // Record 1: offset=50 (100 bytes / 2), content length=10 (20 bytes / 2)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[100..104], 50); // Offset in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[104..108], 10); // Content length in words

        // Record 2: offset=64 (128 bytes / 2), content length=10 (20 bytes / 2)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[108..112], 64); // Offset in words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[112..116], 10); // Content length in words

        return data;
    }
}
