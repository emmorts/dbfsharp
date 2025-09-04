using DbfSharp.Core.Projection;

namespace DbfSharp.Tests.Projection;

/// <summary>
/// Tests for the EPSG database functionality
/// </summary>
public class EpsgDatabaseTests
{
    #region Basic Lookup Tests

    [Fact]
    public void GetByCode_WithValidCode_ReturnsDefinition()
    {
        var definition = EpsgDatabase.GetByCode(4326);
        Assert.NotNull(definition);
        Assert.Equal(4326, definition.Code);
        Assert.Equal("WGS 84", definition.Name);
        Assert.Equal(ProjectionType.Geographic, definition.Type);
        Assert.Contains("WGS 84", definition.Wkt);
        Assert.Equal("World", definition.AreaOfUse);
    }

    [Fact]
    public void GetByCode_WithInvalidCode_ReturnsNull()
    {
        var definition = EpsgDatabase.GetByCode(99999);
        Assert.Null(definition);
    }

    [Fact]
    public void Contains_WithValidCode_ReturnsTrue()
    {
        Assert.True(EpsgDatabase.Contains(4326));
        Assert.True(EpsgDatabase.Contains(3857));
        Assert.True(EpsgDatabase.Contains(27700));
    }

    [Fact]
    public void Contains_WithInvalidCode_ReturnsFalse()
    {
        Assert.False(EpsgDatabase.Contains(99999));
        Assert.False(EpsgDatabase.Contains(-1));
    }

    #endregion

    #region Search Tests

    [Fact]
    public void SearchByName_WithExactMatch_ReturnsCorrectResults()
    {
        var results = EpsgDatabase.SearchByName("WGS 84");
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Code == 4326);
    }

    [Fact]
    public void SearchByName_WithPartialMatch_ReturnsMultipleResults()
    {
        var results = EpsgDatabase.SearchByName("UTM");
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains("UTM", r.Name));
        Assert.Contains(results, r => r.Code == 32633);
        Assert.Contains(results, r => r.Code == 32634);
    }

    [Fact]
    public void SearchByName_CaseInsensitive_ReturnsResults()
    {
        var results1 = EpsgDatabase.SearchByName("wgs 84");
        var results2 = EpsgDatabase.SearchByName("WGS 84");
        var results3 = EpsgDatabase.SearchByName("Wgs 84");
        Assert.NotEmpty(results1);
        Assert.NotEmpty(results2);
        Assert.NotEmpty(results3);
        Assert.Equal(results1.Count, results2.Count);
        Assert.Equal(results2.Count, results3.Count);
    }

    [Fact]
    public void SearchByName_WithEmptyString_ReturnsEmptyList()
    {
        var results = EpsgDatabase.SearchByName("");
        Assert.Empty(results);
    }

    [Fact]
    public void SearchByName_WithNonExistentName_ReturnsEmptyList()
    {
        var results = EpsgDatabase.SearchByName("NonExistentCoordinateSystem");
        Assert.Empty(results);
    }

    #endregion

    #region Type-based Queries

    [Fact]
    public void GetGeographicSystems_ReturnsOnlyGeographicSystems()
    {
        var systems = EpsgDatabase.GetGeographicSystems();
        Assert.NotEmpty(systems);
        Assert.All(systems, s => Assert.Equal(ProjectionType.Geographic, s.Type));
        Assert.Contains(systems, s => s.Code == 4326); // WGS 84
        Assert.Contains(systems, s => s.Code == 4269); // NAD83
    }

    [Fact]
    public void GetProjectedSystems_ReturnsOnlyProjectedSystems()
    {
        var systems = EpsgDatabase.GetProjectedSystems();
        Assert.NotEmpty(systems);
        Assert.All(systems, s => Assert.Equal(ProjectionType.Projected, s.Type));
        Assert.Contains(systems, s => s.Code == 3857); // Web Mercator
        Assert.Contains(systems, s => s.Code == 27700); // British National Grid
    }

    [Fact]
    public void GetGeographicSystems_ExcludesDeprecatedSystems()
    {
        var systems = EpsgDatabase.GetGeographicSystems();
        Assert.All(systems, s => Assert.False(s.IsDeprecated));
    }

    [Fact]
    public void GetProjectedSystems_ExcludesDeprecatedSystems()
    {
        var systems = EpsgDatabase.GetProjectedSystems();
        Assert.All(systems, s => Assert.False(s.IsDeprecated));
    }

    #endregion

    #region Region-based Queries

    [Fact]
    public void GetByRegion_WithWorld_ReturnsWorldSystems()
    {
        var systems = EpsgDatabase.GetByRegion("World");
        Assert.NotEmpty(systems);
        Assert.Contains(systems, s => s.Code == 4326); // WGS 84
        Assert.Contains(systems, s => s.Code == 3857); // Web Mercator
    }

    [Fact]
    public void GetByRegion_WithEurope_ReturnsEuropeanSystems()
    {
        var systems = EpsgDatabase.GetByRegion("Europe");
        Assert.NotEmpty(systems);
        Assert.Contains(systems, s => s.Code == 4258); // ETRS89
        Assert.Contains(systems, s => s.Code == 32633); // UTM 33N
    }

    [Fact]
    public void GetByRegion_WithUK_ReturnsUKSystems()
    {
        var systems = EpsgDatabase.GetByRegion("United Kingdom");
        Assert.NotEmpty(systems);
        Assert.Contains(systems, s => s.Code == 4277); // OSGB 1936
        Assert.Contains(systems, s => s.Code == 27700); // British National Grid
    }

    [Fact]
    public void GetByRegion_CaseInsensitive_ReturnsResults()
    {
        var results1 = EpsgDatabase.GetByRegion("europe");
        var results2 = EpsgDatabase.GetByRegion("EUROPE");
        var results3 = EpsgDatabase.GetByRegion("Europe");
        Assert.NotEmpty(results1);
        Assert.NotEmpty(results2);
        Assert.NotEmpty(results3);
        Assert.Equal(results1.Count, results2.Count);
        Assert.Equal(results2.Count, results3.Count);
    }

    [Fact]
    public void GetByRegion_WithEmptyString_ReturnsEmptyList()
    {
        var systems = EpsgDatabase.GetByRegion("");
        Assert.Empty(systems);
    }

    #endregion

    #region ProjectionFile Integration

    [Fact]
    public void CreateProjectionFile_WithValidCode_ReturnsProjectionFile()
    {
        var projectionFile = EpsgDatabase.CreateProjectionFile(4326);
        Assert.NotNull(projectionFile);
        Assert.True(projectionFile.IsValid);
        Assert.Equal(ProjectionType.Geographic, projectionFile.ProjectionType);
        Assert.Equal("WGS 84", projectionFile.CoordinateSystemName);
        Assert.Equal(4326, projectionFile.EpsgCode);
    }

    [Fact]
    public void CreateProjectionFile_WithInvalidCode_ReturnsNull()
    {
        var projectionFile = EpsgDatabase.CreateProjectionFile(99999);
        Assert.Null(projectionFile);
    }

    [Fact]
    public void CreateProjectionFile_WithProjectedSystem_HasCorrectProperties()
    {
        var projectionFile = EpsgDatabase.CreateProjectionFile(3857);
        Assert.NotNull(projectionFile);
        Assert.True(projectionFile.IsValid);
        Assert.Equal(ProjectionType.Projected, projectionFile.ProjectionType);
        Assert.Equal("WGS 84 / Pseudo-Mercator", projectionFile.CoordinateSystemName);
        Assert.Equal(3857, projectionFile.EpsgCode);
        Assert.NotEmpty(projectionFile.Parameters);
    }

    #endregion

    #region Database Statistics

    [Fact]
    public void Count_ReturnsPositiveNumber()
    {
        var count = EpsgDatabase.Count;
        Assert.True(count > 0);
        Assert.True(count >= 10); // Should have at least 10 common coordinate systems
    }

    [Fact]
    public void AllCodes_ReturnsOrderedCodes()
    {
        var codes = EpsgDatabase.AllCodes.ToList();
        Assert.NotEmpty(codes);
        Assert.Contains(4326, codes);
        Assert.Contains(3857, codes);

        // Verify ordering
        for (var i = 1; i < codes.Count; i++)
        {
            Assert.True(codes[i] > codes[i - 1], "Codes should be in ascending order");
        }
    }

    [Fact]
    public void AllCodes_ContainsExpectedCommonCodes()
    {
        var codes = EpsgDatabase.AllCodes.ToList();
        Assert.Contains(4326, codes); // WGS 84
        Assert.Contains(3857, codes); // Web Mercator
        Assert.Contains(4269, codes); // NAD83
        Assert.Contains(27700, codes); // British National Grid
        Assert.Contains(32633, codes); // UTM 33N
    }

    #endregion

    #region WKT Validation

    [Fact]
    public void AllDefinitions_HaveValidWkt()
    {
        foreach (var code in EpsgDatabase.AllCodes)
        {
            var definition = EpsgDatabase.GetByCode(code);
            Assert.NotNull(definition);
            Assert.NotEmpty(definition.Wkt);

            // Verify WKT can be parsed
            var projectionFile = new ProjectionFile(definition.Wkt);
            Assert.True(projectionFile.IsValid, $"WKT for EPSG:{code} should be valid");
            Assert.Equal(definition.Type, projectionFile.ProjectionType);
        }
    }

    [Fact]
    public void GeographicSystems_HaveGeographicWkt()
    {
        var geographicSystems = EpsgDatabase.GetGeographicSystems();
        foreach (var system in geographicSystems.Take(5)) // Test first 5 to avoid long execution
        {
            var projectionFile = new ProjectionFile(system.Wkt);
            Assert.Equal(ProjectionType.Geographic, projectionFile.ProjectionType);
            Assert.StartsWith("GEOGCS[", system.Wkt);
        }
    }

    [Fact]
    public void ProjectedSystems_HaveProjectedWkt()
    {
        var projectedSystems = EpsgDatabase.GetProjectedSystems();
        foreach (var system in projectedSystems.Take(5)) // Test first 5 to avoid long execution
        {
            var projectionFile = new ProjectionFile(system.Wkt);
            Assert.Equal(ProjectionType.Projected, projectionFile.ProjectionType);
            Assert.StartsWith("PROJCS[", system.Wkt);
        }
    }

    #endregion
}
