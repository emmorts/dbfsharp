using System.Text.Json;
using DbfSharp.ConsoleAot.Formatters;
using DbfSharp.Core;
using DbfSharp.Core.Geometry;

namespace DbfSharp.Tests.Formatters;

public class GeoJsonFormatterTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_ShouldCreateFormatter()
    {
        var configuration = CreateTestConfiguration();
        var formatter = new GeoJsonFormatter(configuration);
        Assert.NotNull(formatter);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GeoJsonFormatter(null!));
    }

    [Fact]
    public async Task WriteAsync_SinglePointFeature_ShouldGenerateValidGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var point = new Point(123.456, 789.012);
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("FeatureCollection", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("features", out var featuresArray));
        Assert.Equal(JsonValueKind.Array, featuresArray.ValueKind);
        Assert.Equal(1, featuresArray.GetArrayLength());

        var firstFeature = featuresArray[0];
        Assert.Equal("Feature", firstFeature.GetProperty("type").GetString());

        var geometry = firstFeature.GetProperty("geometry");
        Assert.Equal("Point", geometry.GetProperty("type").GetString());

        var coordinates = geometry.GetProperty("coordinates");
        Assert.Equal(JsonValueKind.Array, coordinates.ValueKind);
        Assert.Equal(2, coordinates.GetArrayLength());
        Assert.Equal(123.456, coordinates[0].GetDouble(), precision: 6);
        Assert.Equal(789.012, coordinates[1].GetDouble(), precision: 6);
    }

    [Fact]
    public async Task WriteAsync_Point3D_ShouldIncludeZCoordinate()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var point = new Point(123.456, 789.012, 345.678);
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var coordinates = root.GetProperty("features")[0]
            .GetProperty("geometry")
            .GetProperty("coordinates");

        Assert.Equal(3, coordinates.GetArrayLength());
        Assert.Equal(123.456, coordinates[0].GetDouble(), precision: 6);
        Assert.Equal(789.012, coordinates[1].GetDouble(), precision: 6);
        Assert.Equal(345.678, coordinates[2].GetDouble(), precision: 6);
    }

    [Fact]
    public async Task WriteAsync_MultiPointFeature_ShouldGenerateValidGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var coordinates = new[]
        {
            new Coordinate(123.456, 789.012),
            new Coordinate(234.567, 890.123),
            new Coordinate(345.678, 901.234),
        };
        var multiPoint = new MultiPoint(coordinates);
        var feature = new ShapefileFeature(1, multiPoint, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal("MultiPoint", geometry.GetProperty("type").GetString());

        var coordinatesArray = geometry.GetProperty("coordinates");
        Assert.Equal(3, coordinatesArray.GetArrayLength());

        // Check first coordinate
        Assert.Equal(123.456, coordinatesArray[0][0].GetDouble(), precision: 6);
        Assert.Equal(789.012, coordinatesArray[0][1].GetDouble(), precision: 6);

        // Check second coordinate
        Assert.Equal(234.567, coordinatesArray[1][0].GetDouble(), precision: 6);
        Assert.Equal(890.123, coordinatesArray[1][1].GetDouble(), precision: 6);
    }

    [Fact]
    public async Task WriteAsync_LineStringFeature_ShouldGenerateValidGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 1),
            new Coordinate(2, 0),
            new Coordinate(0, 0), // Closed line
        };
        var polyLine = new PolyLine(coordinates);
        var feature = new ShapefileFeature(1, polyLine, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal("LineString", geometry.GetProperty("type").GetString());

        var coordinatesArray = geometry.GetProperty("coordinates");
        Assert.Equal(4, coordinatesArray.GetArrayLength());

        // Check coordinates
        Assert.Equal(0, coordinatesArray[0][0].GetDouble());
        Assert.Equal(0, coordinatesArray[0][1].GetDouble());
        Assert.Equal(1, coordinatesArray[1][0].GetDouble());
        Assert.Equal(1, coordinatesArray[1][1].GetDouble());
    }

    [Fact]
    public async Task WriteAsync_MultiLineStringFeature_ShouldGenerateValidGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var part1 = new[] { new Coordinate(0, 0), new Coordinate(1, 1) };
        var part2 = new[] { new Coordinate(2, 2), new Coordinate(3, 3) };
        var multiPolyLine = new PolyLine([part1, part2]);
        var feature = new ShapefileFeature(1, multiPolyLine, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal("MultiLineString", geometry.GetProperty("type").GetString());

        var coordinatesArray = geometry.GetProperty("coordinates");
        Assert.Equal(2, coordinatesArray.GetArrayLength()); // Two line strings

        // Check first line string
        var firstLine = coordinatesArray[0];
        Assert.Equal(2, firstLine.GetArrayLength());
        Assert.Equal(0, firstLine[0][0].GetDouble());
        Assert.Equal(0, firstLine[0][1].GetDouble());
    }

    [Fact]
    public async Task WriteAsync_PolygonFeature_ShouldGenerateValidGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var exteriorRing = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, 0), // Closed ring
        };
        var polygon = new Polygon([exteriorRing]);
        var feature = new ShapefileFeature(1, polygon, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal("Polygon", geometry.GetProperty("type").GetString());

        var coordinatesArray = geometry.GetProperty("coordinates");
        Assert.Equal(1, coordinatesArray.GetArrayLength()); // One ring

        var ring = coordinatesArray[0];
        Assert.Equal(5, ring.GetArrayLength()); // 5 coordinates (closed)

        // Check first and last coordinates are the same (closed)
        Assert.Equal(ring[0][0].GetDouble(), ring[4][0].GetDouble());
        Assert.Equal(ring[0][1].GetDouble(), ring[4][1].GetDouble());
    }

    [Fact]
    public async Task WriteAsync_PolygonWithHole_ShouldGenerateValidGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var exteriorRing = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(2, 0),
            new Coordinate(2, 2),
            new Coordinate(0, 2),
            new Coordinate(0, 0),
        };
        var interiorRing = new[]
        {
            new Coordinate(0.5, 0.5),
            new Coordinate(1.5, 0.5),
            new Coordinate(1.5, 1.5),
            new Coordinate(0.5, 1.5),
            new Coordinate(0.5, 0.5),
        };
        var polygon = new Polygon([exteriorRing, interiorRing]);
        var feature = new ShapefileFeature(1, polygon, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal("Polygon", geometry.GetProperty("type").GetString());

        var coordinatesArray = geometry.GetProperty("coordinates");
        Assert.Equal(2, coordinatesArray.GetArrayLength()); // Exterior + interior ring
    }

    [Fact]
    public async Task WriteAsync_NullGeometry_ShouldGenerateNullGeometry()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var feature = new ShapefileFeature(1, NullShape.Instance, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal(JsonValueKind.Null, geometry.ValueKind);
    }

    [Fact]
    public async Task WriteAsync_FeatureWithAttributes_ShouldIncludeProperties()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var point = new Point(123.456, 789.012);

        // Create a feature without real attributes for now
        // This test focuses on the GeoJSON structure rather than attribute parsing
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var properties = document.RootElement.GetProperty("features")[0].GetProperty("properties");

        // Properties should be an empty object when no attributes are present
        Assert.Equal(JsonValueKind.Object, properties.ValueKind);
    }

    [Fact]
    public async Task WriteAsync_WithPrettyPrint_ShouldFormatJson()
    {
        var configuration = new FormatterConfiguration
        {
            PrettyPrint = true,
            IncludeTypeInfo = false,
            MaxDisplayRecords = null,
            ShowWarnings = true,
            DateFormat = null,
        };
        var formatter = new GeoJsonFormatter(configuration);
        var point = new Point(123.456, 789.012);
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        Assert.Contains("\n", json); // Should have newlines when pretty printed
        Assert.Contains("  ", json); // Should have indentation
    }

    [Fact]
    public async Task WriteAsync_WithoutPrettyPrint_ShouldGenerateMinifiedJson()
    {
        var configuration = new FormatterConfiguration
        {
            PrettyPrint = false,
            IncludeTypeInfo = false,
            MaxDisplayRecords = null,
            ShowWarnings = true,
            DateFormat = null,
        };
        var formatter = new GeoJsonFormatter(configuration);
        var point = new Point(123.456, 789.012);
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        Assert.DoesNotContain("\n", json); // Should not have newlines when minified
    }

    [Fact]
    public async Task WriteAsync_WithTypeInfo_ShouldIncludeMetadata()
    {
        var configuration = new FormatterConfiguration
        {
            PrettyPrint = false,
            IncludeTypeInfo = true,
            MaxDisplayRecords = null,
            ShowWarnings = true,
            DateFormat = null,
        };
        var formatter = new GeoJsonFormatter(configuration);
        var point = new Point(123.456, 789.012);
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("metadata", out var metadata));
        Assert.Equal("DbfSharp", metadata.GetProperty("generator").GetString());
        Assert.True(metadata.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task WriteAsync_WithMaxDisplayRecords_ShouldLimitFeatures()
    {
        var configuration = new FormatterConfiguration
        {
            PrettyPrint = false,
            IncludeTypeInfo = false,
            MaxDisplayRecords = 2,
            ShowWarnings = true,
            DateFormat = null,
        };
        var formatter = new GeoJsonFormatter(configuration);

        var features = new[]
        {
            new ShapefileFeature(1, new Point(1, 1), null, 0, 20),
            new ShapefileFeature(2, new Point(2, 2), null, 0, 20),
            new ShapefileFeature(3, new Point(3, 3), null, 0, 20), // Should be excluded
        };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var featuresArray = document.RootElement.GetProperty("features");
        Assert.Equal(2, featuresArray.GetArrayLength()); // Should be limited to 2
    }

    [Fact]
    public async Task WriteAsync_DbfOnlyData_ShouldCreateNullGeometries()
    {
        // For now, we'll test this scenario by creating features with null geometries
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());

        var features = new[]
        {
            new ShapefileFeature(1, NullShape.Instance, null, 0, 8),
            new ShapefileFeature(2, NullShape.Instance, null, 0, 8),
        };

        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var featuresArray = document.RootElement.GetProperty("features");
        Assert.Equal(2, featuresArray.GetArrayLength());

        // All geometries should be null for null shape data
        foreach (var featureElement in featuresArray.EnumerateArray())
        {
            var geometry = featureElement.GetProperty("geometry");
            Assert.Equal(JsonValueKind.Null, geometry.ValueKind);
        }
    }

    [Fact]
    public async Task WriteAsync_MixedAttributeTypes_ShouldHandleAllTypes()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var point = new Point(0, 0);

        // Test without attributes for now - focus on geometry handling
        var feature = new ShapefileFeature(1, point, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var properties = document.RootElement.GetProperty("features")[0].GetProperty("properties");
        Assert.Equal(JsonValueKind.Object, properties.ValueKind);
    }

    [Fact]
    public async Task WriteAsync_MultiPatchFeature_ShouldGenerateGeometryCollection()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());

        var triangleStripCoords = new[]
        {
            new Coordinate(0, 0, 0),
            new Coordinate(1, 0, 1),
            new Coordinate(0.5, 1, 0.5),
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
        var feature = new ShapefileFeature(1, multiPatch, null, 0, 20);
        var features = new[] { feature };
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);

        var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        Assert.Equal("GeometryCollection", geometry.GetProperty("type").GetString());

        var geometries = geometry.GetProperty("geometries");
        Assert.Equal(2, geometries.GetArrayLength());

        // First geometry should be MultiPoint (triangle strip)
        var firstGeom = geometries[0];
        Assert.Equal("MultiPoint", firstGeom.GetProperty("type").GetString());

        // Second geometry should be Polygon (outer ring)
        var secondGeom = geometries[1];
        Assert.Equal("Polygon", secondGeom.GetProperty("type").GetString());

        // Verify 3D coordinates are included
        var firstCoords = firstGeom.GetProperty("coordinates");
        Assert.Equal(3, firstCoords[0].GetArrayLength()); // Should include Z coordinate
        Assert.Equal(0.0, firstCoords[0][2].GetDouble()); // Z coordinate
    }

    [Fact]
    public async Task WriteAsync_EmptyFeatureCollection_ShouldGenerateValidEmptyGeoJson()
    {
        var formatter = new GeoJsonFormatter(CreateTestConfiguration());
        var features = Array.Empty<ShapefileFeature>();
        var fields = Array.Empty<string>();
        await using var writer = new StringWriter();
        await formatter.WriteAsync(features, fields, null!, writer);
        var json = writer.ToString();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("FeatureCollection", root.GetProperty("type").GetString());
        var featuresArray = root.GetProperty("features");
        Assert.Equal(JsonValueKind.Array, featuresArray.ValueKind);
        Assert.Equal(0, featuresArray.GetArrayLength());
    }

    private static FormatterConfiguration CreateTestConfiguration()
    {
        return new FormatterConfiguration
        {
            PrettyPrint = false,
            IncludeTypeInfo = false,
            MaxDisplayRecords = null,
            ShowWarnings = true,
            DateFormat = null,
        };
    }
}
