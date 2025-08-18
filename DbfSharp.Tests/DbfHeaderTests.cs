using DbfSharp.Core;
using DbfSharp.Core.Enums;

namespace DbfSharp.Tests;

public class DbfHeaderTests
{
    [Theory]
    [InlineData(DbfVersion.VisualFoxPro, true)]
    [InlineData(DbfVersion.VisualFoxProAutoIncrement, true)]
    [InlineData(DbfVersion.VisualFoxProVarchar, true)]
    [InlineData(DbfVersion.DBase3Plus, false)]
    [InlineData(DbfVersion.DBase4WithMemo, false)]
    public void IsVisualFoxPro_ShouldReturnCorrectValue(DbfVersion version, bool expected)
    {
        var header = new DbfHeader((byte)version, 23, 10, 27, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        var actual = header.IsVisualFoxPro;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var header = new DbfHeader(3, 23, 10, 27, 123, 456, 789, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        var result = header.ToString();

        Assert.Contains("FoxBASE+/dBase III plus, no memory", result);
        Assert.Contains("123 records", result);
        Assert.Contains("Record length: 789", result);
    }
}
