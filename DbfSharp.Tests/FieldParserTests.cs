using System.Text;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Tests;

public class FieldParserTests
{
    private readonly FieldParser _parser = new(DbfVersion.DBase3Plus);
    private readonly DbfReaderOptions _options = new();

    [Theory]
    [InlineData("123.45", 123.45f)]
    [InlineData("-123.45", -123.45f)]
    [InlineData("   .45", .45f)]
    [InlineData("1.23E+2", 123f)]
    public void ParseFloat_ShouldParseCorrectly(string input, float expected)
    {
        var field = new DbfField(
            "TEST",
            FieldType.Float,
            0,
            (byte)input.Length,
            2,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        );
        var data = Encoding.ASCII.GetBytes(input);

        var result = _parser.Parse(field, data, null, Encoding.ASCII, _options);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(12345678, 1234.5678)]
    public void ParseCurrency_ShouldParseCorrectly(long input, decimal expected)
    {
        var field = new DbfField("TEST", FieldType.Currency, 0, 8, 4, 0, 0, 0, 0, 0, 0, 0);
        var data = BitConverter.GetBytes(input);

        var result = _parser.Parse(field, data, null, Encoding.ASCII, _options);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseAutoincrement_ShouldParseCorrectly()
    {
        var field = new DbfField("TEST", FieldType.Autoincrement, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0);
        var data = BitConverter.GetBytes(123);

        var result = _parser.Parse(field, data, null, Encoding.ASCII, _options);

        Assert.Equal(123, result);
    }

    [Fact]
    public void ParseFlags_ShouldParseCorrectly()
    {
        var field = new DbfField("TEST", FieldType.Flags, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0);
        var data = new byte[] { 0x01 };

        var result = _parser.Parse(field, data, null, Encoding.ASCII, _options);

        Assert.Equal(new byte[] { 0x01 }, result);
    }
}
