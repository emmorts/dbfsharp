using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Geometry;

public class BoundingBoxTests
{
    [Fact]
    public void Constructor_2D_ShouldCreateValidBoundingBox()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0);
        Assert.Equal(1.0, bbox.MinX);
        Assert.Equal(2.0, bbox.MinY);
        Assert.Equal(5.0, bbox.MaxX);
        Assert.Equal(8.0, bbox.MaxY);
        Assert.Null(bbox.MinZ);
        Assert.Null(bbox.MaxZ);
        Assert.Null(bbox.MinM);
        Assert.Null(bbox.MaxM);
        Assert.False(bbox.HasZ);
        Assert.False(bbox.HasM);
    }

    [Fact]
    public void Constructor_2D_ShouldSwapMinMaxIfReversed()
    {
        var bbox = new BoundingBox(5.0, 8.0, 1.0, 2.0);

        // Assert - Should automatically correct the order
        Assert.Equal(1.0, bbox.MinX);
        Assert.Equal(2.0, bbox.MinY);
        Assert.Equal(5.0, bbox.MaxX);
        Assert.Equal(8.0, bbox.MaxY);
    }

    [Fact]
    public void Constructor_3D_ShouldCreateValidBoundingBox()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0);
        Assert.Equal(1.0, bbox.MinX);
        Assert.Equal(2.0, bbox.MinY);
        Assert.Equal(5.0, bbox.MaxX);
        Assert.Equal(8.0, bbox.MaxY);
        Assert.Equal(10.0, bbox.MinZ);
        Assert.Equal(20.0, bbox.MaxZ);
        Assert.Null(bbox.MinM);
        Assert.Null(bbox.MaxM);
        Assert.True(bbox.HasZ);
        Assert.False(bbox.HasM);
    }

    [Fact]
    public void Constructor_3D_ShouldSwapZMinMaxIfReversed()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0, 20.0, 10.0);
        Assert.Equal(10.0, bbox.MinZ);
        Assert.Equal(20.0, bbox.MaxZ);
    }

    [Fact]
    public void Constructor_Full_ShouldCreateValidBoundingBox()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0, 100.0, 200.0);
        Assert.Equal(1.0, bbox.MinX);
        Assert.Equal(2.0, bbox.MinY);
        Assert.Equal(5.0, bbox.MaxX);
        Assert.Equal(8.0, bbox.MaxY);
        Assert.Equal(10.0, bbox.MinZ);
        Assert.Equal(20.0, bbox.MaxZ);
        Assert.Equal(100.0, bbox.MinM);
        Assert.Equal(200.0, bbox.MaxM);
        Assert.True(bbox.HasZ);
        Assert.True(bbox.HasM);
    }

    [Fact]
    public void Constructor_WithNullableValues_ShouldHandleNulls()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0, null, null, 100.0, 200.0);
        Assert.Null(bbox.MinZ);
        Assert.Null(bbox.MaxZ);
        Assert.Equal(100.0, bbox.MinM);
        Assert.Equal(200.0, bbox.MaxM);
        Assert.False(bbox.HasZ);
        Assert.True(bbox.HasM);
    }

    [Theory]
    [InlineData(1.0, 2.0, 5.0, 8.0, 4.0)]
    [InlineData(0.0, 0.0, 10.0, 5.0, 10.0)]
    [InlineData(-5.0, -3.0, 5.0, 3.0, 10.0)]
    public void Width_ShouldCalculateCorrectly(
        double minX,
        double minY,
        double maxX,
        double maxY,
        double expectedWidth
    )
    {
        var bbox = new BoundingBox(minX, minY, maxX, maxY);
        Assert.Equal(expectedWidth, bbox.Width);
    }

    [Theory]
    [InlineData(1.0, 2.0, 5.0, 8.0, 6.0)]
    [InlineData(0.0, 0.0, 10.0, 5.0, 5.0)]
    [InlineData(-5.0, -3.0, 5.0, 3.0, 6.0)]
    public void Height_ShouldCalculateCorrectly(
        double minX,
        double minY,
        double maxX,
        double maxY,
        double expectedHeight
    )
    {
        var bbox = new BoundingBox(minX, minY, maxX, maxY);
        Assert.Equal(expectedHeight, bbox.Height);
    }

    [Theory]
    [InlineData(10.0, 20.0, 10.0)]
    [InlineData(0.0, 15.0, 15.0)]
    [InlineData(-5.0, 5.0, 10.0)]
    public void Depth_WithZValues_ShouldCalculateCorrectly(
        double minZ,
        double maxZ,
        double expectedDepth
    )
    {
        var bbox = new BoundingBox(0.0, 0.0, 1.0, 1.0, minZ, maxZ);
        Assert.Equal(expectedDepth, bbox.Depth);
    }

    [Fact]
    public void Depth_WithoutZValues_ShouldReturnNull()
    {
        var bbox = new BoundingBox(0.0, 0.0, 1.0, 1.0);
        Assert.Null(bbox.Depth);
    }

    [Theory]
    [InlineData(1.0, 2.0, 5.0, 8.0, 24.0)]
    [InlineData(0.0, 0.0, 10.0, 5.0, 50.0)]
    [InlineData(-5.0, -3.0, 5.0, 3.0, 60.0)]
    public void Area_ShouldCalculateCorrectly(
        double minX,
        double minY,
        double maxX,
        double maxY,
        double expectedArea
    )
    {
        var bbox = new BoundingBox(minX, minY, maxX, maxY);
        Assert.Equal(expectedArea, bbox.Area);
    }

    [Fact]
    public void Area_EmptyBoundingBox_ShouldReturnZero()
    {
        var bbox = new BoundingBox(5.0, 5.0, 5.0, 5.0); // Zero area
        Assert.Equal(0.0, bbox.Area);
    }

    [Fact]
    public void Center_ShouldCalculateCorrectly()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0, 100.0, 200.0);
        var center = bbox.Center;
        Assert.Equal(3.0, center.X); // (1 + 5) / 2
        Assert.Equal(5.0, center.Y); // (2 + 8) / 2
        Assert.Equal(15.0, center.Z); // (10 + 20) / 2
        Assert.Equal(150.0, center.M); // (100 + 200) / 2
    }

    [Fact]
    public void Center_2D_ShouldCalculateCorrectly()
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0);
        var center = bbox.Center;
        Assert.Equal(3.0, center.X);
        Assert.Equal(5.0, center.Y);
        Assert.Null(center.Z);
        Assert.Null(center.M);
    }

    [Theory]
    [InlineData(3.0, 5.0, true)] // Center point
    [InlineData(1.0, 2.0, true)] // Min corner
    [InlineData(5.0, 8.0, true)] // Max corner
    [InlineData(0.0, 0.0, false)] // Outside
    [InlineData(6.0, 9.0, false)] // Outside
    public void Contains_2D_ShouldReturnCorrectResult(double x, double y, bool expected)
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0);
        var coordinate = new Coordinate(x, y);
        Assert.Equal(expected, bbox.Contains(coordinate));
    }

    [Theory]
    [InlineData(3.0, 5.0, 15.0, true)] // Center point with Z
    [InlineData(3.0, 5.0, 5.0, false)] // Below Z range
    [InlineData(3.0, 5.0, 25.0, false)] // Above Z range
    public void Contains_3D_ShouldReturnCorrectResult(double x, double y, double z, bool expected)
    {
        var bbox = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0);
        var coordinate = new Coordinate(x, y, z);
        Assert.Equal(expected, bbox.Contains(coordinate));
    }

    [Fact]
    public void Contains_MixedDimensions_ShouldWorkCorrectly()
    {
        var bbox3D = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0);
        var coordinate2D = new Coordinate(3.0, 5.0); // No Z value

        // Act & Assert - Should contain since Z is optional
        Assert.True(bbox3D.Contains(coordinate2D));
    }

    [Fact]
    public void Intersects_OverlappingBoxes_ShouldReturnTrue()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 5.0, 8.0);
        var bbox2 = new BoundingBox(3.0, 4.0, 7.0, 10.0);
        Assert.True(bbox1.Intersects(bbox2));
        Assert.True(bbox2.Intersects(bbox1)); // Should be symmetric
    }

    [Fact]
    public void Intersects_NonOverlappingBoxes_ShouldReturnFalse()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 3.0, 4.0);
        var bbox2 = new BoundingBox(5.0, 6.0, 7.0, 8.0);
        Assert.False(bbox1.Intersects(bbox2));
        Assert.False(bbox2.Intersects(bbox1));
    }

    [Fact]
    public void Intersects_TouchingBoxes_ShouldReturnTrue()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 3.0, 4.0);
        var bbox2 = new BoundingBox(3.0, 2.0, 5.0, 4.0); // Touching at X=3
        Assert.True(bbox1.Intersects(bbox2));
    }

    [Fact]
    public void Intersects_3D_ShouldCheckZDimension()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 15.0);
        var bbox2 = new BoundingBox(3.0, 4.0, 7.0, 10.0, 20.0, 25.0); // No Z overlap
        Assert.False(bbox1.Intersects(bbox2)); // Should not intersect due to Z separation
    }

    [Fact]
    public void Union_ShouldCreateExpandedBoundingBox()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 3.0, 4.0);
        var bbox2 = new BoundingBox(2.0, 3.0, 5.0, 6.0);
        var union = bbox1.Union(bbox2);
        Assert.Equal(1.0, union.MinX);
        Assert.Equal(2.0, union.MinY);
        Assert.Equal(5.0, union.MaxX);
        Assert.Equal(6.0, union.MaxY);
    }

    [Fact]
    public void Union_3D_ShouldHandleZDimensions()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 3.0, 4.0, 10.0, 15.0);
        var bbox2 = new BoundingBox(2.0, 3.0, 5.0, 6.0, 12.0, 20.0);
        var union = bbox1.Union(bbox2);
        Assert.Equal(1.0, union.MinX);
        Assert.Equal(2.0, union.MinY);
        Assert.Equal(5.0, union.MaxX);
        Assert.Equal(6.0, union.MaxY);
        Assert.Equal(10.0, union.MinZ);
        Assert.Equal(20.0, union.MaxZ);
    }

    [Fact]
    public void Union_MixedDimensions_ShouldHandleCorrectly()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 3.0, 4.0, 10.0, 15.0); // Has Z
        var bbox2 = new BoundingBox(2.0, 3.0, 5.0, 6.0); // No Z
        var union = bbox1.Union(bbox2);
        Assert.Equal(1.0, union.MinX);
        Assert.Equal(2.0, union.MinY);
        Assert.Equal(5.0, union.MaxX);
        Assert.Equal(6.0, union.MaxY);
        Assert.Null(union.MinZ); // Should lose Z when unioning with 2D box
        Assert.Null(union.MaxZ);
    }

    [Theory]
    [InlineData(5.0, 5.0, 5.0, 5.0, true)] // Point
    [InlineData(5.0, 5.0, 4.0, 5.0, true)] // Zero width
    [InlineData(5.0, 5.0, 5.0, 4.0, true)] // Zero height
    [InlineData(1.0, 2.0, 5.0, 8.0, false)] // Normal box
    public void IsEmpty_ShouldReturnCorrectResult(
        double minX,
        double minY,
        double maxX,
        double maxY,
        bool expected
    )
    {
        var bbox = new BoundingBox(minX, minY, maxX, maxY);
        Assert.Equal(expected, bbox.IsEmpty);
    }

    [Fact]
    public void Equals_SameBoundingBoxes_ShouldReturnTrue()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0, 100.0, 200.0);
        var bbox2 = new BoundingBox(1.0, 2.0, 5.0, 8.0, 10.0, 20.0, 100.0, 200.0);
        Assert.True(bbox1.Equals(bbox2));
        Assert.Equal(bbox1.GetHashCode(), bbox2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentBoundingBoxes_ShouldReturnFalse()
    {
        var bbox1 = new BoundingBox(1.0, 2.0, 5.0, 8.0);
        var bbox2 = new BoundingBox(1.1, 2.0, 5.0, 8.0); // Slightly different
        Assert.False(bbox1.Equals(bbox2));
    }
}
