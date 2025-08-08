using DbfSharp.Core.Enums;
using Xunit;

namespace DbfSharp.Tests;

public class DbfVersionExtensionsTests
{
    [Theory]
    [InlineData(DbfVersion.DBase2, "dBASE II / FoxBASE")]
    [InlineData(DbfVersion.DBase3Plus, "FoxBASE+/dBase III plus, no memory")]
    [InlineData(DbfVersion.DBase4WithMemo, "dBASE IV with memo")]
    [InlineData(DbfVersion.VisualFoxPro, "Visual FoxPro")]
    [InlineData(DbfVersion.Unknown, "Unknown (0xFF)")]
    public void GetDescription_ShouldReturnCorrectString(DbfVersion version, string expected)
    {
        // Act
        var actual = version.GetDescription();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(DbfVersion.DBase2, true)]
    [InlineData(DbfVersion.DBase3Plus, true)]
    [InlineData(DbfVersion.DBase4WithMemo, false)]
    [InlineData(DbfVersion.VisualFoxPro, false)]
    [InlineData(DbfVersion.Unknown, false)]
    public void IsLegacyFormat_ShouldReturnCorrectValue(DbfVersion version, bool expected)
    {
        // Act
        var actual = version.IsLegacyFormat();

        // Assert
        Assert.Equal(expected, actual);
    }
}
