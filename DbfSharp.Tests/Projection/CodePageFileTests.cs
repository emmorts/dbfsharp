using System.Text;
using DbfSharp.Core.Projection;

namespace DbfSharp.Tests.Projection;

/// <summary>
/// Tests for the CodePageFile class and encoding detection functionality
/// </summary>
public class CodePageFileTests
{
    #region File Reading Tests

    [Fact]
    public async Task Read_WithUtf8File_ReadsCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "UTF-8", Encoding.UTF8);

        try
        {
            var codePageFile = await CodePageFile.ReadAsync(tempFile);
            Assert.Equal("UTF-8", codePageFile.CodePageIdentifier);
            Assert.Equal(tempFile, codePageFile.FilePath);
            Assert.True(codePageFile.IsValid);
            Assert.Equal(Encoding.UTF8, codePageFile.Encoding);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithUtf8File_ReadsCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "UTF-8", Encoding.UTF8);

        try
        {
            var codePageFile = await CodePageFile.ReadAsync(tempFile);
            Assert.Equal("UTF-8", codePageFile.CodePageIdentifier);
            Assert.Equal(tempFile, codePageFile.FilePath);
            Assert.True(codePageFile.IsValid);
            Assert.Equal("utf-8", codePageFile.Encoding.WebName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Read_WithFileContainingWhitespace_TrimsCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "   ISO-8859-1   \r\n\t  ", Encoding.UTF8);

        try
        {
            var codePageFile = await CodePageFile.ReadAsync(tempFile);
            Assert.Equal("ISO-8859-1", codePageFile.CodePageIdentifier);
            Assert.True(codePageFile.IsValid);
            Assert.Equal("iso-8859-1", codePageFile.Encoding.WebName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Read_WithNullFilePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("filePath", () => CodePageFile.Read(null!));
    }

    [Fact]
    public void Read_WithEmptyFilePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("filePath", () => CodePageFile.Read(string.Empty));
    }

    [Fact]
    public void Read_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file.cpg");
        var ex = Assert.Throws<FileNotFoundException>(() => CodePageFile.Read(nonExistentPath));
        Assert.Contains($"Code page file not found: {nonExistentPath}", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file.cpg");
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            CodePageFile.ReadAsync(nonExistentPath)
        );
        Assert.Contains($"Code page file not found: {nonExistentPath}", ex.Message);
    }

    #endregion

    #region Parse Method Tests

    [Theory]
    [InlineData("UTF-8", true)]
    [InlineData("utf-8", true)]
    [InlineData("UTF8", true)]
    [InlineData("ASCII", true)]
    [InlineData("US-ASCII", true)]
    [InlineData("Windows-1252", true)]
    [InlineData("ISO-8859-1", true)]
    [InlineData("CP1252", true)]
    public void Parse_WithValidEncodingName_ReturnsValidResult(
        string encodingName,
        bool expectedValid
    )
    {
        var result = CodePageFile.Parse(encodingName);
        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(encodingName, result.CodePageIdentifier);
        Assert.NotNull(result.Encoding);
    }

    [Theory]
    [InlineData("UTF-8", "utf-8")]
    [InlineData("ASCII", "us-ascii")]
    [InlineData("Windows-1252", "windows-1252")]
    [InlineData("ISO-8859-1", "iso-8859-1")]
    public void Parse_WithKnownEncoding_ReturnsCorrectWebName(string input, string expectedWebName)
    {
        var result = CodePageFile.Parse(input);
        Assert.Equal(expectedWebName, result.Encoding.WebName);
    }

    [Fact]
    public void Parse_WithUnknownEncoding_FallsBackToUtf8()
    {
        var result = CodePageFile.Parse("UnknownEncoding123");
        Assert.False(result.IsValid); // Invalid when encoding not found
        Assert.Equal("UnknownEncoding123", result.CodePageIdentifier);
        Assert.Equal(Encoding.UTF8, result.Encoding); // Falls back to UTF-8
    }

    [Fact]
    public void Parse_WithEmptyEncodingName_ReturnsInvalidResult()
    {
        var result = CodePageFile.Parse(string.Empty);
        Assert.False(result.IsValid);
        Assert.Equal("", result.CodePageIdentifier);
        Assert.Equal(Encoding.UTF8, result.Encoding); // Falls back to UTF-8
    }

    [Fact]
    public void Parse_WithWhitespaceOnlyEncodingName_ReturnsInvalidResult()
    {
        var result = CodePageFile.Parse("   \t\r\n   ");
        Assert.False(result.IsValid);
        Assert.Equal(Encoding.UTF8, result.Encoding); // Falls back to UTF-8
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        var encodingName = "UTF-8";
        var encoding = Encoding.UTF8;
        var filePath = @"C:\test\file.cpg";
        var codePageFile = new CodePageFile(encodingName, encoding, filePath);
        Assert.Equal(encodingName, codePageFile.CodePageIdentifier);
        Assert.Equal(encoding, codePageFile.Encoding);
        Assert.True(codePageFile.IsValid);
        Assert.Equal(filePath, codePageFile.FilePath);
    }

    [Fact]
    public void Constructor_WithNullFilePath_AllowsNullFilePath()
    {
        var codePageFile = new CodePageFile("UTF-8", Encoding.UTF8, null);
        Assert.Null(codePageFile.FilePath);
        Assert.True(codePageFile.IsValid);
    }

    [Fact]
    public void Constructor_InvalidOverload_SetsPropertiesCorrectly()
    {
        var encodingName = "UnknownEncoding";
        var filePath = @"C:\test\file.cpg";
        var codePageFile = new CodePageFile(encodingName, filePath);
        Assert.Equal(encodingName, codePageFile.CodePageIdentifier);
        Assert.Equal(Encoding.UTF8, codePageFile.Encoding); // Falls back to UTF-8
        Assert.False(codePageFile.IsValid);
        Assert.Equal(filePath, codePageFile.FilePath);
    }

    #endregion

    #region Real World Test Cases

    [Fact]
    public void Parse_WithWindows1252Encoding_WorksWithEncodingProvider()
    {
        // This test verifies that Windows-1252 encoding works after registering CodePagesEncodingProvider
        var result = CodePageFile.Parse("Windows-1252");
        Assert.True(result.IsValid);
        Assert.Equal("Windows-1252", result.CodePageIdentifier);
        Assert.Equal("windows-1252", result.Encoding.WebName);
        Assert.Equal(1252, result.Encoding.CodePage);
    }

    [Fact]
    public void Parse_WithRealWorldTestCase_Utf8_ParsesCorrectly()
    {
        // This tests the actual content from our test resources
        var result = CodePageFile.Parse("UTF-8");
        Assert.True(result.IsValid);
        Assert.Equal("UTF-8", result.CodePageIdentifier);
        Assert.Equal(Encoding.UTF8, result.Encoding);
        Assert.Equal(65001, result.Encoding.CodePage);
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("ASCII")]
    [InlineData("Windows-1252")]
    [InlineData("ISO-8859-1")]
    [InlineData("CP1252")]
    public void Parse_WithCommonEncodings_AllWorkCorrectly(string encodingName)
    {
        var result = CodePageFile.Parse(encodingName);
        Assert.True(result.IsValid);
        Assert.Equal(encodingName, result.CodePageIdentifier);
        Assert.NotNull(result.Encoding);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithValidEncoding_ReturnsReadableDescription()
    {
        var result = CodePageFile.Parse("UTF-8");
        var stringResult = result.ToString();
        Assert.NotEmpty(stringResult);
        Assert.Contains("UTF-8", stringResult);
    }

    [Fact]
    public void ToString_WithInvalidEncoding_ReturnsReadableDescription()
    {
        var result = CodePageFile.Parse("");
        var stringResult = result.ToString();
        Assert.NotEmpty(stringResult);
    }

    #endregion
}
