namespace DbfSharp.ConsoleAot.Input;

/// <summary>
/// Abstract base class representing an input source for DBF data with resource management
/// </summary>
public abstract class InputSource(Stream stream, string originalPath, bool isStdin) : IDisposable
{
    public Stream Stream { get; } = stream ?? throw new ArgumentNullException(nameof(stream));
    public string OriginalPath { get; } = originalPath ?? throw new ArgumentNullException(nameof(originalPath));
    public bool IsStdin { get; } = isStdin;

    /// <summary>
    /// Gets the file size if available without consuming the stream
    /// </summary>
    /// <returns>The file size in bytes, or null if not determinable</returns>
    public virtual long? GetFileSize()
    {
        return StreamUtilities.EstimateSize(Stream);
    }

    /// <summary>
    /// Gets a display name for this input source suitable for user messages
    /// </summary>
    /// <returns>A user-friendly name for this input source</returns>
    public virtual string GetDisplayName()
    {
        return IsStdin ? "stdin" : OriginalPath;
    }

    /// <summary>
    /// Indicates whether this input source requires cleanup of temporary resources
    /// </summary>
    public virtual bool RequiresCleanup => false;

    public abstract void Dispose();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stream.Dispose();
        }
    }
}

/// <summary>
/// Input source backed by a regular file on disk
/// </summary>
public sealed class FileInputSource(Stream stream, string filePath) : InputSource(stream, filePath, false)
{
    public override long? GetFileSize()
    {
        try
        {
            if (Stream is FileStream fs)
            {
                return fs.Length;
            }

            return base.GetFileSize();
        }
        catch
        {
            return null;
        }
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Input source backed by a memory stream, typically from stdin
/// </summary>
public sealed class MemoryInputSource(MemoryStream stream, string originalPath)
    : InputSource(stream, originalPath, true)
{
    public override long? GetFileSize()
    {
        return Stream is MemoryStream ms ? ms.Length : base.GetFileSize();
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Input source backed by a temporary file that needs cleanup when disposed
/// </summary>
public sealed class TemporaryFileInputSource(FileStream stream, string originalPath, string tempFilePath)
    : InputSource(stream, originalPath, true)
{
    private readonly string _tempFilePath = tempFilePath ?? throw new ArgumentNullException(nameof(tempFilePath));
    private bool _disposed;

    public override bool RequiresCleanup => true;

    public override long? GetFileSize()
    {
        try
        {
            return Stream is FileStream fs ? fs.Length : base.GetFileSize();
        }
        catch
        {
            return null;
        }
    }

    public override string GetDisplayName()
    {
        return $"{base.GetDisplayName()} (temporary)";
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);
        GC.SuppressFinalize(this);
        _disposed = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            base.Dispose(disposing);
            StreamUtilities.CleanupTemporaryFile(_tempFilePath);
        }
    }
}
