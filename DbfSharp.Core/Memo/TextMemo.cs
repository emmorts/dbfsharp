namespace DbfSharp.Core.Memo;

/// <summary>
/// Represents text memo data
/// </summary>
public sealed class TextMemo : MemoData
{
    /// <summary>
    /// Initializes a new instance of TextMemo
    /// </summary>
    /// <param name="data">The text data</param>
    public TextMemo(ReadOnlyMemory<byte> data)
        : base(data) { }

    /// <summary>
    /// Converts the memo to a string using the specified encoding
    /// </summary>
    /// <param name="encoding">The encoding to use</param>
    /// <returns>The memo as a string</returns>
    public string ToString(System.Text.Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        return encoding.GetString(Data.Span);
    }

    /// <summary>
    /// Returns the string representation using UTF-8 encoding
    /// </summary>
    public override string ToString()
    {
        return ToString(System.Text.Encoding.UTF8);
    }
}
