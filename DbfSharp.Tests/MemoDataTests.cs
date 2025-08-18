using System.Text;
using DbfSharp.Core.Memo;

namespace DbfSharp.Tests;

public class MemoDataTests
{
    [Fact]
    public void TextMemo_ToString_ShouldReturnCorrectString()
    {
        var data = new ReadOnlyMemory<byte>("test"u8.ToArray());
        var memo = new TextMemo(data);

        var result = memo.ToString();

        Assert.Equal("test", result);
    }

    [Fact]
    public void BinaryMemo_ShouldStoreData()
    {
        var data = new ReadOnlyMemory<byte>([1, 2, 3]);
        var memo = new BinaryMemo(data);

        var result = (byte[])memo;

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void ObjectMemo_ShouldStoreData()
    {
        var data = new ReadOnlyMemory<byte>([1, 2, 3]);
        var memo = new ObjectMemo(data);

        var result = (byte[])memo;

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void PictureMemo_ShouldStoreData()
    {
        var data = new ReadOnlyMemory<byte>([1, 2, 3]);
        var memo = new PictureMemo(data);

        var result = (byte[])memo;

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }
}
