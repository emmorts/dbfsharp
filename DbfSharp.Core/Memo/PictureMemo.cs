namespace DbfSharp.Core.Memo;

/// <summary>
/// Represents picture memo data
/// </summary>
public sealed class PictureMemo : BinaryMemo
{
    /// <summary>
    /// Initializes a new instance of PictureMemo
    /// </summary>
    /// <param name="data">The picture data</param>
    public PictureMemo(ReadOnlyMemory<byte> data)
        : base(data) { }
}
