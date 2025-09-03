using DbfSharp.Core.Geometry;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Tests.Shapefile;

public class ShapeParserTests
{
    [Fact]
    public void ParseShape_NullShape_ShouldReturnNullShapeInstance()
    {
        var shapeData = CreateShapeData(ShapeType.NullShape);
        var shape = ShapeParser.ParseShape(shapeData);
        Assert.IsType<NullShape>(shape);
        Assert.Same(NullShape.Instance, shape);
    }

    [Fact]
    public void ParseShape_Point2D_ShouldReturnValidPoint()
    {
        var shapeData = CreatePoint2DData(123.456, 789.012);
        var shape = ShapeParser.ParseShape(shapeData);
        var point = Assert.IsType<Point>(shape);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Null(point.Z);
        Assert.Null(point.M);
        Assert.Equal(ShapeType.Point, point.ShapeType);
    }

    [Fact]
    public void ParseShape_PointZ_ShouldReturnValidPointWithZ()
    {
        var shapeData = CreatePointZData(123.456, 789.012, 345.678);
        var shape = ShapeParser.ParseShape(shapeData);
        var point = Assert.IsType<Point>(shape);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Equal(345.678, point.Z);
        Assert.Null(point.M);
        Assert.Equal(ShapeType.PointZ, point.ShapeType);
    }

    [Fact]
    public void ParseShape_PointZWithM_ShouldReturnValidPointWithZAndM()
    {
        var shapeData = CreatePointZWithMData(123.456, 789.012, 345.678, 901.234);
        var shape = ShapeParser.ParseShape(shapeData);
        var point = Assert.IsType<Point>(shape);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Equal(345.678, point.Z);
        Assert.Equal(901.234, point.M);
        Assert.Equal(ShapeType.PointZ, point.ShapeType); // Should prioritize Z
    }

    [Fact]
    public void ParseShape_PointM_ShouldReturnValidPointWithM()
    {
        var shapeData = CreatePointMData(123.456, 789.012, 345.678);
        var shape = ShapeParser.ParseShape(shapeData);
        var point = Assert.IsType<Point>(shape);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Null(point.Z);
        Assert.Equal(345.678, point.M);
        Assert.Equal(ShapeType.PointM, point.ShapeType);
    }

    [Fact]
    public void ParseShape_PointMWithNaN_ShouldReturnPointWithNullM()
    {
        var shapeData = CreatePointMData(123.456, 789.012, double.NaN);
        var shape = ShapeParser.ParseShape(shapeData);
        var point = Assert.IsType<Point>(shape);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Null(point.Z);
        Assert.Null(point.M); // NaN should be converted to null
    }

    [Fact]
    public void ParseShape_MultiPoint2D_ShouldReturnValidMultiPoint()
    {
        var coordinates = new[] { (123.456, 789.012), (234.567, 890.123), (345.678, 901.234) };
        var shapeData = CreateMultiPoint2DData(coordinates);
        var shape = ShapeParser.ParseShape(shapeData);
        var multiPoint = Assert.IsType<MultiPoint>(shape);
        Assert.Equal(3, multiPoint.PointCount);
        var points = multiPoint.GetPoints().ToArray();

        Assert.Equal(123.456, points[0].X);
        Assert.Equal(789.012, points[0].Y);
        Assert.Equal(234.567, points[1].X);
        Assert.Equal(890.123, points[1].Y);
        Assert.Equal(345.678, points[2].X);
        Assert.Equal(901.234, points[2].Y);
    }

    [Fact]
    public void ParseShape_PolyLine2D_ShouldReturnValidPolyLine()
    {
        var parts = new[] { new[] { (0.0, 0.0), (1.0, 1.0), (2.0, 0.0) } };
        var shapeData = CreatePolyLine2DData(parts);
        var shape = ShapeParser.ParseShape(shapeData);
        var polyLine = Assert.IsType<PolyLine>(shape);
        Assert.Equal(1, polyLine.PartCount);
        Assert.Equal(3, polyLine.CoordinateCount);

        var part = polyLine.GetPart(0);
        Assert.Equal(0.0, part[0].X);
        Assert.Equal(0.0, part[0].Y);
        Assert.Equal(1.0, part[1].X);
        Assert.Equal(1.0, part[1].Y);
        Assert.Equal(2.0, part[2].X);
        Assert.Equal(0.0, part[2].Y);
    }

    [Fact]
    public void ParseShape_Polygon2D_ShouldReturnValidPolygon()
    {
        var rings = new[] { new[] { (0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0), (0.0, 0.0) } };
        var shapeData = CreatePolygon2DData(rings);
        var shape = ShapeParser.ParseShape(shapeData);
        var polygon = Assert.IsType<Polygon>(shape);
        Assert.Equal(1, polygon.RingCount);
        Assert.Equal(5, polygon.CoordinateCount);

        var ring = polygon.GetRing(0);
        Assert.Equal(0.0, ring[0].X);
        Assert.Equal(0.0, ring[0].Y);
        Assert.Equal(1.0, ring[1].X);
        Assert.Equal(0.0, ring[1].Y);
        Assert.Equal(1.0, ring[2].X);
        Assert.Equal(1.0, ring[2].Y);
    }

    [Fact]
    public void ParseShape_TooShortData_ShouldThrowArgumentException()
    {
        var shortData = new byte[2]; // Less than required 4 bytes for shape type
        Assert.Throws<ArgumentException>(() => ShapeParser.ParseShape(shortData));
    }

    [Fact]
    public void ParseShape_InvalidShapeType_ShouldThrowFormatException()
    {
        var invalidShapeData = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidShapeData.AsSpan(0, 4),
            999
        ); // Invalid shape type
        Assert.Throws<FormatException>(() => ShapeParser.ParseShape(invalidShapeData));
    }

    [Fact]
    public void ParseShape_UnsupportedShapeType_ShouldThrowFormatException()
    {
        var invalidShapeData = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidShapeData.AsSpan(0, 4),
            999
        ); // Invalid shape type
        Assert.Throws<FormatException>(() => ShapeParser.ParseShape(invalidShapeData));
    }

    [Fact]
    public void ParseShape_MultiPatch_ShouldReturnValidMultiPatch()
    {
        var shapeData = CreateMultiPatchData();
        var shape = ShapeParser.ParseShape(shapeData);
        var multiPatch = Assert.IsType<MultiPatch>(shape);
        Assert.Equal(ShapeType.MultiPatch, multiPatch.ShapeType);
        Assert.Equal(2, multiPatch.PartCount);

        var parts = multiPatch.GetParts().ToList();
        Assert.Equal(PatchType.TriangleStrip, parts[0].PatchType);
        Assert.Equal(PatchType.OuterRing, parts[1].PatchType);

        // Verify coordinates
        Assert.Equal(3, parts[0].CoordinateCount);
        Assert.Equal(4, parts[1].CoordinateCount);

        // Check some coordinate values (triangle strip)
        var coords0 = parts[0].Coordinates;
        Assert.Equal(0.0, coords0[0].X);
        Assert.Equal(0.0, coords0[0].Y);
        Assert.Equal(0.0, coords0[0].Z);

        Assert.Equal(1.0, coords0[1].X);
        Assert.Equal(0.0, coords0[1].Y);
        Assert.Equal(1.0, coords0[1].Z);
    }

    [Fact]
    public void ParseShape_InvalidPointData_ShouldThrowFormatException()
    {
        var invalidPointData = new byte[12]; // Less than required 20 bytes (4 for type + 16 for coordinates)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidPointData.AsSpan(0, 4),
            (int)ShapeType.Point
        );
        Assert.Throws<FormatException>(() => ShapeParser.ParseShape(invalidPointData));
    }

    [Fact]
    public void ParseShape_InvalidPointZData_ShouldThrowFormatException()
    {
        var invalidPointZData = new byte[20]; // Less than required 28 bytes (4 for type + 24 for coordinates)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidPointZData.AsSpan(0, 4),
            (int)ShapeType.PointZ
        );
        Assert.Throws<FormatException>(() => ShapeParser.ParseShape(invalidPointZData));
    }

    [Fact]
    public void ParseShape_InvalidMultiPointData_ShouldThrowFormatException()
    {
        var invalidMultiPointData = new byte[20]; // Less than required 36 bytes (4 for type + 32 for bbox + point count)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidMultiPointData.AsSpan(0, 4),
            (int)ShapeType.MultiPoint
        );
        Assert.Throws<FormatException>(() => ShapeParser.ParseShape(invalidMultiPointData));
    }

    [Fact]
    public void ParseShape_NegativePointCount_ShouldThrowFormatException()
    {
        var invalidData = new byte[40];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidData.AsSpan(0, 4),
            (int)ShapeType.MultiPoint
        );
        // Skip bounding box (32 bytes)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            invalidData.AsSpan(36, 4),
            -1
        ); // Negative point count
        Assert.Throws<FormatException>(() => ShapeParser.ParseShape(invalidData));
    }

    private static byte[] CreateShapeData(ShapeType shapeType)
    {
        var data = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(0, 4),
            (int)shapeType
        );
        return data;
    }

    private static byte[] CreatePoint2DData(double x, double y)
    {
        var data = new byte[20]; // 4 bytes shape type + 16 bytes coordinates
        var span = data.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[0..4],
            (int)ShapeType.Point
        );
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[4..12], x);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[12..20], y);

        return data;
    }

    private static byte[] CreatePointZData(double x, double y, double z)
    {
        var data = new byte[28]; // 4 bytes shape type + 24 bytes coordinates
        var span = data.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[0..4],
            (int)ShapeType.PointZ
        );
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[4..12], x);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[12..20], y);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[20..28], z);

        return data;
    }

    private static byte[] CreatePointZWithMData(double x, double y, double z, double m)
    {
        var data = new byte[36]; // 4 bytes shape type + 32 bytes coordinates
        var span = data.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[0..4],
            (int)ShapeType.PointZ
        );
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[4..12], x);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[12..20], y);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[20..28], z);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[28..36], m);

        return data;
    }

    private static byte[] CreatePointMData(double x, double y, double m)
    {
        var data = new byte[28]; // 4 bytes shape type + 24 bytes coordinates
        var span = data.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[0..4],
            (int)ShapeType.PointM
        );
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[4..12], x);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[12..20], y);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(span[20..28], m);

        return data;
    }

    private static byte[] CreateMultiPoint2DData((double X, double Y)[] coordinates)
    {
        var dataSize = 40 + coordinates.Length * 16; // Shape type + bbox + count + points
        var data = new byte[dataSize];
        var span = data.AsSpan();
        var offset = 0;

        // Shape type
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            (int)ShapeType.MultiPoint
        );
        offset += 4;

        // Bounding box (calculate from coordinates)
        var minX = coordinates.Min(c => c.X);
        var minY = coordinates.Min(c => c.Y);
        var maxX = coordinates.Max(c => c.X);
        var maxY = coordinates.Max(c => c.Y);

        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            minX
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            minY
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            maxX
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            maxY
        );
        offset += 8;

        // Point count
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            coordinates.Length
        );
        offset += 4;

        // Points
        foreach (var (x, y) in coordinates)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
                span[offset..(offset + 8)],
                x
            );
            offset += 8;
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
                span[offset..(offset + 8)],
                y
            );
            offset += 8;
        }

        return data;
    }

    private static byte[] CreatePolyLine2DData((double X, double Y)[][] parts)
    {
        var totalPoints = parts.Sum(part => part.Length);
        var dataSize = 44 + parts.Length * 4 + totalPoints * 16; // Shape type + bbox + counts + part offsets + points
        var data = new byte[dataSize];
        var span = data.AsSpan();
        var offset = 0;

        // Shape type
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            (int)ShapeType.PolyLine
        );
        offset += 4;

        // Bounding box
        var allPoints = parts.SelectMany(part => part).ToArray();
        var minX = allPoints.Min(p => p.X);
        var minY = allPoints.Min(p => p.Y);
        var maxX = allPoints.Max(p => p.X);
        var maxY = allPoints.Max(p => p.Y);

        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            minX
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            minY
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            maxX
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            maxY
        );
        offset += 8;

        // Part count
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            parts.Length
        );
        offset += 4;

        // Point count
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            totalPoints
        );
        offset += 4;

        // Part offsets
        var pointOffset = 0;
        foreach (var part in parts)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                span[offset..(offset + 4)],
                pointOffset
            );
            offset += 4;
            pointOffset += part.Length;
        }

        // Points
        foreach (var part in parts)
        {
            foreach (var (x, y) in part)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
                    span[offset..(offset + 8)],
                    x
                );
                offset += 8;
                System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
                    span[offset..(offset + 8)],
                    y
                );
                offset += 8;
            }
        }

        return data;
    }

    private static byte[] CreatePolygon2DData((double X, double Y)[][] rings)
    {
        var totalPoints = rings.Sum(ring => ring.Length);
        var dataSize = 44 + rings.Length * 4 + totalPoints * 16; // Shape type + bbox + counts + ring offsets + points
        var data = new byte[dataSize];
        var span = data.AsSpan();
        var offset = 0;

        // Shape type
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            (int)ShapeType.Polygon
        );
        offset += 4;

        // Bounding box
        var allPoints = rings.SelectMany(ring => ring).ToArray();
        var minX = allPoints.Min(p => p.X);
        var minY = allPoints.Min(p => p.Y);
        var maxX = allPoints.Max(p => p.X);
        var maxY = allPoints.Max(p => p.Y);

        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            minX
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            minY
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            maxX
        );
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            maxY
        );
        offset += 8;

        // Ring count
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            rings.Length
        );
        offset += 4;

        // Point count
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            totalPoints
        );
        offset += 4;

        // Ring offsets
        var pointOffset = 0;
        foreach (var ring in rings)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                span[offset..(offset + 4)],
                pointOffset
            );
            offset += 4;
            pointOffset += ring.Length;
        }

        // Points
        foreach (var ring in rings)
        {
            foreach (var (x, y) in ring)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
                    span[offset..(offset + 8)],
                    x
                );
                offset += 8;
                System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
                    span[offset..(offset + 8)],
                    y
                );
                offset += 8;
            }
        }

        return data;
    }

    private static byte[] CreateMultiPatchData()
    {
        // Create a MultiPatch with 2 parts:
        // Part 1: TriangleStrip (3 points)
        // Part 2: OuterRing (4 points)

        var totalPoints = 7;
        var numParts = 2;

        // Calculate data size: 4 (shape type) + 32 (bbox) + 4 (numParts) + 4 (numPoints) +
        //                     8 (part indices) + 8 (part types) + 112 (XY coords) + 16 (Z range) + 56 (Z coords)
        var data = new byte[244];
        var span = data.AsSpan();
        var offset = 0;

        // Shape type
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            (int)ShapeType.MultiPatch
        );
        offset += 4;

        // Bounding box (XMin, YMin, XMax, YMax)
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // XMin
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // YMin
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            3.0
        ); // XMax
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // YMax
        offset += 8;

        // Number of parts
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            numParts
        );
        offset += 4;

        // Number of points
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            totalPoints
        );
        offset += 4;

        // Part indices
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            0
        ); // Part 1 starts at point 0
        offset += 4;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            3
        ); // Part 2 starts at point 3
        offset += 4;

        // Part types
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            (int)PatchType.TriangleStrip
        );
        offset += 4;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            span[offset..(offset + 4)],
            (int)PatchType.OuterRing
        );
        offset += 4;

        // XY coordinates for all points
        // Part 1: TriangleStrip (3 points)
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // X1
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // Y1
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            1.0
        ); // X2
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // Y2
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.5
        ); // X3
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            1.0
        ); // Y3
        offset += 8;

        // Part 2: OuterRing (4 points)
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // X4
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // Y4
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            3.0
        ); // X5
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // Y5
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            3.0
        ); // X6
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // Y6
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // X7
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // Y7
        offset += 8;

        // Z range (ZMin, ZMax)
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // ZMin
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // ZMax
        offset += 8;

        // Z coordinates for all points
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.0
        ); // Z1
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            1.0
        ); // Z2
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            0.5
        ); // Z3
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            1.5
        ); // Z4
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            2.0
        ); // Z5
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            1.8
        ); // Z6
        offset += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(
            span[offset..(offset + 8)],
            1.2
        ); // Z7
        offset += 8;

        return data;
    }
}
