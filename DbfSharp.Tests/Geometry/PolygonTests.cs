using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Geometry;

public class PolygonTests
{
    [Fact]
    public void Constructor_SimpleRectangle_ShouldCreateValidPolygon()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, 0),
        };
        var polygon = new Polygon(coordinates);
        Assert.Equal(1, polygon.RingCount);
        Assert.Equal(5, polygon.CoordinateCount);
        Assert.Equal(ShapeType.Polygon, polygon.ShapeType);
        Assert.False(polygon.IsEmpty);
        Assert.Equal(0, polygon.InteriorRingCount);
    }

    [Fact]
    public void Constructor_AutoClosesUnclosedRing_ShouldCreateValidPolygon()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
        };
        var polygon = new Polygon(coordinates);
        Assert.Equal(5, polygon.CoordinateCount); // Should add closing coordinate
        var ring = polygon.ExteriorRing!;
        Assert.Equal(ring[0], ring[ring.Count - 1]); // Should be closed
    }

    [Fact]
    public void Constructor_WithInteriorRings_ShouldCreateValidPolygon()
    {
        var exteriorRing = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(3, 0),
            new Coordinate(3, 3),
            new Coordinate(0, 3),
            new Coordinate(0, 0),
        };

        var interiorRing = new[]
        {
            new Coordinate(1, 1),
            new Coordinate(2, 1),
            new Coordinate(2, 2),
            new Coordinate(1, 2),
            new Coordinate(1, 1),
        };
        var polygon = new Polygon(new[] { exteriorRing, interiorRing });
        Assert.Equal(2, polygon.RingCount);
        Assert.Equal(1, polygon.InteriorRingCount);
        Assert.Equal(10, polygon.CoordinateCount);
    }

    [Fact]
    public void Constructor_3D_ShouldSetCorrectShapeType()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, 0),
            new Coordinate(1, 0, 1),
            new Coordinate(1, 1, 2),
            new Coordinate(0, 1, 3),
            new Coordinate(0, 0, 0),
        };
        var polygon = new Polygon(coordinates);
        Assert.Equal(ShapeType.PolygonZ, polygon.ShapeType);
    }

    [Fact]
    public void Constructor_WithMeasuredCoordinates_ShouldSetCorrectShapeType()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, null, 0),
            new Coordinate(1, 0, null, 1),
            new Coordinate(1, 1, null, 2),
            new Coordinate(0, 1, null, 3),
            new Coordinate(0, 0, null, 0),
        };
        var polygon = new Polygon(coordinates);
        Assert.Equal(ShapeType.PolygonM, polygon.ShapeType);
    }

    [Fact]
    public void Constructor_WithBothZAndM_ShouldPrioritizeZ()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, 0, 0),
            new Coordinate(1, 0, 1, 1),
            new Coordinate(1, 1, 2, 2),
            new Coordinate(0, 1, 3, 3),
            new Coordinate(0, 0, 0, 0),
        };
        var polygon = new Polygon(coordinates);
        Assert.Equal(ShapeType.PolygonZ, polygon.ShapeType);
    }

    [Fact]
    public void Constructor_WithInvalidInput_ShouldThrowExceptions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Polygon((IEnumerable<IEnumerable<Coordinate>>)null!)
        );
        Assert.Throws<ArgumentNullException>(() =>
            new Polygon(new IEnumerable<Coordinate>[] { null! })
        );
        Assert.Throws<ArgumentException>(() =>
            new Polygon(new[] { new Coordinate(0, 0), new Coordinate(1, 0) })
        ); // Too few coordinates
    }

    [Theory]
    [InlineData(new double[] { 0, 0, 1, 0, 1, 1, 0, 1, 0, 0 }, 1.0)] // Unit square
    [InlineData(new double[] { 0, 0, 2, 0, 2, 2, 0, 2, 0, 0 }, 4.0)] // 2x2 square
    [InlineData(new double[] { 0, 0, 3, 0, 0, 4, 0, 0 }, 6.0)] // Triangle
    public void Area_ShouldCalculateCorrectArea(double[] coords, double expectedArea)
    {
        var coordinates = new List<Coordinate>();
        for (int i = 0; i < coords.Length; i += 2)
        {
            coordinates.Add(new Coordinate(coords[i], coords[i + 1]));
        }
        var polygon = new Polygon(coordinates);
        Assert.Equal(expectedArea, polygon.Area, precision: 10);
    }

    [Theory]
    [InlineData(0.5, 0.5, true)] // Center of unit square
    [InlineData(0.0, 0.0, true)] // Corner point
    [InlineData(1.5, 0.5, false)] // Outside
    [InlineData(-0.5, 0.5, false)] // Outside
    public void Contains_ShouldReturnCorrectResult(double x, double y, bool expected)
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, 0),
        };
        var polygon = new Polygon(coordinates);
        Assert.Equal(expected, polygon.Contains(new Coordinate(x, y)));
    }

    [Fact]
    public void Contains_WithHole_ShouldExcludeInteriorPoints()
    {
        var exteriorRing = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(3, 0),
            new Coordinate(3, 3),
            new Coordinate(0, 3),
            new Coordinate(0, 0),
        };

        var interiorRing = new[]
        {
            new Coordinate(1, 1),
            new Coordinate(2, 1),
            new Coordinate(2, 2),
            new Coordinate(1, 2),
            new Coordinate(1, 1),
        };

        var polygon = new Polygon(new[] { exteriorRing, interiorRing });
        Assert.True(polygon.Contains(new Coordinate(0.5, 0.5))); // In exterior, not in hole
        Assert.False(polygon.Contains(new Coordinate(1.5, 1.5))); // In hole
    }

    [Fact]
    public void Transform_ShouldApplyTransformationToAllCoordinates()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, 0),
        };
        var polygon = new Polygon(coordinates);
        Func<Coordinate, Coordinate> transform = coord => new Coordinate(
            coord.X + 10,
            coord.Y + 20
        );
        var transformedShape = polygon.Transform(transform);
        var transformedPolygon = (Polygon)transformedShape;
        var transformedCoords = transformedPolygon.GetCoordinates().ToList();
        Assert.Equal(new Coordinate(10, 20), transformedCoords[0]);
        Assert.Equal(new Coordinate(11, 20), transformedCoords[1]);
    }

    [Fact]
    public void IsValid_ShouldValidateCorrectly()
    {
        var validPolygon = new Polygon(
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, 0)
        );

        var invalidPolygon = new Polygon(
            new[]
            {
                new Coordinate(double.NaN, 0),
                new Coordinate(1, 0),
                new Coordinate(1, 1),
                new Coordinate(0, 1),
                new Coordinate(double.NaN, 0),
            }
        );
        Assert.True(validPolygon.IsValid());
        Assert.False(invalidPolygon.IsValid());
        Assert.Empty(validPolygon.GetValidationErrors());
        Assert.NotEmpty(invalidPolygon.GetValidationErrors());
    }

    [Fact]
    public void Equals_ShouldWorkCorrectly()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, 0),
        };
        var polygon1 = new Polygon(coordinates);
        var polygon2 = new Polygon(coordinates);
        var polygon3 = new Polygon(
            new Coordinate(0, 0),
            new Coordinate(2, 0),
            new Coordinate(2, 2),
            new Coordinate(0, 2),
            new Coordinate(0, 0)
        );
        Assert.True(polygon1.Equals(polygon2));
        Assert.False(polygon1.Equals(polygon3));
        Assert.False(polygon1.Equals("not a polygon"));
    }
}
