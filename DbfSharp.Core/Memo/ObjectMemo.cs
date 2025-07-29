namespace DbfSharp.Core.Memo;

/// <summary>
/// Represents OLE object memo data
/// </summary>
public sealed class ObjectMemo : BinaryMemo
{
    /// <summary>
    /// Initializes a new instance of ObjectMemo
    /// </summary>
    /// <param name="data">The OLE object data</param>
    public ObjectMemo(ReadOnlyMemory<byte> data)
        : base(data) { }
}
