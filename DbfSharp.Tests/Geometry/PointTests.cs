using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Geometry;

public class PointTests
{
    [Fact]
    public void Constructor_WithCoordinate_ShouldCreateValidPoint()
    {
        var coordinate = new Coordinate(123.456, 789.012);
        var point = new Point(coordinate);
        Assert.Equal(coordinate, point.Coordinate);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Null(point.Z);
        Assert.Null(point.M);
    }

    [Fact]
    public void Constructor_2D_ShouldCreateValidPoint()
    {
        var point = new Point(123.456, 789.012);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Null(point.Z);
        Assert.Null(point.M);
        Assert.Equal(ShapeType.Point, point.ShapeType);
    }

    [Fact]
    public void Constructor_3D_ShouldCreateValidPoint()
    {
        var point = new Point(123.456, 789.012, 345.678);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Equal(345.678, point.Z);
        Assert.Null(point.M);
        Assert.Equal(ShapeType.PointZ, point.ShapeType);
    }

    [Fact]
    public void Constructor_WithMeasure_ShouldCreateValidPoint()
    {
        var point = new Point(123.456, 789.012, null, 901.234);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Null(point.Z);
        Assert.Equal(901.234, point.M);
        Assert.Equal(ShapeType.PointM, point.ShapeType);
    }

    [Fact]
    public void Constructor_4D_ShouldPrioritizeZOverM()
    {
        var point = new Point(123.456, 789.012, 345.678, 901.234);
        Assert.Equal(123.456, point.X);
        Assert.Equal(789.012, point.Y);
        Assert.Equal(345.678, point.Z);
        Assert.Equal(901.234, point.M);
        Assert.Equal(ShapeType.PointZ, point.ShapeType); // Should prioritize Z according to shapefile spec
    }

    [Fact]
    public void BoundingBox_ShouldReturnPointBounds()
    {
        var point = new Point(123.456, 789.012, 345.678, 901.234);
        var bbox = point.BoundingBox;
        Assert.Equal(123.456, bbox.MinX);
        Assert.Equal(789.012, bbox.MinY);
        Assert.Equal(123.456, bbox.MaxX);
        Assert.Equal(789.012, bbox.MaxY);
        Assert.Equal(345.678, bbox.MinZ);
        Assert.Equal(345.678, bbox.MaxZ);
        Assert.Equal(901.234, bbox.MinM);
        Assert.Equal(901.234, bbox.MaxM);
    }

    [Fact]
    public void IsEmpty_ShouldAlwaysReturnFalse()
    {
        var point2D = new Point(123.456, 789.012);
        var point3D = new Point(123.456, 789.012, 345.678);
        Assert.False(point2D.IsEmpty);
        Assert.False(point3D.IsEmpty);
    }

    [Fact]
    public void GetCoordinates_ShouldReturnSingleCoordinate()
    {
        var coordinate = new Coordinate(123.456, 789.012, 345.678, 901.234);
        var point = new Point(coordinate);
        var coordinates = point.GetCoordinates().ToList();
        Assert.Single(coordinates);
        Assert.Equal(coordinate, coordinates[0]);
    }

    [Fact]
    public void CoordinateCount_ShouldReturnOne()
    {
        var point = new Point(123.456, 789.012);
        Assert.Equal(1, point.CoordinateCount);
    }

    [Theory]
    [InlineData(0, 0, 3, 4, 5.0)]
    [InlineData(1, 1, 4, 5, 5.0)]
    [InlineData(-1, -1, 2, 2, 4.242640687119285)]
    public void DistanceTo_ShouldCalculateCorrectDistance(
        double x1,
        double y1,
        double x2,
        double y2,
        double expectedDistance
    )
    {
        var point1 = new Point(x1, y1);
        var point2 = new Point(x2, y2);
        var distance = point1.DistanceTo(point2);
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
        var point1 = new Point(x1, y1, z1);
        var point2 = new Point(x2, y2, z2);
        var distance = point1.Distance3DTo(point2);
        Assert.Equal(expectedDistance, distance, precision: 10);
    }

    [Fact]
    public void Transform_ShouldApplyTransformation()
    {
        var point = new Point(123.456, 789.012, 345.678);
        Func<Coordinate, Coordinate> transform = coord => new Coordinate(
            coord.X + 10,
            coord.Y + 20,
            coord.HasZ ? coord.Z + 30 : null,
            coord.M
        );
        var transformedShape = point.Transform(transform);
        var transformedPoint = (Point)transformedShape;
        Assert.Equal(133.456, transformedPoint.X, precision: 10);
        Assert.Equal(809.012, transformedPoint.Y, precision: 10);
        Assert.Equal(375.678, transformedPoint.Z!.Value, precision: 10);
    }

    [Fact]
    public void Transform_WithNullTransform_ShouldThrowArgumentNullException()
    {
        var point = new Point(123.456, 789.012);
        Assert.Throws<ArgumentNullException>(() => point.Transform(null!));
    }

    [Fact]
    public void Offset_2D_ShouldCreateOffsetPoint()
    {
        var point = new Point(123.456, 789.012);
        var offsetPoint = point.Offset(10.0, 20.0);
        Assert.Equal(133.456, offsetPoint.X, precision: 10);
        Assert.Equal(809.012, offsetPoint.Y, precision: 10);
        Assert.Null(offsetPoint.Z);
    }

    [Fact]
    public void Offset_3D_ShouldCreateOffsetPoint()
    {
        var point = new Point(123.456, 789.012, 345.678);
        var offsetPoint = point.Offset(10.0, 20.0, 30.0);
        Assert.Equal(133.456, offsetPoint.X, precision: 10);
        Assert.Equal(809.012, offsetPoint.Y, precision: 10);
        Assert.Equal(375.678, offsetPoint.Z!.Value, precision: 10);
    }

    [Fact]
    public void Offset_WithoutZDelta_ShouldPreserveMeasure()
    {
        var point = new Point(123.456, 789.012, 345.678, 901.234);
        var offsetPoint = point.Offset(10.0, 20.0); // Don't specify Z delta
        Assert.Equal(133.456, offsetPoint.X, precision: 10);
        Assert.Equal(809.012, offsetPoint.Y, precision: 10);
        Assert.Equal(345.678, offsetPoint.Z!.Value, precision: 10); // Z should be preserved
        Assert.Equal(901.234, offsetPoint.M!.Value, precision: 10); // M should be preserved
    }

    [Fact]
    public void IsValid_ShouldAlwaysReturnTrue()
    {
        var point = new Point(123.456, 789.012);
        Assert.True(point.IsValid());
    }

    [Fact]
    public void GetValidationErrors_ValidPoint_ShouldReturnEmpty()
    {
        var point = new Point(123.456, 789.012, 345.678, 901.234);
        var errors = point.GetValidationErrors().ToList();
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(double.NaN, 0, "Point X coordinate is not a valid number")]
    [InlineData(0, double.NaN, "Point Y coordinate is not a valid number")]
    [InlineData(double.PositiveInfinity, 0, "Point X coordinate is not a valid number")]
    [InlineData(0, double.NegativeInfinity, "Point Y coordinate is not a valid number")]
    public void GetValidationErrors_InvalidCoordinates_ShouldReturnErrors(
        double x,
        double y,
        string expectedError
    )
    {
        var point = new Point(x, y);
        var errors = point.GetValidationErrors().ToList();
        Assert.Contains(expectedError, errors);
    }

    [Fact]
    public void GetValidationErrors_InvalidZCoordinate_ShouldReturnError()
    {
        var point = new Point(123.456, 789.012, double.NaN);
        var errors = point.GetValidationErrors().ToList();
        Assert.Contains("Point Z coordinate is not a valid number", errors);
    }

    [Fact]
    public void GetValidationErrors_InvalidMCoordinate_ShouldReturnError()
    {
        var point = new Point(123.456, 789.012, null, double.PositiveInfinity);
        var errors = point.GetValidationErrors().ToList();
        Assert.Contains("Point M coordinate is not a valid number", errors);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var point = new Point(123.456789, 789.012345);
        var result = point.ToString();
        Assert.Equal("POINT (123.456789, 789.012345)", result);
    }

    [Fact]
    public void Equals_SamePoints_ShouldReturnTrue()
    {
        var point1 = new Point(123.456, 789.012, 345.678, 901.234);
        var point2 = new Point(123.456, 789.012, 345.678, 901.234);
        Assert.True(point1.Equals(point2));
        Assert.Equal(point1.GetHashCode(), point2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentPoints_ShouldReturnFalse()
    {
        var point1 = new Point(123.456, 789.012);
        var point2 = new Point(123.457, 789.012); // Slightly different X
        Assert.False(point1.Equals(point2));
    }

    [Fact]
    public void Equals_WithNonPoint_ShouldReturnFalse()
    {
        var point = new Point(123.456, 789.012);
        var notAPoint = "not a point";
        Assert.False(point.Equals(notAPoint));
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var point = new Point(123.456, 789.012);
        Assert.False(point.Equals(null));
    }
}
