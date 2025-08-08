using System.IO;
using System.Text;
using DbfSharp.Core;
using DbfSharp.Core.Memo;
using Xunit;

namespace DbfSharp.Tests;

public class Db4MemoFileTests
{
    [Fact]
    public void ReadLargeMemo_ShouldReadCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                // Header
                fs.Write(new byte[512], 0, 512); // Next available block
                fs.Seek(4, SeekOrigin.Begin);
                fs.Write(BitConverter.GetBytes((ushort)512), 0, 2); // Block size

                // Memo block
                fs.Seek(512, SeekOrigin.Begin);
                fs.Write(new byte[] { 0xFF, 0xFF, 0x08, 0x00 }, 0, 4); // Signature
                fs.Write(BitConverter.GetBytes(2000), 0, 4); // Length
                var largeMemo = new byte[2000];
                for (int i = 0; i < largeMemo.Length; i++)
                {
                    largeMemo[i] = (byte)'A';
                }
                fs.Write(largeMemo, 0, largeMemo.Length);
            }

            var options = new DbfReaderOptions();
            using (var memoFile = new Db4MemoFile(tempFile, options))
            {
                // Act
                var memo = memoFile.GetMemo(1);

                // Assert
                Assert.NotNull(memo);
                var textMemo = memo as TextMemo;
                Assert.NotNull(textMemo);
                var text = textMemo.ToString(Encoding.ASCII);
                Assert.Equal(2000, text.Length);
                Assert.All(text, c => Assert.Equal('A', c));
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
