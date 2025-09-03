using DbfSharp.Core.Projection;

namespace DbfSharp.Tests.Projection;

/// <summary>
/// Tests for ProjectionType enumeration and its extension methods
/// </summary>
public class ProjectionTypeTests
{
    #region GetDescription Extension Method Tests

    [Theory]
    [InlineData(ProjectionType.Geographic, "Geographic (Lat/Lon)")]
    [InlineData(ProjectionType.Projected, "Projected (Planar)")]
    [InlineData(ProjectionType.Geocentric, "Geocentric (3D Cartesian)")]
    [InlineData(ProjectionType.Unknown, "Unknown")]
    public void GetDescription_WithValidProjectionType_ReturnsCorrectDescription(
        ProjectionType projectionType,
        string expectedDescription
    )
    {
        var description = projectionType.GetDescription();
        Assert.Equal(expectedDescription, description);
    }

    [Fact]
    public void GetDescription_WithInvalidProjectionType_ReturnsUnknownWithNumericValue()
    {
        var invalidType = (ProjectionType)999;
        var description = invalidType.GetDescription();
        Assert.Equal("Unknown (999)", description);
    }

    [Fact]
    public void GetDescription_WithAllDefinedValues_CoverAllEnumMembers()
    {
        var definedValues = Enum.GetValues<ProjectionType>();

        // Act & Assert - Ensure no exceptions are thrown for any defined value
        foreach (var value in definedValues)
        {
            var description = value.GetDescription();
            Assert.NotNull(description);
            Assert.NotEmpty(description);
            Assert.False(
                description.StartsWith("Unknown ("),
                "Should not fall back to numeric format for defined values"
            );
        }
    }

    #endregion

    #region Enum Value Tests

    [Fact]
    public void ProjectionType_HasExpectedValues()
    {
        // Assert - Verify all expected enum values exist
        Assert.True(Enum.IsDefined(typeof(ProjectionType), ProjectionType.Unknown));
        Assert.True(Enum.IsDefined(typeof(ProjectionType), ProjectionType.Geographic));
        Assert.True(Enum.IsDefined(typeof(ProjectionType), ProjectionType.Projected));
        Assert.True(Enum.IsDefined(typeof(ProjectionType), ProjectionType.Geocentric));
    }

    [Fact]
    public void ProjectionType_Unknown_HasZeroValue()
    {
        // Assert - Unknown should be the default value (0)
        Assert.Equal(0, (int)ProjectionType.Unknown);
    }

    [Theory]
    [InlineData(ProjectionType.Unknown, 0)]
    [InlineData(ProjectionType.Geographic, 1)]
    [InlineData(ProjectionType.Projected, 2)]
    [InlineData(ProjectionType.Geocentric, 3)]
    public void ProjectionType_HasExpectedNumericValues(ProjectionType type, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)type);
    }

    #endregion
}
