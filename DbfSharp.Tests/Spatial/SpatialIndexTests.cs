using DbfSharp.Core.Geometry;
using DbfSharp.Core.Spatial;
using Xunit;

namespace DbfSharp.Tests.Spatial;

public class SpatialIndexTests
{
    [Fact]
    public void Constructor_DefaultParameters_ShouldInitializeCorrectly()
    {
        // Act
        var index = new SpatialIndex();

        // Assert
        Assert.Equal(0, index.Count);
        Assert.True(index.IsEmpty);
        Assert.Null(index.BoundingBox);
    }

    [Fact]
    public void Constructor_CustomParameters_ShouldInitializeCorrectly()
    {
        // Act
        var index = new SpatialIndex(8, 2);

        // Assert
        Assert.Equal(0, index.Count);
        Assert.True(index.IsEmpty);
    }

    [Theory]
    [InlineData(-1, 4)] // maxEntries < 2
    [InlineData(1, 4)] // maxEntries < 2
    [InlineData(4, 0)] // minEntries < 1
    [InlineData(4, -1)] // minEntries < 1
    [InlineData(4, 3)] // minEntries > maxEntries / 2
    public void Constructor_InvalidParameters_ShouldThrowException(int maxEntries, int minEntries)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SpatialIndex(maxEntries, minEntries));
    }

    [Fact]
    public void Insert_SinglePoint_ShouldAddSuccessfully()
    {
        // Arrange
        var index = new SpatialIndex();
        var point = new Point(new Coordinate(10, 20));
        var entry = RTreeEntry.FromShape(point, 1);

        // Act
        index.Insert(entry);

        // Assert
        Assert.Equal(1, index.Count);
        Assert.False(index.IsEmpty);
        Assert.NotNull(index.BoundingBox);
    }

    [Fact]
    public void Insert_NullEntry_ShouldThrowException()
    {
        // Arrange
        var index = new SpatialIndex();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => index.Insert(null!));
    }

    [Fact]
    public void Insert_MultipleShapes_ShouldAddAllSuccessfully()
    {
        // Arrange
        var index = new SpatialIndex();
        var shapes = new Shape[]
        {
            new Point(new Coordinate(0, 0)),
            new Point(new Coordinate(10, 10)),
            new Point(new Coordinate(20, 20)),
        };

        // Act
        for (var i = 0; i < shapes.Length; i++)
        {
            index.Insert(shapes[i], i + 1);
        }

        // Assert
        Assert.Equal(3, index.Count);
        Assert.False(index.IsEmpty);

        var bbox = index.BoundingBox!.Value;
        Assert.Equal(0, bbox.MinX);
        Assert.Equal(0, bbox.MinY);
        Assert.Equal(20, bbox.MaxX);
        Assert.Equal(20, bbox.MaxY);
    }

    [Fact]
    public void Search_EmptyIndex_ShouldReturnEmpty()
    {
        // Arrange
        var index = new SpatialIndex();
        var searchBox = new BoundingBox(0, 0, 10, 10);

        // Act
        var results = index.Search(searchBox);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_BoundingBoxQuery_ShouldReturnIntersectingEntries()
    {
        // Arrange
        var index = new SpatialIndex();

        // Add points at different locations
        var points = new[]
        {
            new Point(new Coordinate(5, 5)), // Inside search box
            new Point(new Coordinate(15, 15)), // Outside search box
            new Point(new Coordinate(0, 0)), // On search box boundary
            new Point(new Coordinate(10, 10)), // On search box boundary
        };

        for (var i = 0; i < points.Length; i++)
        {
            index.Insert(points[i], i + 1);
        }

        var searchBox = new BoundingBox(0, 0, 10, 10);

        // Act
        var results = index.Search(searchBox);

        // Assert
        Assert.Equal(3, results.Count); // Points 1, 3, and 4 should be found
        var recordNumbers = results.Select(r => r.RecordNumber).ToHashSet();
        Assert.Contains(1, recordNumbers); // Point at (5,5)
        Assert.Contains(3, recordNumbers); // Point at (0,0)
        Assert.Contains(4, recordNumbers); // Point at (10,10)
        Assert.DoesNotContain(2, recordNumbers); // Point at (15,15) should not be found
    }

    [Fact]
    public void Search_CoordinateQuery_ShouldReturnContainingEntries()
    {
        // Arrange
        var index = new SpatialIndex();
        var coordinate = new Coordinate(5, 5);

        // Add a polygon that contains the coordinate and one that doesn't
        var polygon1 = CreateSquarePolygon(0, 0, 10, 10); // Contains (5,5)
        var polygon2 = CreateSquarePolygon(20, 20, 30, 30); // Doesn't contain (5,5)

        index.Insert(polygon1, 1);
        index.Insert(polygon2, 2);

        // Act
        var results = index.Search(coordinate);

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].RecordNumber);
    }

    [Fact]
    public void FindNearest_SinglePoint_ShouldReturnThatPoint()
    {
        // Arrange
        var index = new SpatialIndex();
        var point = new Point(new Coordinate(10, 20));
        index.Insert(point, 1);

        var queryCoordinate = new Coordinate(12, 22);

        // Act
        var nearest = index.FindNearest(queryCoordinate, 1);

        // Assert
        Assert.Single(nearest);
        Assert.Equal(1, nearest[0].RecordNumber);
    }

    [Fact]
    public void FindNearest_MultiplePoints_ShouldReturnClosestFirst()
    {
        // Arrange
        var index = new SpatialIndex();
        var points = new[]
        {
            new Point(new Coordinate(0, 0)), // Distance: ~14.14
            new Point(new Coordinate(5, 5)), // Distance: ~7.07
            new Point(new Coordinate(15, 15)), // Distance: ~7.07
            new Point(new Coordinate(20, 20)), // Distance: ~14.14
        };

        for (var i = 0; i < points.Length; i++)
        {
            index.Insert(points[i], i + 1);
        }

        var queryCoordinate = new Coordinate(10, 10);

        // Act
        var nearest = index.FindNearest(queryCoordinate, 2);

        // Assert
        Assert.Equal(2, nearest.Count);

        // Results should be sorted by distance
        var recordNumbers = nearest.Select(e => e.RecordNumber).ToArray();

        // Points at (5,5) and (15,15) should be returned as they are closest
        Assert.Contains(2, recordNumbers); // Point at (5,5)
        Assert.Contains(3, recordNumbers); // Point at (15,15)
    }

    [Fact]
    public void Clear_WithEntries_ShouldRemoveAll()
    {
        // Arrange
        var index = new SpatialIndex();
        var point = new Point(new Coordinate(10, 20));
        index.Insert(point, 1);

        Assert.Equal(1, index.Count);

        // Act
        index.Clear();

        // Assert
        Assert.Equal(0, index.Count);
        Assert.True(index.IsEmpty);
        Assert.Null(index.BoundingBox);
    }

    [Fact]
    public void GetStatistics_EmptyIndex_ShouldReturnZeros()
    {
        // Arrange
        var index = new SpatialIndex();

        // Act
        var stats = index.GetStatistics();

        // Assert
        Assert.Equal(0, stats.LeafCount);
        Assert.Equal(0, stats.InternalCount);
        Assert.Equal(0, stats.MaxDepth);
        Assert.Equal(0, stats.TotalEntries);
        Assert.Equal(0, stats.AverageEntriesPerLeaf);
    }

    [Fact]
    public void GetStatistics_WithEntries_ShouldReturnValidStats()
    {
        // Arrange
        var index = new SpatialIndex();

        // Add several entries to force tree structure
        for (var i = 0; i < 5; i++)
        {
            var point = new Point(new Coordinate(i * 10, i * 10));
            index.Insert(point, i + 1);
        }

        // Act
        var stats = index.GetStatistics();

        // Assert
        Assert.True(stats.LeafCount >= 1);
        Assert.True(stats.TotalEntries >= 5);
        Assert.True(stats.AverageEntriesPerLeaf > 0);
    }

    [Fact]
    public void LargeDataset_ShouldHandleEfficientlyWithSplitting()
    {
        // Arrange
        var index = new SpatialIndex(4, 2); // Small node capacity to force splitting
        var random = new Random(42); // Fixed seed for reproducible results

        // Act - Insert many random points
        const int pointCount = 100;
        for (var i = 0; i < pointCount; i++)
        {
            var x = random.NextDouble() * 1000;
            var y = random.NextDouble() * 1000;
            var point = new Point(new Coordinate(x, y));
            index.Insert(point, i + 1);
        }

        // Assert
        Assert.Equal(pointCount, index.Count);

        var stats = index.GetStatistics();
        Assert.True(stats.LeafCount > 1, "Should have multiple leaves due to splitting");
        Assert.True(stats.MaxDepth >= 1, "Should have some depth due to splitting");

        // Test search functionality
        var searchBox = new BoundingBox(0, 0, 100, 100);
        var results = index.Search(searchBox);
        Assert.True(results.Count > 0, "Should find some points in search area");
    }

    [Fact]
    public void ToString_ShouldProvideUsefulInformation()
    {
        // Arrange
        var index = new SpatialIndex();
        var point = new Point(new Coordinate(10, 20));
        index.Insert(point, 1);

        // Act
        var result = index.ToString();

        // Assert
        Assert.Contains("1 entries", result);
        Assert.Contains("leaves", result);
        Assert.Contains("depth", result);
    }

    private static Polygon CreateSquarePolygon(double minX, double minY, double maxX, double maxY)
    {
        var coordinates = new[]
        {
            new Coordinate(minX, minY), // Bottom-left
            new Coordinate(maxX, minY), // Bottom-right
            new Coordinate(maxX, maxY), // Top-right
            new Coordinate(minX, maxY), // Top-left
            new Coordinate(minX, minY), // Close the ring
        };

        return new Polygon(coordinates);
    }
}
