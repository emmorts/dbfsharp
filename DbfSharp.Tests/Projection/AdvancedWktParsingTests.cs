using DbfSharp.Core.Projection;

namespace DbfSharp.Tests.Projection;

/// <summary>
/// Tests for advanced WKT parsing capabilities including EPSG codes and complex coordinate systems
/// </summary>
public class AdvancedWktParsingTests
{
    #region Test Data Constants

    private const string WktWithEpsgCode =
        """GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]]""";

    private const string UtmWktWithEpsg =
        """PROJCS["WGS 84 / UTM zone 33N",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",15],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",500000],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Easting",EAST],AXIS["Northing",NORTH],AUTHORITY["EPSG","32633"]]""";

    private const string CompoundWktWithEpsg =
        """COMPD_CS["OSGB 1936 / British National Grid + ODN height",PROJCS["OSGB 1936 / British National Grid",GEOGCS["OSGB 1936",DATUM["OSGB_1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],TOWGS84[446.448,-125.157,542.060,0.1502,0.2470,0.8421,-20.4894],AUTHORITY["EPSG","6277"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4277"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",49],PARAMETER["central_meridian",-2],PARAMETER["scale_factor",0.9996012717],PARAMETER["false_easting",400000],PARAMETER["false_northing",-100000],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AUTHORITY["EPSG","27700"]],VERT_CS["ODN height",VERT_DATUM["Ordnance Datum Newlyn",2005,AUTHORITY["EPSG","5101"]],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Up",UP],AUTHORITY["EPSG","5701"]],AUTHORITY["EPSG","7405"]]""";

    private const string VerticalWkt =
        """VERT_CS["NAVD88 height",VERT_DATUM["North American Vertical Datum 1988",2005,AUTHORITY["EPSG","5103"]],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Up",UP],AUTHORITY["EPSG","5703"]]""";

    #endregion

    #region EPSG Code Extraction Tests

    [Fact]
    public void ProjectionFile_WithEpsgCode_ExtractsCodeCorrectly()
    {
        var projection = new ProjectionFile(WktWithEpsgCode);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Geographic, projection.ProjectionType);
        Assert.Equal("WGS 84", projection.CoordinateSystemName);
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal("4326", projection.AuthorityCode);
        Assert.Equal(4326, projection.EpsgCode);
    }

    [Fact]
    public void ProjectionFile_WithUtmEpsgCode_ExtractsProjectedSystemCorrectly()
    {
        var projection = new ProjectionFile(UtmWktWithEpsg);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Projected, projection.ProjectionType);
        Assert.Equal("WGS 84 / UTM zone 33N", projection.CoordinateSystemName);
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal("32633", projection.AuthorityCode);
        Assert.Equal(32633, projection.EpsgCode);
        Assert.Equal("Transverse_Mercator", projection.ProjectionName);

        // Check key UTM parameters
        Assert.True(projection.Parameters.ContainsKey("central_meridian"));
        Assert.Equal(15.0, projection.Parameters["central_meridian"]);
        Assert.True(projection.Parameters.ContainsKey("scale_factor"));
        Assert.Equal(0.9996, projection.Parameters["scale_factor"]);
    }

    [Fact]
    public void ProjectionFile_WithoutEpsgCode_HandlesGracefully()
    {
        var wktWithoutEpsg =
            """GEOGCS["GCS_WGS_1984",DATUM["D_WGS_1984",SPHEROID["WGS_1984",6378137.0,298.257223563]],PRIMEM["Greenwich",0.0],UNIT["Degree",0.0174532925199433]]""";
        var projection = new ProjectionFile(wktWithoutEpsg);
        Assert.True(projection.IsValid);
        Assert.Null(projection.Authority);
        Assert.Null(projection.AuthorityCode);
        Assert.Null(projection.EpsgCode);
    }

    #endregion

    #region Complex Coordinate System Tests

    [Fact]
    public void ProjectionFile_WithCompoundCoordinateSystem_ParsesCompleteStructure()
    {
        var projection = new ProjectionFile(CompoundWktWithEpsg);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Compound, projection.ProjectionType);
        Assert.Equal(
            "OSGB 1936 / British National Grid + ODN height",
            projection.CoordinateSystemName
        );
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal("7405", projection.AuthorityCode);
        Assert.Equal(7405, projection.EpsgCode);

        // Check child systems
        Assert.Equal(2, projection.ChildSystems.Count);

        // Horizontal component should be projected
        var horizontalSystem = projection.ChildSystems[0];
        Assert.Equal(ProjectionType.Projected, horizontalSystem.ProjectionType);
        Assert.Equal("OSGB 1936 / British National Grid", horizontalSystem.CoordinateSystemName);
        Assert.Equal(27700, horizontalSystem.EpsgCode);

        // Vertical component should be vertical
        var verticalSystem = projection.ChildSystems[1];
        Assert.Equal(ProjectionType.Vertical, verticalSystem.ProjectionType);
        Assert.Equal("ODN height", verticalSystem.CoordinateSystemName);
        Assert.Equal(5701, verticalSystem.EpsgCode);
    }

    [Fact]
    public void ProjectionFile_WithVerticalCoordinateSystem_ParsesCorrectly()
    {
        var projection = new ProjectionFile(VerticalWkt);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Vertical, projection.ProjectionType);
        Assert.Equal("NAVD88 height", projection.CoordinateSystemName);
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal("5703", projection.AuthorityCode);
        Assert.Equal(5703, projection.EpsgCode);
        Assert.Equal("North American Vertical Datum 1988", projection.VerticalDatum);
        Assert.Equal("metre", projection.LinearUnit);
        Assert.Equal(1.0, projection.LinearUnitValue);
    }

    #endregion

    #region Enhanced Parameter Extraction Tests

    [Fact]
    public void ProjectionFile_WithDetailedSpheroidInfo_ExtractsParametersCorrectly()
    {
        var projection = new ProjectionFile(UtmWktWithEpsg);
        Assert.True(projection.IsValid);
        Assert.Equal("WGS_1984", projection.Datum);
        Assert.Equal("WGS 84", projection.Spheroid);
        Assert.Equal(6378137.0, projection.SemiMajorAxis);
        Assert.Equal(298.257223563, projection.InverseFlattening);
        Assert.Equal("Greenwich", projection.PrimeMeridian);
        Assert.Equal(0.0, projection.PrimeMeridianValue);
        Assert.Equal("metre", projection.LinearUnit);
        Assert.Equal(1.0, projection.LinearUnitValue);
    }

    [Fact]
    public void ProjectionFile_WithAngularUnits_ParsesUnitsCorrectly()
    {
        var projection = new ProjectionFile(WktWithEpsgCode);
        Assert.True(projection.IsValid);
        Assert.Equal("degree", projection.AngularUnit);
        Assert.Equal(0.01745329251994328, projection.AngularUnitValue!.Value, 15); // High precision comparison
        Assert.Null(projection.LinearUnit); // Geographic systems don't have linear units at the root level
    }

    #endregion

    #region Summary and Display Tests

    [Fact]
    public void GetSummary_WithEpsgCode_IncludesEpsgInformation()
    {
        var projection = new ProjectionFile(WktWithEpsgCode);
        var summary = projection.GetSummary();
        Assert.Contains("EPSG: 4326", summary);
        Assert.Contains("WGS 84", summary);
        Assert.Contains("Geographic (Lat/Lon)", summary);
    }

    [Fact]
    public void GetSummary_WithProjectedSystem_IncludesProjectionName()
    {
        var projection = new ProjectionFile(UtmWktWithEpsg);
        var summary = projection.GetSummary();
        Assert.Contains("EPSG: 32633", summary);
        Assert.Contains("Projection: Transverse_Mercator", summary);
        Assert.Contains("Projected (Planar)", summary);
    }

    [Fact]
    public void GetSummary_WithCompoundSystem_ShowsComponents()
    {
        var projection = new ProjectionFile(CompoundWktWithEpsg);
        var summary = projection.GetSummary();
        Assert.Contains("EPSG: 7405", summary);
        Assert.Contains("Components: 2", summary);
        Assert.Contains("Compound (Horizontal + Vertical)", summary);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ProjectionFile_WithInvalidEpsgCode_HandlesGracefully()
    {
        var wktWithInvalidEpsg =
            """GEOGCS["Test",DATUM["Test",SPHEROID["Test",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.01745329251994328],AUTHORITY["EPSG","INVALID"]]""";
        var projection = new ProjectionFile(wktWithInvalidEpsg);
        Assert.True(projection.IsValid);
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal("INVALID", projection.AuthorityCode);
        Assert.Null(projection.EpsgCode); // Should be null for invalid numeric codes
    }

    [Fact]
    public void ProjectionFile_WithNonEpsgAuthority_ParsesCorrectly()
    {
        var wktWithOtherAuthority =
            """GEOGCS["Test",DATUM["Test",SPHEROID["Test",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.01745329251994328],AUTHORITY["ESRI","54004"]]""";
        var projection = new ProjectionFile(wktWithOtherAuthority);
        Assert.True(projection.IsValid);
        Assert.Equal("ESRI", projection.Authority);
        Assert.Equal("54004", projection.AuthorityCode);
        Assert.Null(projection.EpsgCode); // Should be null for non-EPSG authorities
    }

    #endregion
}
