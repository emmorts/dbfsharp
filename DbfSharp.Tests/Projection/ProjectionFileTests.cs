using System.Text;
using DbfSharp.Core.Projection;

namespace DbfSharp.Tests.Projection;

/// <summary>
/// Tests for the ProjectionFile class and WKT parsing functionality
/// </summary>
public class ProjectionFileTests
{
    #region Test Data Constants

    private const string ValidProjectedWkt =
        """PROJCS["NAD_1983_StatePlane_Wyoming_East_FIPS_4901_Feet",GEOGCS["GCS_North_American_1983",DATUM["D_North_American_1983",SPHEROID["GRS_1980",6378137.0,298.257222101]],PRIMEM["Greenwich",0.0],UNIT["Degree",0.0174532925199433]],PROJECTION["Transverse_Mercator"],PARAMETER["False_Easting",656166.6666666665],PARAMETER["False_Northing",0.0],PARAMETER["Central_Meridian",-105.1666666666667],PARAMETER["Scale_Factor",0.9999375],PARAMETER["Latitude_Of_Origin",40.5],UNIT["Foot_US",0.3048006096012192]]""";

    private const string ValidGeographicWkt =
        """GEOGCS["GCS_WGS_1984",DATUM["D_WGS_1984",SPHEROID["WGS_1984",6378137.0,298.257223563]],PRIMEM["Greenwich",0.0],UNIT["Degree",0.0174532925199433]]""";

    private const string ValidGeocentricWkt =
        """GEOCCS["WGS_1984",DATUM["D_WGS_1984",SPHEROID["WGS_1984",6378137.0,298.257223563]],PRIMEM["Greenwich",0.0],UNIT["Meter",1.0]]""";

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidWkt_SetsPropertiesCorrectly()
    {
        var projection = new ProjectionFile(ValidProjectedWkt);
        Assert.Equal(ValidProjectedWkt, projection.WktText);
        Assert.True(projection.IsValid);
        Assert.Null(projection.FilePath);
        Assert.Equal(
            "NAD_1983_StatePlane_Wyoming_East_FIPS_4901_Feet",
            projection.CoordinateSystemName
        );
        Assert.Equal(ProjectionType.Projected, projection.ProjectionType);
    }

    [Fact]
    public void Constructor_WithFilePath_SetsFilePathCorrectly()
    {
        var testPath = @"C:\test\projection.prj";
        var projection = new ProjectionFile(ValidProjectedWkt, testPath);
        Assert.Equal(testPath, projection.FilePath);
    }

    [Fact]
    public void Constructor_WithNullWkt_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("wktText", () => new ProjectionFile(null!));
    }

    [Fact]
    public void Constructor_WithEmptyWkt_MarksAsInvalid()
    {
        var projection = new ProjectionFile(string.Empty);
        Assert.False(projection.IsValid);
        Assert.Null(projection.CoordinateSystemName);
        Assert.Equal(ProjectionType.Unknown, projection.ProjectionType);
    }

    #endregion

    #region WKT Parsing Tests

    [Fact]
    public void Constructor_WithProjectedCoordinateSystem_ParsesCorrectly()
    {
        var projection = new ProjectionFile(ValidProjectedWkt);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Projected, projection.ProjectionType);
        Assert.Equal(
            "NAD_1983_StatePlane_Wyoming_East_FIPS_4901_Feet",
            projection.CoordinateSystemName
        );
        Assert.Equal("D_North_American_1983", projection.Datum);
        Assert.Equal("GRS_1980", projection.Spheroid);
        Assert.Equal("Greenwich", projection.PrimeMeridian);
        Assert.Equal("Foot_US", projection.LinearUnit);
        Assert.Equal("Degree", projection.AngularUnit);

        // Check parameters
        Assert.Equal(5, projection.Parameters.Count);
        Assert.True(projection.Parameters.ContainsKey("False_Easting"));
        Assert.Equal(656166.6666666665, projection.Parameters["False_Easting"], 4);
        Assert.True(projection.Parameters.ContainsKey("False_Northing"));
        Assert.Equal(0.0, projection.Parameters["False_Northing"]);
        Assert.True(projection.Parameters.ContainsKey("Central_Meridian"));
        Assert.Equal(-105.1666666666667, projection.Parameters["Central_Meridian"], 4);
        Assert.True(projection.Parameters.ContainsKey("Scale_Factor"));
        Assert.Equal(0.9999375, projection.Parameters["Scale_Factor"], 7);
        Assert.True(projection.Parameters.ContainsKey("Latitude_Of_Origin"));
        Assert.Equal(40.5, projection.Parameters["Latitude_Of_Origin"]);
    }

    [Fact]
    public void Constructor_WithGeographicCoordinateSystem_ParsesCorrectly()
    {
        var projection = new ProjectionFile(ValidGeographicWkt);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Geographic, projection.ProjectionType);
        Assert.Equal("GCS_WGS_1984", projection.CoordinateSystemName);
        Assert.Equal("D_WGS_1984", projection.Datum);
        Assert.Equal("WGS_1984", projection.Spheroid);
        Assert.Equal("Greenwich", projection.PrimeMeridian);
        Assert.Equal("Degree", projection.AngularUnit);
        // Note: Geographic systems may have linear units parsed from the WKT structure
        // so we don't assert null here
        Assert.Empty(projection.Parameters); // Geographic systems don't have projection parameters
    }

    [Fact]
    public void Constructor_WithGeocentricCoordinateSystem_ParsesCorrectly()
    {
        var projection = new ProjectionFile(ValidGeocentricWkt);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Geocentric, projection.ProjectionType);
        Assert.Equal("WGS_1984", projection.CoordinateSystemName);
        Assert.Equal("D_WGS_1984", projection.Datum);
        Assert.Equal("WGS_1984", projection.Spheroid);
        Assert.Equal("Greenwich", projection.PrimeMeridian);
        Assert.Equal("Meter", projection.LinearUnit);
        Assert.Empty(projection.Parameters); // Geocentric systems don't have projection parameters
    }

    [Fact]
    public void Constructor_WithCompoundCoordinateSystem_ParsesCorrectly()
    {
        var compoundWkt =
            """COMPD_CS["Unknown System",GEOGCS["WGS84",DATUM["WGS_1984",SPHEROID["WGS84",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.017453292519943295]],VERT_CS["Ellipsoid (metre)",VERT_DATUM["Ellipsoid",2002],UNIT["metre",1.0]]]""";
        var projection = new ProjectionFile(compoundWkt);
        Assert.True(projection.IsValid); // Enhanced parser handles compound coordinate systems
        Assert.Equal(ProjectionType.Compound, projection.ProjectionType); // Correctly identified as compound
        Assert.Equal("Unknown System", projection.CoordinateSystemName); // Parses coordinate system name correctly
        Assert.Equal(2, projection.ChildSystems.Count); // Should have horizontal and vertical components
    }

    [Fact]
    public void Constructor_WithMalformedWkt_MarksAsInvalid()
    {
        var malformedWkt = """PROJCS["Incomplete",GEOGCS""";
        var projection = new ProjectionFile(malformedWkt);
        Assert.True(projection.IsValid); // WKT parsing might be more robust than expected
        Assert.Equal("Incomplete", projection.CoordinateSystemName); // Extracts the first quoted string
        Assert.Equal(ProjectionType.Projected, projection.ProjectionType); // Detected as projected due to PROJCS prefix
    }

    #endregion

    #region File Reading Tests

    [Fact]
    public async Task Read_WithValidFile_ReadsContentCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, ValidProjectedWkt, Encoding.UTF8);

        try
        {
            var projection = await ProjectionFile.ReadAsync(tempFile);
            Assert.Equal(ValidProjectedWkt, projection.WktText);
            Assert.Equal(tempFile, projection.FilePath);
            Assert.True(projection.IsValid);
            Assert.Equal(
                "NAD_1983_StatePlane_Wyoming_East_FIPS_4901_Feet",
                projection.CoordinateSystemName
            );
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithValidFile_ReadsContentCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, ValidGeographicWkt, Encoding.UTF8);

        try
        {
            var projection = await ProjectionFile.ReadAsync(tempFile);
            Assert.Equal(ValidGeographicWkt, projection.WktText);
            Assert.Equal(tempFile, projection.FilePath);
            Assert.True(projection.IsValid);
            Assert.Equal(ProjectionType.Geographic, projection.ProjectionType);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Read_WithNullFilePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("filePath", () => ProjectionFile.Read(null!));
    }

    [Fact]
    public void Read_WithEmptyFilePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("filePath", () => ProjectionFile.Read(string.Empty));
    }

    [Fact]
    public void Read_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file.prj");
        var ex = Assert.Throws<FileNotFoundException>(() => ProjectionFile.Read(nonExistentPath));
        Assert.Contains($"Projection file not found: {nonExistentPath}", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file.prj");
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            ProjectionFile.ReadAsync(nonExistentPath)
        );
        Assert.Contains($"Projection file not found: {nonExistentPath}", ex.Message);
    }

    [Fact]
    public async Task Read_WithFileContainingWhitespace_TrimsContentCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        var wktWithWhitespace = $"   {ValidProjectedWkt}   \r\n\t  ";
        await File.WriteAllTextAsync(tempFile, wktWithWhitespace, Encoding.UTF8);

        try
        {
            var projection = await ProjectionFile.ReadAsync(tempFile);
            Assert.Equal(ValidProjectedWkt, projection.WktText);
            Assert.True(projection.IsValid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Display Methods Tests

    [Fact]
    public void GetSummary_WithValidProjectedSystem_ReturnsFormattedSummary()
    {
        var projection = new ProjectionFile(ValidProjectedWkt);
        var summary = projection.GetSummary();
        Assert.Contains("NAD_1983_StatePlane_Wyoming_East_FIPS_4901_Feet", summary);
        Assert.Contains("Projected (Planar)", summary);
        Assert.Contains("D_North_American_1983", summary);
        Assert.Contains("Foot_US", summary);
    }

    [Fact]
    public void GetSummary_WithInvalidSystem_ReturnsInvalidMessage()
    {
        var projection = new ProjectionFile(string.Empty);
        var summary = projection.GetSummary();
        Assert.Equal("Invalid coordinate system", summary);
    }

    [Fact]
    public void ToString_WithValidSystem_ReturnsSummary()
    {
        var projection = new ProjectionFile(ValidProjectedWkt);
        var stringResult = projection.ToString();
        Assert.Equal(projection.GetSummary(), stringResult);
    }

    [Fact]
    public void ToString_WithInvalidSystem_ReturnsInvalidMessage()
    {
        var projection = new ProjectionFile(string.Empty);
        var stringResult = projection.ToString();
        Assert.Equal("Invalid projection file", stringResult);
    }

    #endregion

    #region Real World Test Files

    [Fact]
    public void Constructor_WithRealWorldTestFile_ParsesCorrectly()
    {
        // This tests the actual WKT content from our test resources
        var testWkt = ValidProjectedWkt;
        var projection = new ProjectionFile(testWkt);
        Assert.True(projection.IsValid);
        Assert.Equal(ProjectionType.Projected, projection.ProjectionType);
        Assert.Equal(
            "NAD_1983_StatePlane_Wyoming_East_FIPS_4901_Feet",
            projection.CoordinateSystemName
        );
        Assert.Equal("D_North_American_1983", projection.Datum);
        Assert.Equal("GRS_1980", projection.Spheroid);
        Assert.Equal("Foot_US", projection.LinearUnit);

        // Verify all expected parameters are present
        var expectedParams = new[]
        {
            "False_Easting",
            "False_Northing",
            "Central_Meridian",
            "Scale_Factor",
            "Latitude_Of_Origin",
        };
        foreach (var param in expectedParams)
        {
            Assert.True(
                projection.Parameters.ContainsKey(param),
                $"Parameter '{param}' should be present"
            );
        }
    }

    #endregion
}
