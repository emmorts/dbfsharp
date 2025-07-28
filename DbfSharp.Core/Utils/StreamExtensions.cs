namespace DbfSharp.Core.Utils;

/// <summary>
/// Provides extension methods for the Stream class.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
    /// This method reads from the stream until the buffer is completely filled.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The region of memory to write the data into.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="EndOfStreamException">Thrown if the end of the stream is reached before the buffer is filled.</exception>
    public static async Task ReadExactlyAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer[totalBytesRead..], cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException("The stream ended before the buffer could be filled.");
            }
            
            totalBytesRead += bytesRead;
        }
    }
}
