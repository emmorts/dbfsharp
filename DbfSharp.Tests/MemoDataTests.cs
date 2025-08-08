using System;
using System.Text;
using DbfSharp.Core.Memo;
using Xunit;

namespace DbfSharp.Tests;

public class MemoDataTests
{
    [Fact]
    public void TextMemo_ToString_ShouldReturnCorrectString()
    {
        // Arrange
        var data = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("test"));
        var memo = new TextMemo(data);

        // Act
        var result = memo.ToString();

        // Assert
        Assert.Equal("test", result);
    }

    [Fact]
    public void BinaryMemo_ShouldStoreData()
    {
        // Arrange
        var data = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var memo = new BinaryMemo(data);

        // Act
        var result = (byte[])memo;

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void ObjectMemo_ShouldStoreData()
    {
        // Arrange
        var data = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var memo = new ObjectMemo(data);

        // Act
        var result = (byte[])memo;

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void PictureMemo_ShouldStoreData()
    {
        // Arrange
        var data = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var memo = new PictureMemo(data);

        // Act
        var result = (byte[])memo;

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }
}
