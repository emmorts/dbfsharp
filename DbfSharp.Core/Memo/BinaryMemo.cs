namespace DbfSharp.Core.Memo;

/// <summary>
/// Represents binary memo data (images, OLE objects, etc.)
/// </summary>
public class BinaryMemo : MemoData
{
    /// <summary>
    /// Initializes a new instance of BinaryMemo
    /// </summary>
    /// <param name="data">The binary data</param>
    public BinaryMemo(ReadOnlyMemory<byte> data)
        : base(data) { }
}
