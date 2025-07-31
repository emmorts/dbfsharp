using DbfSharp.ConsoleAot.Input;

namespace DbfSharp.ConsoleAot.Extensions;

public static class InputSourceResultExtensions
{
    /// <summary>
    /// Gets the file size from an input source result
    /// </summary>
    /// <param name="inputSource">The input source to get the size from</param>
    /// <returns>The file size in bytes, or null if not available</returns>
    public static long? GetFileSize(this InputSource inputSource)
    {
        try
        {
            if (inputSource.Stream is FileStream fs)
            {
                return fs.Length;
            }

            if (inputSource.Stream.CanSeek)
            {
                return inputSource.Stream.Length;
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
