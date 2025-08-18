using System.Text;
using DbfSharp.Core;
using DbfSharp.Core.Memo;

namespace DbfSharp.Tests;

public class VfpMemoFileTests
{
    [Fact]
    public void ReadLargeMemo_ShouldReadCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                // header
                fs.Write(new byte[512], 0, 512); // next available block
                fs.Seek(6, SeekOrigin.Begin);
                fs.Write(BitConverter.GetBytes((ushort)512), 0, 2); // block size

                // memo block
                fs.Seek(512, SeekOrigin.Begin);
                fs.Write(BitConverter.GetBytes(1), 0, 4); // type: Text
                fs.Write(BitConverter.GetBytes(2000), 0, 4); // length
                var largeMemo = new byte[2000];
                for (var i = 0; i < largeMemo.Length; i++)
                {
                    largeMemo[i] = (byte)'A';
                }
                fs.Write(largeMemo, 0, largeMemo.Length);
            }

            var options = new DbfReaderOptions();
            using (var memoFile = new VfpMemoFile(tempFile, options))
            {
                var memo = memoFile.GetMemo(1);

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
            {
                File.Delete(tempFile);
            }
        }
    }
}
