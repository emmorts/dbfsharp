using DbfSharp.Console.Utils;

namespace DbfSharp.Console.Extensions;

/// <summary>
/// Extension methods for working with input sources efficiently
/// </summary>
public static class InputSourceExtensions
{
    /// <summary>
    /// Creates a binary reader optimized for the input source type
    /// </summary>
    public static BinaryReader CreateOptimizedBinaryReader(this InputSourceResult inputSource)
    {
        if (inputSource is not { IsStdin: true, IsTemporary: false })
        {
            return new BinaryReader(inputSource.Stream, System.Text.Encoding.UTF8, leaveOpen: false);
        }

        var bufferedStream = inputSource.Stream is BufferedStream
            ? inputSource.Stream
            : new BufferedStream(inputSource.Stream, 65536);
        
        return new BinaryReader(bufferedStream, System.Text.Encoding.UTF8, leaveOpen: false);
    }

    /// <summary>
    /// Checks if the input source supports seeking operations
    /// </summary>
    public static bool CanSeek(this InputSourceResult inputSource)
    {
        return inputSource.Stream.CanSeek;
    }

    /// <summary>
    /// Gets the length of the input if available, otherwise returns null
    /// </summary>
    public static long? TryGetLength(this InputSourceResult inputSource)
    {
        try
        {
            return inputSource.Stream.CanSeek ? inputSource.Stream.Length : null;
        }
        catch
        {
            return null;
        }
    }
}
