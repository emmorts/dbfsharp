using DbfSharp.Core.Geometry;
using DbfSharp.Core.Spatial;
using Xunit;

namespace DbfSharp.Tests.Spatial;

public class SpatialOperationsTests
{
    [Fact]
    public void Intersects_PointAndPoint_SameLocation_ShouldReturnTrue()
    {
        // Arrange
        var point1 = new Point(new Coordinate(10, 20));
        var point2 = new Point(new Coordinate(10, 20));

        // Act & Assert
        Assert.True(SpatialOperations.Intersects(point1, point2));
    }

    [Fact]
    public void Intersects_PointAndPoint_DifferentLocation_ShouldReturnFalse()
    {
        // Arrange
        var point1 = new Point(new Coordinate(10, 20));
        var point2 = new Point(new Coordinate(15, 25));

        // Act & Assert
        Assert.False(SpatialOperations.Intersects(point1, point2));
    }

    [Fact]
    public void Contains_PolygonAndPoint_PointInside_ShouldReturnTrue()
    {
        // Arrange
        var polygon = CreateSquarePolygon(0, 0, 10, 10);
        var point = new Point(new Coordinate(5, 5));

        // Act & Assert
        Assert.True(SpatialOperations.Contains(polygon, point));
    }

    [Fact]
    public void Contains_PolygonAndPoint_PointOutside_ShouldReturnFalse()
    {
        // Arrange
        var polygon = CreateSquarePolygon(0, 0, 10, 10);
        var point = new Point(new Coordinate(15, 15));

        // Act & Assert
        Assert.False(SpatialOperations.Contains(polygon, point));
    }

    [Fact]
    public void Contains_PolygonAndPoint_PointOnBoundary_ShouldReturnTrue()
    {
        // Arrange
        var polygon = CreateSquarePolygon(0, 0, 10, 10);
        var point = new Point(new Coordinate(0, 5)); // On left edge

        // Act & Assert
        Assert.True(SpatialOperations.Contains(polygon, point));
    }

    [Fact]
    public void Within_PointAndPolygon_PointInside_ShouldReturnTrue()
    {
        // Arrange
        var point = new Point(new Coordinate(5, 5));
        var polygon = CreateSquarePolygon(0, 0, 10, 10);

        // Act & Assert
        Assert.True(SpatialOperations.Within(point, polygon));
    }

    [Fact]
    public void Distance_TwoPoints_ShouldCalculateCorrectly()
    {
        // Arrange
        var point1 = new Point(new Coordinate(0, 0));
        var point2 = new Point(new Coordinate(3, 4));
        var expectedDistance = 5.0; // Pythagorean: 3-4-5 triangle

        // Act
        var distance = SpatialOperations.Distance(point1, point2);

        // Assert
        Assert.Equal(expectedDistance, distance, precision: 6);
    }

    [Fact]
    public void Distance_IntersectingShapes_ShouldReturnZero()
    {
        // Arrange
        var point = new Point(new Coordinate(5, 5));
        var polygon = CreateSquarePolygon(0, 0, 10, 10);

        // Act
        var distance = SpatialOperations.Distance(point, polygon);

        // Assert
        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void GetRelationship_PointInPolygon_ShouldReturnContains()
    {
        // Arrange
        var polygon = CreateSquarePolygon(0, 0, 10, 10);
        var point = new Point(new Coordinate(5, 5));

        // Act
        var relationship = SpatialOperations.GetRelationship(polygon, point);

        // Assert
        Assert.Equal(SpatialRelationship.Contains, relationship);
    }

    [Fact]
    public void GetRelationship_PointInPolygon_ShouldReturnWithin()
    {
        // Arrange
        var point = new Point(new Coordinate(5, 5));
        var polygon = CreateSquarePolygon(0, 0, 10, 10);

        // Act
        var relationship = SpatialOperations.GetRelationship(point, polygon);

        // Assert
        Assert.Equal(SpatialRelationship.Within, relationship);
    }

    [Fact]
    public void GetRelationship_NonIntersectingShapes_ShouldReturnDisjoint()
    {
        // Arrange
        var polygon1 = CreateSquarePolygon(0, 0, 5, 5);
        var polygon2 = CreateSquarePolygon(10, 10, 15, 15);

        // Act
        var relationship = SpatialOperations.GetRelationship(polygon1, polygon2);

        // Assert
        Assert.Equal(SpatialRelationship.Disjoint, relationship);
    }

    [Fact]
    public void GetRelationship_IdenticalShapes_ShouldReturnEqual()
    {
        // Arrange
        var polygon1 = CreateSquarePolygon(0, 0, 10, 10);
        var polygon2 = CreateSquarePolygon(0, 0, 10, 10);

        // Act
        var relationship = SpatialOperations.GetRelationship(polygon1, polygon2);

        // Assert
        Assert.Equal(SpatialRelationship.Equal, relationship);
    }

    [Fact]
    public void SpatialExtensions_Intersects_ShouldWorkCorrectly()
    {
        // Arrange
        var point1 = new Point(new Coordinate(5, 5));
        var polygon = CreateSquarePolygon(0, 0, 10, 10);

        // Act & Assert
        Assert.True(point1.Intersects(polygon));
        Assert.True(polygon.Intersects(point1));
    }

    [Fact]
    public void SpatialExtensions_Contains_ShouldWorkCorrectly()
    {
        // Arrange
        var point = new Point(new Coordinate(5, 5));
        var polygon = CreateSquarePolygon(0, 0, 10, 10);

        // Act & Assert
        Assert.True(polygon.Contains(point));
        Assert.False(point.Contains(polygon));
    }

    [Fact]
    public void SpatialExtensions_ContainsCoordinate_ShouldWorkCorrectly()
    {
        // Arrange
        var polygon = CreateSquarePolygon(0, 0, 10, 10);
        var coordinate = new Coordinate(5, 5);

        // Act & Assert
        Assert.True(polygon.Contains(coordinate));
    }

    [Fact]
    public void SpatialExtensions_DistanceTo_ShouldWorkCorrectly()
    {
        // Arrange
        var point1 = new Point(new Coordinate(0, 0));
        var point2 = new Point(new Coordinate(3, 4));
        var expectedDistance = 5.0;

        // Act
        var distance = point1.DistanceTo(point2);

        // Assert
        Assert.Equal(expectedDistance, distance, precision: 6);
    }

    [Fact]
    public void SpatialExtensions_GetRelationshipTo_ShouldWorkCorrectly()
    {
        // Arrange
        var point = new Point(new Coordinate(5, 5));
        var polygon = CreateSquarePolygon(0, 0, 10, 10);

        // Act
        var relationship = point.GetRelationshipTo(polygon);

        // Assert
        Assert.Equal(SpatialRelationship.Within, relationship);
    }

    [Theory]
    [InlineData(1, 1, true)]   // Inside
    [InlineData(0, 0, true)]   // On corner
    [InlineData(5, 0, true)]   // On edge
    [InlineData(0, 5, true)]   // On edge
    [InlineData(-1, 5, false)] // Outside left
    [InlineData(11, 5, false)] // Outside right
    [InlineData(5, -1, false)] // Outside bottom
    [InlineData(5, 11, false)] // Outside top
    public void IsPointInPolygon_VariousLocations_ShouldReturnExpectedResults(double x, double y, bool expected)
    {
        // Arrange
        var polygon = CreateSquarePolygon(0, 0, 10, 10);
        var point = new Point(new Coordinate(x, y));

        // Act
        var result = SpatialOperations.Contains(polygon, point);

        // Assert
        Assert.Equal(expected, result);
    }

    private static Polygon CreateSquarePolygon(double minX, double minY, double maxX, double maxY)
    {
        var coordinates = new[]
        {
            new Coordinate(minX, minY),  // Bottom-left
            new Coordinate(maxX, minY),  // Bottom-right
            new Coordinate(maxX, maxY),  // Top-right
            new Coordinate(minX, maxY),  // Top-left
            new Coordinate(minX, minY)   // Close the ring
        };

        return new Polygon(coordinates);
    }
}
