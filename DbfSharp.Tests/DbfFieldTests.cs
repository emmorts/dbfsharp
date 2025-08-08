using DbfSharp.Core;
using DbfSharp.Core.Enums;
using Xunit;

namespace DbfSharp.Tests;

public class DbfFieldTests
{
    [Theory]
    [InlineData(FieldType.Character, typeof(string))]
    [InlineData(FieldType.Currency, typeof(decimal))]
    [InlineData(FieldType.Date, typeof(DateTime?))]
    [InlineData(FieldType.Timestamp, typeof(DateTime?))]
    [InlineData(FieldType.Double, typeof(double))]
    [InlineData(FieldType.Float, typeof(float?))]
    [InlineData(FieldType.General, typeof(byte[]))]
    [InlineData(FieldType.Integer, typeof(int))]
    [InlineData(FieldType.Logical, typeof(bool?))]
    [InlineData(FieldType.Memo, typeof(string))]
    [InlineData(FieldType.Numeric, typeof(decimal?))]
    [InlineData(FieldType.Picture, typeof(byte[]))]
    [InlineData(FieldType.TimestampAlternate, typeof(DateTime?))]
    [InlineData(FieldType.Varchar, typeof(string))]
    public void GetExpectedNetType_ShouldReturnCorrectType(FieldType dbfType, Type expectedType)
    {
        // Act
        var actualType = dbfType.GetExpectedNetType();

        // Assert
        Assert.Equal(expectedType, actualType);
    }

    [Theory]
    [InlineData(FieldType.Memo, true)]
    [InlineData(FieldType.General, true)]
    [InlineData(FieldType.Picture, true)]
    [InlineData(FieldType.Binary, true)]
    [InlineData(FieldType.Character, false)]
    [InlineData(FieldType.Numeric, false)]
    public void UsesMemoFile_ShouldReturnCorrectValue(FieldType dbfType, bool expected)
    {
        // Act
        var actual = dbfType.UsesMemoFile();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(FieldType.Character, true)]
    [InlineData(FieldType.Date, true)]
    [InlineData(FieldType.Float, true)]
    [InlineData(FieldType.Logical, true)]
    [InlineData(FieldType.Memo, true)]
    [InlineData(FieldType.Numeric, true)]
    [InlineData(FieldType.Currency, false)]
    [InlineData(FieldType.Integer, false)]
    public void SupportsNull_ShouldReturnCorrectValue(FieldType dbfType, bool expected)
    {
        // Act
        var actual = dbfType.SupportsNull();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToString_ShouldReturnNameAndType()
    {
        // Arrange
        var field = new DbfField("TEST", FieldType.Character, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0);

        // Act
        var result = field.ToString();

        // Assert
        Assert.Equal("TEST (Character, 10)", result);
    }

    [Fact]
    public void Equals_ShouldReturnTrueForSameField()
    {
        // Arrange
        var field1 = new DbfField("TEST", FieldType.Character, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0);
        var field2 = new DbfField("TEST", FieldType.Character, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0);

        // Act & Assert
        Assert.True(field1.Equals(field2));
        Assert.True(field1 == field2);
        Assert.False(field1 != field2);
        Assert.Equal(field1.GetHashCode(), field2.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentField()
    {
        // Arrange
        var field1 = new DbfField("TEST1", FieldType.Character, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0);
        var field2 = new DbfField("TEST2", FieldType.Numeric, 0, 12, 2, 0, 0, 0, 0, 0, 0, 0);

        // Act & Assert
        Assert.False(field1.Equals(field2));
        Assert.False(field1 == field2);
        Assert.True(field1 != field2);
        Assert.NotEqual(field1.GetHashCode(), field2.GetHashCode());
    }

    //[Theory]
    [InlineData("N", 10, 2, DbfVersion.DBase3Plus, true)] // Valid numeric
    [InlineData("C", 254, 0, DbfVersion.DBase3Plus, true)] // Valid character
    [InlineData("L", 1, 0, DbfVersion.DBase3Plus, true)] // Valid logical
    [InlineData("D", 8, 0, DbfVersion.DBase3Plus, true)] // Valid date
    [InlineData("M", 10, 0, DbfVersion.DBase3PlusWithMemo, true)] // Valid memo
    [InlineData("F", 20, 5, DbfVersion.DBase3Plus, true)] // Valid float
    [InlineData("I", 4, 0, DbfVersion.VisualFoxPro, true)] // Valid integer
    [InlineData("Y", 8, 2, DbfVersion.VisualFoxPro, true)] // Valid currency
    [InlineData("T", 8, 0, DbfVersion.VisualFoxPro, true)] // Valid datetime
    [InlineData("B", 8, 2, DbfVersion.VisualFoxPro, true)] // Valid double
    [InlineData("C", 255, 0, DbfVersion.DBase3Plus, false)] // Invalid char length
    [InlineData("N", 20, 21, DbfVersion.DBase3Plus, false)] // Invalid numeric precision
    [InlineData("L", 2, 0, DbfVersion.DBase3Plus, false)] // Invalid logical length
    [InlineData("D", 9, 0, DbfVersion.DBase3Plus, false)] // Invalid date length
    [InlineData("M", 11, 0, DbfVersion.DBase3Plus, false)] // Invalid memo length
    public void Validate_ShouldWorkForVariousFieldTypes(string type, byte length, byte decimalCount, DbfVersion version, bool expected)
    {
        // Arrange
        var fieldType = FieldTypeExtensions.FromChar(type[0]);
        var field = new DbfField("TEST", fieldType.Value, 0, length, decimalCount, 0, 0, 0, 0, 0, 0, 0);

        // Act
        var exception = Record.Exception(() => field.Validate(version));

        // Assert
        if (expected)
            Assert.Null(exception);
        else
            Assert.NotNull(exception);
    }
}
