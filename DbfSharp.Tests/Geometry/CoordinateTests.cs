using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Geometry;

public class CoordinateTests
{
    [Fact]
    public void Constructor_2D_ShouldCreateValidCoordinate()
    {
        var coordinate = new Coordinate(123.456, 789.012);
        Assert.Equal(123.456, coordinate.X);
        Assert.Equal(789.012, coordinate.Y);
        Assert.Null(coordinate.Z);
        Assert.Null(coordinate.M);
        Assert.False(coordinate.HasZ);
        Assert.False(coordinate.HasM);
        Assert.False(coordinate.Is3D);
        Assert.False(coordinate.IsMeasured);
    }

    [Fact]
    public void Constructor_3D_ShouldCreateValidCoordinate()
    {
        var coordinate = new Coordinate(123.456, 789.012, 345.678);
        Assert.Equal(123.456, coordinate.X);
        Assert.Equal(789.012, coordinate.Y);
        Assert.Equal(345.678, coordinate.Z);
        Assert.Null(coordinate.M);
        Assert.True(coordinate.HasZ);
        Assert.False(coordinate.HasM);
        Assert.True(coordinate.Is3D);
        Assert.False(coordinate.IsMeasured);
    }

    [Fact]
    public void Constructor_4D_ShouldCreateValidCoordinate()
    {
        var coordinate = new Coordinate(123.456, 789.012, 345.678, 901.234);
        Assert.Equal(123.456, coordinate.X);
        Assert.Equal(789.012, coordinate.Y);
        Assert.Equal(345.678, coordinate.Z);
        Assert.Equal(901.234, coordinate.M);
        Assert.True(coordinate.HasZ);
        Assert.True(coordinate.HasM);
        Assert.True(coordinate.Is3D);
        Assert.True(coordinate.IsMeasured);
    }

    [Fact]
    public void Constructor_WithNullableValues_ShouldHandleNulls()
    {
        var coordinate = new Coordinate(123.456, 789.012, null, 901.234);
        Assert.Equal(123.456, coordinate.X);
        Assert.Equal(789.012, coordinate.Y);
        Assert.Null(coordinate.Z);
        Assert.Equal(901.234, coordinate.M);
        Assert.False(coordinate.HasZ);
        Assert.True(coordinate.HasM);
        Assert.False(coordinate.Is3D);
        Assert.True(coordinate.IsMeasured);
    }

    [Theory]
    [InlineData(0, 0, 3, 4, 5.0)]
    [InlineData(1, 1, 4, 5, 5.0)]
    [InlineData(-1, -1, 2, 2, 4.242640687119285)]
    public void DistanceTo_2D_ShouldCalculateCorrectDistance(
        double x1,
        double y1,
        double x2,
        double y2,
        double expectedDistance
    )
    {
        var coord1 = new Coordinate(x1, y1);
        var coord2 = new Coordinate(x2, y2);
        var distance = coord1.DistanceTo(coord2);
        Assert.Equal(expectedDistance, distance, precision: 10);
    }

    [Theory]
    [InlineData(0, 0, 0, 3, 4, 5, 7.0710678118654755)]
    [InlineData(1, 1, 1, 4, 5, 6, 7.0710678118654755)]
    public void Distance3DTo_ShouldCalculateCorrect3DDistance(
        double x1,
        double y1,
        double z1,
        double x2,
        double y2,
        double z2,
        double expectedDistance
    )
    {
        var coord1 = new Coordinate(x1, y1, z1);
        var coord2 = new Coordinate(x2, y2, z2);
        var distance = coord1.Distance3DTo(coord2);
        Assert.Equal(expectedDistance, distance, precision: 10);
    }

    [Fact]
    public void Distance3DTo_WithMissingZValues_ShouldFallbackTo2D()
    {
        var coord1 = new Coordinate(0, 0); // No Z
        var coord2 = new Coordinate(3, 4, 5); // Has Z
        var distance = coord1.Distance3DTo(coord2);
        Assert.Equal(5.0, distance); // Should be 2D distance only
    }

    [Fact]
    public void Equals_SameCoordinates_ShouldReturnTrue()
    {
        var coord1 = new Coordinate(123.456, 789.012, 345.678, 901.234);
        var coord2 = new Coordinate(123.456, 789.012, 345.678, 901.234);
        Assert.True(coord1.Equals(coord2));
        Assert.True(coord1 == coord2);
        Assert.False(coord1 != coord2);
        Assert.Equal(coord1.GetHashCode(), coord2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentCoordinates_ShouldReturnFalse()
    {
        var coord1 = new Coordinate(123.456, 789.012);
        var coord2 = new Coordinate(123.457, 789.012); // Slightly different X
        Assert.False(coord1.Equals(coord2));
        Assert.False(coord1 == coord2);
        Assert.True(coord1 != coord2);
    }

    [Fact]
    public void Equals_DifferentNullableValues_ShouldReturnFalse()
    {
        var coord1 = new Coordinate(123.456, 789.012, 345.678, null);
        var coord2 = new Coordinate(123.456, 789.012, 345.678, 901.234);
        Assert.False(coord1.Equals(coord2));
    }

    [Fact]
    public void ToString_2D_ShouldFormatCorrectly()
    {
        var coordinate = new Coordinate(123.456789, 789.012345);
        var result = coordinate.ToString();
        Assert.Equal("(123.456789, 789.012345)", result);
    }

    [Fact]
    public void ToString_3D_ShouldFormatCorrectly()
    {
        var coordinate = new Coordinate(123.456789, 789.012345, 345.678901);
        var result = coordinate.ToString();
        Assert.Equal("(123.456789, 789.012345, 345.678901)", result);
    }

    [Fact]
    public void ToString_WithMeasure_ShouldFormatCorrectly()
    {
        var coordinate = new Coordinate(123.456789, 789.012345, null, 901.234567);
        var result = coordinate.ToString();
        Assert.Equal("(123.456789, 789.012345, M:901.234567)", result);
    }

    [Fact]
    public void ToString_4D_ShouldFormatCorrectly()
    {
        var coordinate = new Coordinate(123.456789, 789.012345, 345.678901, 901.234567);
        var result = coordinate.ToString();
        Assert.Equal("(123.456789, 789.012345, 345.678901, M:901.234567)", result);
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(0, double.NegativeInfinity)]
    public void Constructor_WithInvalidValues_ShouldStillCreate(double x, double y)
    {
        var coordinate = new Coordinate(x, y);

        // Assert - Constructor should not throw, validation happens elsewhere
        Assert.Equal(x, coordinate.X);
        Assert.Equal(y, coordinate.Y);
    }

    [Fact]
    public void Equals_WithObject_ShouldWorkCorrectly()
    {
        var coord = new Coordinate(123.456, 789.012);
        object coordObj = new Coordinate(123.456, 789.012);
        object differentObj = "not a coordinate";
        Assert.True(coord.Equals(coordObj));
        Assert.False(coord.Equals(differentObj));
        Assert.False(coord.Equals(null));
    }
}
