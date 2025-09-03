using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Geometry;

public class MultiPatchTests
{
    [Fact]
    public void Constructor_WithValidParts_ShouldCreateMultiPatch()
    {
        var triangleStripCoords = new[]
        {
            new Coordinate(0, 0, 0),
            new Coordinate(1, 0, 1),
            new Coordinate(0, 1, 1),
            new Coordinate(1, 1, 2),
        };
        var triangleStripPart = new PatchPart(PatchType.TriangleStrip, triangleStripCoords);

        var outerRingCoords = new[]
        {
            new Coordinate(2, 2, 0),
            new Coordinate(4, 2, 0),
            new Coordinate(4, 4, 2),
            new Coordinate(2, 4, 2),
            new Coordinate(2, 2, 0),
        };
        var outerRingPart = new PatchPart(PatchType.OuterRing, outerRingCoords);
        var multiPatch = new MultiPatch(triangleStripPart, outerRingPart);
        Assert.Equal(ShapeType.MultiPatch, multiPatch.ShapeType);
        Assert.Equal(2, multiPatch.PartCount);
        Assert.Equal(9, multiPatch.CoordinateCount);
        Assert.False(multiPatch.IsEmpty);
    }

    [Fact]
    public void Constructor_WithEmptyParts_ShouldCreateEmptyMultiPatch()
    {
        var multiPatch = new MultiPatch();
        Assert.Equal(ShapeType.MultiPatch, multiPatch.ShapeType);
        Assert.Equal(0, multiPatch.PartCount);
        Assert.Equal(0, multiPatch.CoordinateCount);
        Assert.True(multiPatch.IsEmpty);
    }

    [Fact]
    public void Constructor_WithNullParts_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MultiPatch(null!));
    }

    [Fact]
    public void GetPart_ValidIndex_ShouldReturnCorrectPart()
    {
        var coords1 = new[] { new Coordinate(0, 0, 0), new Coordinate(1, 1, 1) };
        var coords2 = new[] { new Coordinate(2, 2, 2), new Coordinate(3, 3, 3) };
        var part1 = new PatchPart(PatchType.TriangleStrip, coords1);
        var part2 = new PatchPart(PatchType.OuterRing, coords2);
        var multiPatch = new MultiPatch(part1, part2);
        var retrievedPart1 = multiPatch.GetPart(0);
        var retrievedPart2 = multiPatch.GetPart(1);
        Assert.Equal(PatchType.TriangleStrip, retrievedPart1.PatchType);
        Assert.Equal(2, retrievedPart1.CoordinateCount);
        Assert.Equal(PatchType.OuterRing, retrievedPart2.PatchType);
        Assert.Equal(2, retrievedPart2.CoordinateCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void GetPart_InvalidIndex_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var coords = new[] { new Coordinate(0, 0, 0) };
        var part = new PatchPart(PatchType.TriangleStrip, coords);
        var multiPatch = new MultiPatch(part);
        Assert.Throws<ArgumentOutOfRangeException>(() => multiPatch.GetPart(index));
    }

    [Fact]
    public void GetParts_ShouldReturnAllParts()
    {
        var coords1 = new[] { new Coordinate(0, 0, 0) };
        var coords2 = new[] { new Coordinate(1, 1, 1) };
        var coords3 = new[] { new Coordinate(2, 2, 2) };
        var part1 = new PatchPart(PatchType.TriangleStrip, coords1);
        var part2 = new PatchPart(PatchType.TriangleFan, coords2);
        var part3 = new PatchPart(PatchType.OuterRing, coords3);
        var multiPatch = new MultiPatch(part1, part2, part3);
        var parts = multiPatch.GetParts().ToList();
        Assert.Equal(3, parts.Count);
        Assert.Equal(PatchType.TriangleStrip, parts[0].PatchType);
        Assert.Equal(PatchType.TriangleFan, parts[1].PatchType);
        Assert.Equal(PatchType.OuterRing, parts[2].PatchType);
    }

    [Fact]
    public void GetCoordinates_ShouldReturnAllCoordinates()
    {
        var coords1 = new[] { new Coordinate(0, 0, 0), new Coordinate(1, 1, 1) };
        var coords2 = new[] { new Coordinate(2, 2, 2) };
        var part1 = new PatchPart(PatchType.TriangleStrip, coords1);
        var part2 = new PatchPart(PatchType.OuterRing, coords2);
        var multiPatch = new MultiPatch(part1, part2);
        var allCoords = multiPatch.GetCoordinates().ToList();
        Assert.Equal(3, allCoords.Count);
        Assert.Equal(new Coordinate(0, 0, 0), allCoords[0]);
        Assert.Equal(new Coordinate(1, 1, 1), allCoords[1]);
        Assert.Equal(new Coordinate(2, 2, 2), allCoords[2]);
    }

    [Fact]
    public void BoundingBox_ShouldCalculateCorrectBounds()
    {
        var coords = new[]
        {
            new Coordinate(-1, -2, -3, 10),
            new Coordinate(3, 4, 5, 20),
            new Coordinate(0, 1, 2, 15),
        };
        var part = new PatchPart(PatchType.TriangleStrip, coords);
        var multiPatch = new MultiPatch(part);
        var bbox = multiPatch.BoundingBox;
        Assert.Equal(-1, bbox.MinX);
        Assert.Equal(-2, bbox.MinY);
        Assert.Equal(3, bbox.MaxX);
        Assert.Equal(4, bbox.MaxY);
        Assert.Equal(-3, bbox.MinZ);
        Assert.Equal(5, bbox.MaxZ);
        Assert.Equal(10, bbox.MinM);
        Assert.Equal(20, bbox.MaxM);
    }

    [Fact]
    public void BoundingBox_EmptyMultiPatch_ShouldReturnZeroBounds()
    {
        var multiPatch = new MultiPatch();
        var bbox = multiPatch.BoundingBox;
        Assert.Equal(0, bbox.MinX);
        Assert.Equal(0, bbox.MinY);
        Assert.Equal(0, bbox.MaxX);
        Assert.Equal(0, bbox.MaxY);
        Assert.Null(bbox.MinZ);
        Assert.Null(bbox.MaxZ);
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        var coords = new[] { new Coordinate(0, 0, 0), new Coordinate(1, 1, 1) };
        var part = new PatchPart(PatchType.TriangleStrip, coords);
        var multiPatch = new MultiPatch(part);
        var result = multiPatch.ToString();
        Assert.Contains("MultiPatch", result);
        Assert.Contains("1 parts", result);
        Assert.Contains("2 coordinates", result);
    }
}

public class PatchPartTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreatePatchPart()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, 0),
            new Coordinate(1, 1, 1),
            new Coordinate(2, 2, 2),
        };
        var part = new PatchPart(PatchType.TriangleStrip, coordinates);
        Assert.Equal(PatchType.TriangleStrip, part.PatchType);
        Assert.Equal(3, part.CoordinateCount);
        Assert.Equal(coordinates, part.Coordinates);
    }

    [Fact]
    public void Constructor_WithNullCoordinates_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PatchPart(PatchType.TriangleStrip, null!));
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        var coordinates = new[] { new Coordinate(0, 0, 0), new Coordinate(1, 1, 1) };
        var part = new PatchPart(PatchType.OuterRing, coordinates);
        var result = part.ToString();
        Assert.Contains("OuterRing", result);
        Assert.Contains("2 coordinates", result);
    }
}

public class PatchTypeExtensionsTests
{
    [Theory]
    [InlineData(PatchType.OuterRing, true)]
    [InlineData(PatchType.InnerRing, true)]
    [InlineData(PatchType.FirstRing, true)]
    [InlineData(PatchType.Ring, true)]
    [InlineData(PatchType.TriangleStrip, false)]
    [InlineData(PatchType.TriangleFan, false)]
    public void IsRing_ShouldReturnCorrectValue(PatchType patchType, bool expected)
    {
        Assert.Equal(expected, patchType.IsRing());
    }

    [Theory]
    [InlineData(PatchType.TriangleStrip, true)]
    [InlineData(PatchType.TriangleFan, true)]
    [InlineData(PatchType.OuterRing, false)]
    [InlineData(PatchType.InnerRing, false)]
    [InlineData(PatchType.FirstRing, false)]
    [InlineData(PatchType.Ring, false)]
    public void IsTriangle_ShouldReturnCorrectValue(PatchType patchType, bool expected)
    {
        Assert.Equal(expected, patchType.IsTriangle());
    }

    [Theory]
    [InlineData(PatchType.TriangleStrip, "Triangle Strip")]
    [InlineData(PatchType.TriangleFan, "Triangle Fan")]
    [InlineData(PatchType.OuterRing, "Outer Ring")]
    [InlineData(PatchType.InnerRing, "Inner Ring")]
    [InlineData(PatchType.FirstRing, "First Ring")]
    [InlineData(PatchType.Ring, "Ring")]
    public void GetDescription_ShouldReturnCorrectDescription(PatchType patchType, string expected)
    {
        Assert.Equal(expected, patchType.GetDescription());
    }

    [Fact]
    public void GetDescription_UnknownType_ShouldReturnUnknownFormat()
    {
        var unknownType = (PatchType)999;
        var result = unknownType.GetDescription();
        Assert.Equal("Unknown (999)", result);
    }
}
