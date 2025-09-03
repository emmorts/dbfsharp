using DbfSharp.Core.Geometry;
using DbfSharp.Core.Projection;

namespace DbfSharp.Tests.Projection;

/// <summary>
/// Tests for the TransformationEngine class
/// </summary>
public class TransformationEngineTests
{
    #region Test Data Constants

    private const string Wgs84GeographicWkt =
        """GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]]""";

    private const string WebMercatorWkt =
        """PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","3857"]]""";

    #endregion

    #region Shape Transformation Tests

    [Fact]
    public void TransformationEngine_TransformPoint_ReturnsTransformedPoint()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var originalPoint = new Point(-122.4194, 37.7749); // San Francisco
        var transformedShape = TransformationEngine.Transform(originalPoint, source, target);
        Assert.IsType<Point>(transformedShape);
        var transformedPoint = (Point)transformedShape;
        Assert.True(Math.Abs(transformedPoint.X) > 1000); // Should be in meters
        Assert.True(Math.Abs(transformedPoint.Y) > 1000);
    }

    [Fact]
    public void TransformationEngine_TransformPointWithEpsgCodes_ReturnsTransformedPoint()
    {
        var originalPoint = new Point(-122.4194, 37.7749); // San Francisco in WGS84
        var transformedShape = TransformationEngine.Transform(originalPoint, 4326, 3857); // WGS84 to Web Mercator
        Assert.IsType<Point>(transformedShape);
        var transformedPoint = (Point)transformedShape;
        Assert.True(Math.Abs(transformedPoint.X) > 1000000); // Should be in meters, large values
        Assert.True(Math.Abs(transformedPoint.Y) > 1000000);
    }

    [Fact]
    public void TransformationEngine_TransformPolyLine_ReturnsTransformedPolyLine()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var coordinates = new[]
        {
            new Coordinate(-122.4194, 37.7749), // San Francisco
            new Coordinate(-122.4094, 37.7849), // Nearby point
            new Coordinate(-122.3994, 37.7949), // Another nearby point
        };
        var originalPolyLine = new PolyLine(new[] { coordinates });
        var transformedShape = TransformationEngine.Transform(originalPolyLine, source, target);
        Assert.IsType<PolyLine>(transformedShape);
        var transformedPolyLine = (PolyLine)transformedShape;
        Assert.Single(transformedPolyLine.Parts);
        Assert.Equal(3, transformedPolyLine.Parts[0].Count);

        // All coordinates should be transformed to meters
        Assert.All(
            transformedPolyLine.GetCoordinates(),
            coord =>
            {
                Assert.True(Math.Abs(coord.X) > 1000);
                Assert.True(Math.Abs(coord.Y) > 1000);
            }
        );
    }

    [Fact]
    public void TransformationEngine_TransformPolygon_ReturnsTransformedPolygon()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var ring = new[]
        {
            new Coordinate(-122.5, 37.7),
            new Coordinate(-122.4, 37.7),
            new Coordinate(-122.4, 37.8),
            new Coordinate(-122.5, 37.8),
            new Coordinate(-122.5, 37.7), // Closed ring
        };
        var originalPolygon = new Polygon(new[] { ring });
        var transformedShape = TransformationEngine.Transform(originalPolygon, source, target);
        Assert.IsType<Polygon>(transformedShape);
        var transformedPolygon = (Polygon)transformedShape;
        Assert.Single(transformedPolygon.Rings);
        Assert.Equal(5, transformedPolygon.Rings[0].Count);

        // All coordinates should be transformed to meters
        Assert.All(
            transformedPolygon.GetCoordinates(),
            coord =>
            {
                Assert.True(Math.Abs(coord.X) > 1000000);
                Assert.True(Math.Abs(coord.Y) > 1000000);
            }
        );
    }

    #endregion

    #region Multiple Shape Transformation Tests

    [Fact]
    public void TransformationEngine_TransformMultipleShapes_AllTransformed()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var shapes = new Shape[]
        {
            new Point(-122.4194, 37.7749),
            new Point(-74.0060, 40.7128),
            new Point(2.3522, 48.8566),
        };
        var transformedShapes = TransformationEngine.Transform(shapes, source, target).ToList();
        Assert.Equal(3, transformedShapes.Count);
        Assert.All(
            transformedShapes,
            shape =>
            {
                Assert.IsType<Point>(shape);
                var point = (Point)shape;
                Assert.True(Math.Abs(point.X) > 1000);
                Assert.True(Math.Abs(point.Y) > 1000);
            }
        );
    }

    [Fact]
    public void TransformationEngine_TransformMultipleShapesWithEpsgCodes_AllTransformed()
    {
        var shapes = new Shape[]
        {
            new Point(-122.4194, 37.7749), // San Francisco
            new Point(-74.0060, 40.7128), // New York
            new Point(2.3522, 48.8566), // Paris
        };
        var transformedShapes = TransformationEngine.Transform(shapes, 4326, 3857).ToList();
        Assert.Equal(3, transformedShapes.Count);
        Assert.All(
            transformedShapes,
            shape =>
            {
                Assert.IsType<Point>(shape);
                var point = (Point)shape;
                Assert.True(Math.Abs(point.X) > 100000); // Should be in Web Mercator meters
                Assert.True(Math.Abs(point.Y) > 100000);
            }
        );
    }

    #endregion

    #region Coordinate Transformation Tests

    [Fact]
    public void TransformationEngine_TransformCoordinate_ReturnsTransformedCoordinate()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var coordinate = new Coordinate(-122.4194, 37.7749);
        var transformedCoordinate = TransformationEngine.Transform(coordinate, source, target);
        Assert.True(Math.Abs(transformedCoordinate.X) > 1000000);
        Assert.True(Math.Abs(transformedCoordinate.Y) > 1000000);
    }

    [Fact]
    public void TransformationEngine_TransformCoordinateWithEpsgCodes_ReturnsTransformedCoordinate()
    {
        var coordinate = new Coordinate(-122.4194, 37.7749);
        var transformedCoordinate = TransformationEngine.Transform(coordinate, 4326, 3857);
        Assert.True(Math.Abs(transformedCoordinate.X) > 1000000);
        Assert.True(Math.Abs(transformedCoordinate.Y) > 1000000);
    }

    [Fact]
    public void TransformationEngine_TransformMultipleCoordinates_AllTransformed()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var coordinates = new[]
        {
            new Coordinate(-122.4194, 37.7749),
            new Coordinate(-74.0060, 40.7128),
            new Coordinate(2.3522, 48.8566),
        };
        var transformedCoordinates = TransformationEngine
            .Transform(coordinates, source, target)
            .ToList();
        Assert.Equal(3, transformedCoordinates.Count);
        Assert.All(
            transformedCoordinates,
            coord =>
            {
                Assert.True(Math.Abs(coord.X) > 100000);
                Assert.True(Math.Abs(coord.Y) > 100000);
            }
        );
    }

    #endregion

    #region Bounding Box Transformation Tests

    [Fact]
    public void TransformationEngine_TransformBoundingBox_ReturnsTransformedBoundingBox()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var originalBounds = new BoundingBox(-123.0, 37.0, -122.0, 38.0); // San Francisco Bay Area
        var transformedBounds = TransformationEngine.Transform(originalBounds, source, target);
        Assert.False(transformedBounds.IsEmpty);
        Assert.True(Math.Abs(transformedBounds.MinX) > 1000000); // Should be in Web Mercator meters
        Assert.True(Math.Abs(transformedBounds.MaxX) > 1000000);
        Assert.True(Math.Abs(transformedBounds.MinY) > 1000000);
        Assert.True(Math.Abs(transformedBounds.MaxY) > 1000000);
        Assert.True(transformedBounds.Width > 0);
        Assert.True(transformedBounds.Height > 0);
    }

    [Fact]
    public void TransformationEngine_TransformEmptyBoundingBox_ReturnsEmptyBoundingBox()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var emptyBounds = new BoundingBox(0, 0, 0, 0); // Create an empty bounding box
        var transformedBounds = TransformationEngine.Transform(emptyBounds, source, target);
        Assert.True(transformedBounds.IsEmpty);
    }

    #endregion

    #region Transformation Creation Tests

    [Fact]
    public void TransformationEngine_CreateTransformation_ReturnsValidTransformation()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var transformation = TransformationEngine.CreateTransformation(source, target);
        Assert.NotNull(transformation);
        Assert.True(transformation.IsValid);
        Assert.Equal(source, transformation.SourceCoordinateSystem);
        Assert.Equal(target, transformation.TargetCoordinateSystem);
    }

    [Fact]
    public void TransformationEngine_CreateTransformationWithEpsgCodes_ReturnsValidTransformation()
    {
        var transformation = TransformationEngine.CreateTransformation(4326, 3857);
        Assert.NotNull(transformation);
        Assert.True(transformation.IsValid);
        Assert.Equal(4326, transformation.SourceCoordinateSystem.EpsgCode);
        Assert.Equal(3857, transformation.TargetCoordinateSystem.EpsgCode);
    }

    [Fact]
    public void TransformationEngine_IsTransformationSupported_ValidSystems_ReturnsTrue()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var isSupported = TransformationEngine.IsTransformationSupported(source, target);
        Assert.True(isSupported);
    }

    [Fact]
    public void TransformationEngine_IsTransformationSupported_NullSystems_ReturnsFalse()
    {
        var validSystem = new ProjectionFile(Wgs84GeographicWkt);
        Assert.False(TransformationEngine.IsTransformationSupported(null!, validSystem));
        Assert.False(TransformationEngine.IsTransformationSupported(validSystem, null!));
        Assert.False(TransformationEngine.IsTransformationSupported(null!, null!));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void TransformationEngine_TransformShape_NullShape_ThrowsArgumentNullException()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        Assert.Throws<ArgumentNullException>(() =>
            TransformationEngine.Transform((Shape)null!, source, target)
        );
    }

    [Fact]
    public void TransformationEngine_TransformShape_NullCoordinateSystem_ThrowsArgumentNullException()
    {
        var shape = new Point(0, 0);
        var validSystem = new ProjectionFile(Wgs84GeographicWkt);
        Assert.Throws<ArgumentNullException>(() =>
            TransformationEngine.Transform(shape, null!, validSystem)
        );
        Assert.Throws<ArgumentNullException>(() =>
            TransformationEngine.Transform(shape, validSystem, null!)
        );
    }

    [Fact]
    public void TransformationEngine_TransformWithInvalidEpsgCode_ThrowsArgumentException()
    {
        var shape = new Point(0, 0);
        Assert.Throws<ArgumentException>(() => TransformationEngine.Transform(shape, 99999, 4326));
        Assert.Throws<ArgumentException>(() => TransformationEngine.Transform(shape, 4326, 99999));
    }

    [Fact]
    public void TransformationEngine_TransformCoordinates_NullInput_ThrowsArgumentNullException()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        Assert.Throws<ArgumentNullException>(() =>
            TransformationEngine.Transform((Coordinate[])null!, source, target)
        );
    }

    #endregion

    #region Shape Extension Method Tests

    [Fact]
    public void Shape_TransformExtensionMethod_WithProjectionFiles_TransformsCorrectly()
    {
        var source = new ProjectionFile(Wgs84GeographicWkt);
        var target = new ProjectionFile(WebMercatorWkt);
        var originalPoint = new Point(-122.4194, 37.7749);
        var transformedShape = originalPoint.Transform(source, target);
        Assert.IsType<Point>(transformedShape);
        var transformedPoint = (Point)transformedShape;
        Assert.True(Math.Abs(transformedPoint.X) > 1000000);
        Assert.True(Math.Abs(transformedPoint.Y) > 1000000);
    }

    [Fact]
    public void Shape_TransformExtensionMethod_WithEpsgCodes_TransformsCorrectly()
    {
        var originalPoint = new Point(-122.4194, 37.7749);
        var transformedShape = originalPoint.Transform(4326, 3857);
        Assert.IsType<Point>(transformedShape);
        var transformedPoint = (Point)transformedShape;
        Assert.True(Math.Abs(transformedPoint.X) > 1000000);
        Assert.True(Math.Abs(transformedPoint.Y) > 1000000);
    }

    #endregion
}
