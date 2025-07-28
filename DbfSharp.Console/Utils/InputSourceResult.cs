namespace DbfSharp.Console.Utils;

/// <summary>
/// Represents the result of resolving an input source, containing the stream and metadata
/// </summary>
public sealed class InputSourceResult : IDisposable
{
    public Stream Stream { get; }
    public string OriginalPath { get; }
    public bool IsStdin { get; }
    public bool IsTemporary => TempFilePath is not null;
    public string? TempFilePath { get; }

    internal InputSourceResult(Stream stream, string originalPath, bool isStdin, string? tempFilePath)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        OriginalPath = originalPath ?? throw new ArgumentNullException(nameof(originalPath));
        IsStdin = isStdin;
        TempFilePath = tempFilePath;
    }

    public void Dispose()
    {
        Stream.Dispose();

        if (TempFilePath is not null)
        {
            StdinHelper.CleanupTemporaryFile(TempFilePath);
        }
    }
}
