using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Geometry;

public class PolyLineTests
{
    [Fact]
    public void Constructor_SinglePart_ShouldCreateValidPolyLine()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 1),
            new Coordinate(2, 0),
        };
        var polyLine = new PolyLine(coordinates);
        Assert.Equal(1, polyLine.PartCount);
        Assert.Equal(3, polyLine.CoordinateCount);
        Assert.Equal(ShapeType.PolyLine, polyLine.ShapeType);
        Assert.False(polyLine.IsEmpty);
    }

    [Fact]
    public void Constructor_MultiPart_ShouldCreateValidPolyLine()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3), new Coordinate(4, 4) };
        var parts = new[] { part1, part2 };
        var polyLine = new PolyLine(parts);
        Assert.Equal(2, polyLine.PartCount);
        Assert.Equal(5, polyLine.CoordinateCount);
        Assert.Equal(ShapeType.PolyLine, polyLine.ShapeType);
    }

    [Fact]
    public void Constructor_With3DCoordinates_ShouldSetCorrectShapeType()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, 0),
            new Coordinate(1, 1, 1),
            new Coordinate(2, 0, 2),
        };
        var polyLine = new PolyLine(coordinates);
        Assert.Equal(ShapeType.PolyLineZ, polyLine.ShapeType);
    }

    [Fact]
    public void Constructor_WithMeasuredCoordinates_ShouldSetCorrectShapeType()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, null, 0),
            new Coordinate(1, 1, null, 1),
            new Coordinate(2, 0, null, 2),
        };
        var polyLine = new PolyLine(coordinates);
        Assert.Equal(ShapeType.PolyLineM, polyLine.ShapeType);
    }

    [Fact]
    public void Constructor_WithBothZAndM_ShouldPrioritizeZ()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, 0, 0),
            new Coordinate(1, 1, 1, 1),
            new Coordinate(2, 0, 2, 2),
        };
        var polyLine = new PolyLine(coordinates);
        Assert.Equal(ShapeType.PolyLineZ, polyLine.ShapeType); // Should prioritize Z
    }

    [Fact]
    public void Constructor_WithNullParts_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PolyLine((IEnumerable<IEnumerable<Coordinate>>)null!)
        );
    }

    [Fact]
    public void Constructor_WithNullPart_ShouldThrowArgumentNullException()
    {
        var parts = new IEnumerable<Coordinate>[] { null! };
        Assert.Throws<ArgumentNullException>(() => new PolyLine(parts));
    }

    [Fact]
    public void Constructor_WithPartHavingLessThanTwoCoordinates_ShouldThrowArgumentException()
    {
        var invalidPart = new[] { new Coordinate(0, 0) }; // Only one coordinate
        Assert.Throws<ArgumentException>(() => new PolyLine(invalidPart));
    }

    [Fact]
    public void Constructor_EmptyParts_ShouldCreateEmptyPolyLine()
    {
        var emptyParts = Array.Empty<IEnumerable<Coordinate>>();
        var polyLine = new PolyLine(emptyParts);
        Assert.True(polyLine.IsEmpty);
        Assert.Equal(0, polyLine.PartCount);
        Assert.Equal(0, polyLine.CoordinateCount);
    }

    [Fact]
    public void Constructor_VariadicCoordinates_ShouldCreateValidPolyLine()
    {
        var polyLine = new PolyLine(
            new Coordinate(0, 0),
            new Coordinate(1, 1),
            new Coordinate(2, 0)
        );
        Assert.Equal(1, polyLine.PartCount);
        Assert.Equal(3, polyLine.CoordinateCount);
    }

    [Fact]
    public void BoundingBox_ShouldCalculateCorrectBounds()
    {
        var coordinates = new[]
        {
            new Coordinate(-5, -3),
            new Coordinate(10, 7),
            new Coordinate(2, -8),
        };
        var polyLine = new PolyLine(coordinates);
        var bbox = polyLine.BoundingBox;
        Assert.Equal(-5, bbox.MinX);
        Assert.Equal(-8, bbox.MinY);
        Assert.Equal(10, bbox.MaxX);
        Assert.Equal(7, bbox.MaxY);
    }

    [Fact]
    public void BoundingBox_EmptyPolyLine_ShouldReturnZeroBounds()
    {
        var polyLine = new PolyLine(Array.Empty<IEnumerable<Coordinate>>());
        var bbox = polyLine.BoundingBox;
        Assert.Equal(0, bbox.MinX);
        Assert.Equal(0, bbox.MinY);
        Assert.Equal(0, bbox.MaxX);
        Assert.Equal(0, bbox.MaxY);
    }

    [Fact]
    public void GetPart_ValidIndex_ShouldReturnCorrectPart()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var polyLine = new PolyLine([part1, part2]);
        var retrievedPart = polyLine.GetPart(1);
        Assert.Equal(2, retrievedPart.Count);
        Assert.Equal(new Coordinate(2, 2), retrievedPart[0]);
        Assert.Equal(new Coordinate(3, 3), retrievedPart[1]);
    }

    [Fact]
    public void GetPart_InvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => polyLine.GetPart(1)); // Only has part 0
        Assert.Throws<ArgumentOutOfRangeException>(() => polyLine.GetPart(-1));
    }

    [Fact]
    public void GetCoordinates_ShouldReturnAllCoordinates()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var polyLine = new PolyLine([part1, part2]);
        var allCoordinates = polyLine.GetCoordinates().ToList();
        Assert.Equal(4, allCoordinates.Count);
        Assert.Equal(new Coordinate(0, 0), allCoordinates[0]);
        Assert.Equal(new Coordinate(1, 1), allCoordinates[1]);
        Assert.Equal(new Coordinate(2, 2), allCoordinates[2]);
        Assert.Equal(new Coordinate(3, 3), allCoordinates[3]);
    }

    [Theory]
    [InlineData(new double[] { 0, 0, 3, 4 }, 5.0)] // 3-4-5 triangle
    [InlineData(new double[] { 0, 0, 1, 0, 1, 1 }, 2.0)] // Right angle, 1+1
    [InlineData(new double[] { 0, 0, 10, 0 }, 10.0)] // Straight line
    public void Length_ShouldCalculateCorrectDistance(double[] coords, double expectedLength)
    {
        var coordinates = new List<Coordinate>();
        for (int i = 0; i < coords.Length; i += 2)
        {
            coordinates.Add(new Coordinate(coords[i], coords[i + 1]));
        }
        var polyLine = new PolyLine(coordinates);
        var length = polyLine.Length;
        Assert.Equal(expectedLength, length, precision: 10);
    }

    [Fact]
    public void Length3D_ShouldCalculateCorrect3DDistance()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0, 0),
            new Coordinate(3, 4, 5), // Distance should be sqrt(3²+4²+5²) = sqrt(50) ≈ 7.07
        };
        var polyLine = new PolyLine(coordinates);
        var length3D = polyLine.Length3D;
        Assert.Equal(7.0710678118654755, length3D, precision: 10);
    }

    [Fact]
    public void GetPartLength_ShouldCalculateCorrectLength()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(3, 4) }; // Length = 5
        var part2 = new[] { new Coordinate(0, 0), new Coordinate(1, 0), new Coordinate(1, 1) }; // Length = 2
        var polyLine = new PolyLine([part1, part2]);
        var part0Length = polyLine.GetPartLength(0);
        var part1Length = polyLine.GetPartLength(1);
        Assert.Equal(5.0, part0Length);
        Assert.Equal(2.0, part1Length);
    }

    [Fact]
    public void GetPartLength_InvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => polyLine.GetPartLength(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => polyLine.GetPartLength(-1));
    }

    [Fact]
    public void Transform_ShouldApplyTransformationToAllCoordinates()
    {
        var coordinates = new[] { new Coordinate(1, 2), new Coordinate(3, 4) };
        var polyLine = new PolyLine(coordinates);
        Func<Coordinate, Coordinate> transform = coord => new Coordinate(
            coord.X + 10,
            coord.Y + 20
        );
        var transformedShape = polyLine.Transform(transform);
        var transformedPolyLine = (PolyLine)transformedShape;
        var transformedCoords = transformedPolyLine.GetCoordinates().ToList();
        Assert.Equal(new Coordinate(11, 22), transformedCoords[0]);
        Assert.Equal(new Coordinate(13, 24), transformedCoords[1]);
    }

    [Fact]
    public void Transform_WithNullTransform_ShouldThrowArgumentNullException()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        Assert.Throws<ArgumentNullException>(() => polyLine.Transform(null!));
    }

    [Fact]
    public void AddPart_ShouldCreateNewPolyLineWithAdditionalPart()
    {
        var originalPart = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var polyLine = new PolyLine(originalPart);
        var newPart = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var expandedPolyLine = polyLine.AddPart(newPart);
        Assert.Equal(1, polyLine.PartCount); // Original should be unchanged
        Assert.Equal(2, expandedPolyLine.PartCount);
        Assert.Equal(4, expandedPolyLine.CoordinateCount);
    }

    [Fact]
    public void AddPart_WithNullCoordinates_ShouldThrowArgumentNullException()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        Assert.Throws<ArgumentNullException>(() => polyLine.AddPart(null!));
    }

    [Fact]
    public void RemovePart_ShouldCreateNewPolyLineWithoutSpecifiedPart()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var polyLine = new PolyLine([part1, part2]);
        var reducedPolyLine = polyLine.RemovePart(0);
        Assert.Equal(2, polyLine.PartCount); // Original should be unchanged
        Assert.Equal(1, reducedPolyLine.PartCount);
        var remainingPart = reducedPolyLine.GetPart(0);
        Assert.Equal(new Coordinate(2, 2), remainingPart[0]);
    }

    [Fact]
    public void RemovePart_InvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => polyLine.RemovePart(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => polyLine.RemovePart(-1));
    }

    [Fact]
    public void Simplify_ShouldRemoveRedundantVertices()
    {
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0.001), // Very close to the line from (0,0) to (2,0)
            new Coordinate(2, 0),
        };
        var polyLine = new PolyLine(coordinates);
        var simplifiedPolyLine = polyLine.Simplify(0.01); // Tolerance higher than deviation
        Assert.Equal(2, simplifiedPolyLine.CoordinateCount); // Should remove middle point
        var simplifiedCoords = simplifiedPolyLine.GetCoordinates().ToList();
        Assert.Equal(new Coordinate(0, 0), simplifiedCoords[0]);
        Assert.Equal(new Coordinate(2, 0), simplifiedCoords[1]);
    }

    [Fact]
    public void Simplify_WithZeroTolerance_ShouldThrowArgumentException()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        Assert.Throws<ArgumentException>(() => polyLine.Simplify(0));
        Assert.Throws<ArgumentException>(() => polyLine.Simplify(-1));
    }

    [Fact]
    public void IsValid_ValidPolyLine_ShouldReturnTrue()
    {
        var polyLine = new PolyLine(
            new Coordinate(0, 0),
            new Coordinate(1, 1),
            new Coordinate(2, 0)
        );
        Assert.True(polyLine.IsValid());
    }

    [Fact]
    public void GetValidationErrors_ValidPolyLine_ShouldReturnEmpty()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        var errors = polyLine.GetValidationErrors().ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void GetValidationErrors_InvalidCoordinates_ShouldReturnErrors()
    {
        var coordinates = new[]
        {
            new Coordinate(double.NaN, 0),
            new Coordinate(1, double.PositiveInfinity),
        };
        var polyLine = new PolyLine(coordinates);
        var errors = polyLine.GetValidationErrors().ToList();
        Assert.Contains("Part 0, Coordinate 0: X coordinate is not a valid number", errors);
        Assert.Contains("Part 0, Coordinate 1: Y coordinate is not a valid number", errors);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var polyLine = new PolyLine([part1, part2]);
        var result = polyLine.ToString();
        Assert.Equal("POLYLINE (2 parts, 4 coordinates)", result);
    }

    [Fact]
    public void Equals_SamePolyLines_ShouldReturnTrue()
    {
        var coordinates = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var polyLine1 = new PolyLine(coordinates);
        var polyLine2 = new PolyLine(coordinates);
        Assert.True(polyLine1.Equals(polyLine2));
        Assert.Equal(polyLine1.GetHashCode(), polyLine2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentPolyLines_ShouldReturnFalse()
    {
        var polyLine1 = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        var polyLine2 = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 2)); // Different Y
        Assert.False(polyLine1.Equals(polyLine2));
    }

    [Fact]
    public void Equals_DifferentPartCounts_ShouldReturnFalse()
    {
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var polyLine1 = new PolyLine(part1);
        var polyLine2 = new PolyLine([part1, part2]);
        Assert.False(polyLine1.Equals(polyLine2));
    }

    [Fact]
    public void Equals_WithNonPolyLine_ShouldReturnFalse()
    {
        var polyLine = new PolyLine(new Coordinate(0, 0), new Coordinate(1, 1));
        var notAPolyLine = "not a polyline";
        Assert.False(polyLine.Equals(notAPolyLine));
    }
}
