using DbfSharp.Core;
using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Shapefile;

public class ShapefileHeaderTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreateValidHeader()
    {
        var unused = new byte[20];
        var header = new ShapefileHeader(
            ShapefileHeader.ExpectedFileCode,
            unused,
            fileLength: 1000,
            ShapefileHeader.ExpectedVersion,
            ShapeType.Point,
            xMin: -180.0,
            yMin: -90.0,
            xMax: 180.0,
            yMax: 90.0,
            zMin: -1000.0,
            zMax: 1000.0,
            mMin: 0.0,
            mMax: 100.0
        );
        Assert.Equal(ShapefileHeader.ExpectedFileCode, header.FileCode);
        Assert.Equal(unused, header.Unused);
        Assert.Equal(1000, header.FileLength);
        Assert.Equal(ShapefileHeader.ExpectedVersion, header.Version);
        Assert.Equal(ShapeType.Point, header.ShapeType);
        Assert.Equal(-180.0, header.XMin);
        Assert.Equal(-90.0, header.YMin);
        Assert.Equal(180.0, header.XMax);
        Assert.Equal(90.0, header.YMax);
        Assert.Equal(-1000.0, header.ZMin);
        Assert.Equal(1000.0, header.ZMax);
        Assert.Equal(0.0, header.MMin);
        Assert.Equal(100.0, header.MMax);
        Assert.True(header.IsValid);
    }

    [Fact]
    public void Constructor_WithNullUnused_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ShapefileHeader(
                ShapefileHeader.ExpectedFileCode,
                null!,
                1000,
                ShapefileHeader.ExpectedVersion,
                ShapeType.Point,
                0,
                0,
                1,
                1
            )
        );
    }

    [Theory]
    [InlineData(ShapeType.Point, false, false)]
    [InlineData(ShapeType.PointZ, true, false)]
    [InlineData(ShapeType.PointM, false, true)]
    [InlineData(ShapeType.PolyLine, false, false)]
    [InlineData(ShapeType.PolyLineZ, true, false)]
    [InlineData(ShapeType.PolyLineM, false, true)]
    [InlineData(ShapeType.Polygon, false, false)]
    [InlineData(ShapeType.PolygonZ, true, false)]
    [InlineData(ShapeType.PolygonM, false, true)]
    public void HasZ_HasM_ShouldReturnCorrectValues(
        ShapeType shapeType,
        bool expectedHasZ,
        bool expectedHasM
    )
    {
        var header = new ShapefileHeader(
            ShapefileHeader.ExpectedFileCode,
            new byte[20],
            1000,
            ShapefileHeader.ExpectedVersion,
            shapeType,
            0,
            0,
            1,
            1
        );
        Assert.Equal(expectedHasZ, header.HasZ);
        Assert.Equal(expectedHasM, header.HasM);
    }

    [Fact]
    public void FileLengthInBytes_ShouldCalculateCorrectly()
    {
        var header = new ShapefileHeader(
            ShapefileHeader.ExpectedFileCode,
            new byte[20],
            fileLength: 500, // 500 words
            ShapefileHeader.ExpectedVersion,
            ShapeType.Point,
            0,
            0,
            1,
            1
        );
        Assert.Equal(1000, header.FileLengthInBytes); // 500 * 2 = 1000 bytes
    }

    [Fact]
    public void BoundingBox_ShouldReturnCorrectBounds()
    {
        var header = new ShapefileHeader(
            ShapefileHeader.ExpectedFileCode,
            new byte[20],
            1000,
            ShapefileHeader.ExpectedVersion,
            ShapeType.PointZ,
            xMin: -180.0,
            yMin: -90.0,
            xMax: 180.0,
            yMax: 90.0,
            zMin: -1000.0,
            zMax: 1000.0,
            mMin: 0.0,
            mMax: 100.0
        );
        var bbox = header.BoundingBox;
        Assert.Equal(-180.0, bbox.MinX);
        Assert.Equal(-90.0, bbox.MinY);
        Assert.Equal(180.0, bbox.MaxX);
        Assert.Equal(90.0, bbox.MaxY);
        Assert.Equal(-1000.0, bbox.MinZ);
        Assert.Equal(1000.0, bbox.MaxZ);
        Assert.Equal(0.0, bbox.MinM);
        Assert.Equal(100.0, bbox.MaxM);
    }

    [Theory]
    [InlineData(9994, 1000, ShapeType.Point, 0.0, 0.0, 1.0, 1.0, true)] // Valid header
    [InlineData(9995, 1000, ShapeType.Point, 0.0, 0.0, 1.0, 1.0, false)] // Invalid file code
    [InlineData(9994, 1001, ShapeType.Point, 0.0, 0.0, 1.0, 1.0, false)] // Invalid version
    [InlineData(9994, 1000, (ShapeType)999, 0.0, 0.0, 1.0, 1.0, false)] // Invalid shape type
    [InlineData(9994, 1000, ShapeType.Point, 1.0, 0.0, 0.0, 1.0, false)] // XMin > XMax
    [InlineData(9994, 1000, ShapeType.Point, 0.0, 1.0, 1.0, 0.0, false)] // YMin > YMax
    public void IsValid_ShouldReturnCorrectValidationResult(
        int fileCode,
        int version,
        ShapeType shapeType,
        double xMin,
        double yMin,
        double xMax,
        double yMax,
        bool expected
    )
    {
        var header = new ShapefileHeader(
            fileCode,
            new byte[20],
            1000,
            version,
            shapeType,
            xMin,
            yMin,
            xMax,
            yMax
        );
        Assert.Equal(expected, header.IsValid);
    }

    [Fact]
    public void IsValid_WithInvalidZBounds_ShouldReturnFalse()
    {
        var header = new ShapefileHeader(
            ShapefileHeader.ExpectedFileCode,
            new byte[20],
            1000,
            ShapefileHeader.ExpectedVersion,
            ShapeType.PointZ,
            0,
            0,
            1,
            1,
            zMin: 100.0, // ZMin > ZMax
            zMax: 50.0
        );
        Assert.False(header.IsValid);
    }

    [Fact]
    public void IsValid_WithInvalidMBounds_ShouldReturnFalse()
    {
        var header = new ShapefileHeader(
            ShapefileHeader.ExpectedFileCode,
            new byte[20],
            1000,
            ShapefileHeader.ExpectedVersion,
            ShapeType.PointM,
            0,
            0,
            1,
            1,
            mMin: 100.0, // MMin > MMax
            mMax: 50.0
        );
        Assert.False(header.IsValid);
    }

    [Fact]
    public void Read_ValidStream_ShouldParseHeaderCorrectly()
    {
        var headerData = CreateValidHeaderBytes();
        using var stream = new MemoryStream(headerData);
        var header = ShapefileHeader.Read(stream);
        Assert.Equal(ShapefileHeader.ExpectedFileCode, header.FileCode);
        Assert.Equal(ShapefileHeader.ExpectedVersion, header.Version);
        Assert.Equal(ShapeType.Point, header.ShapeType);
        Assert.True(header.IsValid);
    }

    [Fact]
    public void Read_NullStream_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShapefileHeader.Read((Stream)null!));
    }

    [Fact]
    public void Read_NonReadableStream_ShouldThrowArgumentException()
    {
        using var stream = new MemoryStream();
        stream.Close(); // Makes stream non-readable
        Assert.Throws<ArgumentException>(() => ShapefileHeader.Read(stream));
    }

    [Fact]
    public void Read_StreamTooShort_ShouldThrowEndOfStreamException()
    {
        var shortData = new byte[50]; // Less than required 100 bytes
        using var stream = new MemoryStream(shortData);
        Assert.Throws<EndOfStreamException>(() => ShapefileHeader.Read(stream));
    }

    [Fact]
    public void Read_RealShapefileHeader_ShouldParseCorrectly()
    {
        var shapefilePath = Path.Combine("DbfSharp.Tests", "Resources", "shp", "trAirHeli.shp");

        if (!File.Exists(shapefilePath))
        {
            return;
        }

        using var stream = File.OpenRead(shapefilePath);
        var header = ShapefileHeader.Read(stream);
        Assert.Equal(ShapefileHeader.ExpectedFileCode, header.FileCode);
        Assert.Equal(ShapefileHeader.ExpectedVersion, header.Version);
        Assert.True(header.IsValid);
        Assert.True(header.FileLength > 0);
        Assert.True(header.XMin <= header.XMax);
        Assert.True(header.YMin <= header.YMax);
    }

    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        Assert.Equal(9994, ShapefileHeader.ExpectedFileCode);
        Assert.Equal(1000, ShapefileHeader.ExpectedVersion);
        Assert.Equal(100, ShapefileHeader.Size);
    }

    private static byte[] CreateValidHeaderBytes()
    {
        var header = new byte[100];
        var span = header.AsSpan();

        // File code (big-endian)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[0..4], 9994);

        // Unused bytes 4-23 (leave as zeros)

        // File length (big-endian) - words
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span[24..28], 50);

        // Version (little-endian)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[28..32], 1000);

        // Shape type (little-endian)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[32..36],
            (int)ShapeType.Point
        );

        // Bounding box (little-endian doubles)
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[36..44], -180.0); // XMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[44..52], -90.0); // YMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[52..60], 180.0); // XMax
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[60..68], 90.0); // YMax

        // Z and M ranges (little-endian doubles) - set to 0 for 2D shapes
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[68..76], 0.0); // ZMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[76..84], 0.0); // ZMax
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[84..92], 0.0); // MMin
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[92..100], 0.0); // MMax

        return header;
    }
}
