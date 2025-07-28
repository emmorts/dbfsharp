namespace DbfSharp.Console.Utils;

/// <summary>
/// A stream adapter that bridges Utf8JsonWriter's byte output to a TextWriter's character interface
/// This allows streaming JSON output to any TextWriter without intermediate buffering
/// </summary>
internal sealed class TextWriterStream(TextWriter writer) : Stream
{
    private readonly TextWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    private readonly System.Text.Decoder _decoder = System.Text.Encoding.UTF8.GetDecoder();

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count == 0)
        {
            return;
        }

        WriteCore(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            return;
        }

        WriteCore(buffer);
    }

    /// <summary>
    /// Core synchronous write implementation that handles both array-based and span-based writes efficiently
    /// </summary>
    private void WriteCore(ReadOnlySpan<byte> buffer)
    {
        var maxCharCount = _decoder.GetCharCount(buffer, flush: false);
        if (maxCharCount == 0)
        {
            return;
        }

        // use stack alloc for small buffers, heap for larger ones
        const int stackAllocThreshold = 512;
        char[]? rentedBuffer = null;
        var charBuffer = maxCharCount <= stackAllocThreshold
            ? stackalloc char[maxCharCount]
            : (rentedBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(maxCharCount)).AsSpan(0, maxCharCount);

        try
        {
            var charCount = _decoder.GetChars(buffer, charBuffer, flush: false);
            if (charCount > 0)
            {
                _writer.Write(charBuffer[..charCount]);
            }
        }
        finally
        {
            if (rentedBuffer != null)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rentedBuffer);
            }
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (count == 0)
        {
            return;
        }

        var segment = new ArraySegment<byte>(buffer, offset, count);

        await WriteAsyncCore(segment, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty)
        {
            return;
        }

        // handle memory-based input by extracting to a safe format for async operations
        if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out var segment))
        {
            // memory is backed by an array - we can use it safely in async context
            await WriteAsyncCore(segment, cancellationToken);
        }
        else
        {
            // memory is not array-backed (could be native memory, etc.)
            // need to copy to a temporary array for safe async processing
            var tempArray = buffer.ToArray();
            var tempSegment = new ArraySegment<byte>(tempArray, 0, tempArray.Length);
            await WriteAsyncCore(tempSegment, cancellationToken);
        }
    }

    /// <summary>
    /// Core async write implementation that works with array segments to avoid span limitations
    /// </summary>
    private async ValueTask WriteAsyncCore(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var maxCharCount = _decoder.GetCharCount(buffer.Array!, buffer.Offset, buffer.Count, flush: false);
        if (maxCharCount == 0)
        {
            return;
        }

        var rentedCharBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(maxCharCount);

        try
        {
            var charCount = _decoder.GetChars(buffer.Array!, buffer.Offset, buffer.Count, rentedCharBuffer, 0,
                flush: false);
            if (charCount > 0)
            {
                await _writer.WriteAsync(rentedCharBuffer.AsMemory(0, charCount), cancellationToken);
            }
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(rentedCharBuffer);
        }
    }

    public override void Flush()
    {
        FlushDecoder();
        _writer.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await FlushDecoderAsync(cancellationToken);
        await _writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Flushes any remaining characters from the decoder synchronously
    /// </summary>
    private void FlushDecoder()
    {
        var emptyBytes = Array.Empty<byte>();
        var finalCharCount = _decoder.GetCharCount(emptyBytes, 0, 0, flush: true);
        if (finalCharCount == 0)
        {
            return;
        }

        // use stack alloc for small final buffers, array pool for larger ones
        const int stackAllocThreshold = 512;

        if (finalCharCount <= stackAllocThreshold)
        {
            var finalBuffer = new char[finalCharCount];
            var actualCharCount = _decoder.GetChars(emptyBytes, 0, 0, finalBuffer, 0, flush: true);
            if (actualCharCount > 0)
            {
                _writer.Write(finalBuffer.AsSpan(0, actualCharCount));
            }
        }
        else
        {
            var rentedBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(finalCharCount);
            try
            {
                var actualCharCount = _decoder.GetChars(emptyBytes, 0, 0, rentedBuffer, 0, flush: true);
                if (actualCharCount > 0)
                {
                    _writer.Write(rentedBuffer.AsSpan(0, actualCharCount));
                }
            }
            finally
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Flushes any remaining characters from the decoder asynchronously
    /// </summary>
    private async ValueTask FlushDecoderAsync(CancellationToken cancellationToken)
    {
        // use empty array for flushing - this is the standard pattern for decoder finalization
        var emptyBytes = Array.Empty<byte>();
        var finalCharCount = _decoder.GetCharCount(emptyBytes, 0, 0, flush: true);
        if (finalCharCount == 0)
        {
            return;
        }

        // for async operations, always use array pool (no stack allocation possible)
        var rentedBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(finalCharCount);

        try
        {
            var actualCharCount = _decoder.GetChars(emptyBytes, 0, 0, rentedBuffer, 0, flush: true);
            if (actualCharCount > 0)
            {
                await _writer.WriteAsync(rentedBuffer.AsMemory(0, actualCharCount), cancellationToken);
            }
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(rentedBuffer);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushDecoder();
        }

        base.Dispose(disposing);
    }
}
