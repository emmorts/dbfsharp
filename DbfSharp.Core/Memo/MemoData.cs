namespace DbfSharp.Core.Memo;

/// <summary>
/// Base class for memo data with type information
/// </summary>
public abstract class MemoData
{
    /// <summary>
    /// Gets the raw memo data
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Initializes a new instance of MemoData
    /// </summary>
    /// <param name="data">The raw memo data</param>
    protected MemoData(ReadOnlyMemory<byte> data)
    {
        Data = data;
    }

    /// <summary>
    /// Implicitly converts MemoData to byte array
    /// </summary>
    public static implicit operator byte[](MemoData memo)
    {
        return memo.Data.ToArray();
    }

    /// <summary>
    /// Implicitly converts MemoData to ReadOnlySpan
    /// </summary>
    public static implicit operator ReadOnlySpan<byte>(MemoData memo)
    {
        return memo.Data.Span;
    }
}
