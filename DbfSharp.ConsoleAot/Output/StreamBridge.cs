namespace DbfSharp.ConsoleAot.Output;

/// <summary>
/// A stream adapter that bridges Utf8JsonWriter's byte output to a TextWriter's character interface
/// This allows streaming JSON output to any TextWriter without intermediate buffering
/// </summary>
internal sealed class StreamBridge(TextWriter writer) : Stream
{
    private readonly TextWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    private readonly System.Text.Decoder _decoder = System.Text.Encoding.UTF8.GetDecoder();
    private const int SmallBufferThreshold = 512;
    private bool _disposed;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !_disposed;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (count == 0)
        {
            return;
        }

        WriteCore(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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

        char[]? rentedBuffer = null;
        var charBuffer =
            maxCharCount <= SmallBufferThreshold
                ? stackalloc char[maxCharCount]
                : (rentedBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(maxCharCount)).AsSpan(
                    0,
                    maxCharCount
                );

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

    public override async Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (count == 0)
        {
            return;
        }

        var segment = new ArraySegment<byte>(buffer, offset, count);
        await WriteAsyncCore(segment, cancellationToken);
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.IsEmpty)
        {
            return;
        }

        if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out var segment))
        {
            await WriteAsyncCore(segment, cancellationToken);
        }
        else
        {
            var tempArray = buffer.ToArray();
            var tempSegment = new ArraySegment<byte>(tempArray, 0, tempArray.Length);
            await WriteAsyncCore(tempSegment, cancellationToken);
        }
    }

    /// <summary>
    /// Core async write implementation that works with array segments to avoid span limitations
    /// </summary>
    private async ValueTask WriteAsyncCore(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken
    )
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var maxCharCount = _decoder.GetCharCount(
            buffer.Array!,
            buffer.Offset,
            buffer.Count,
            flush: false
        );
        if (maxCharCount == 0)
        {
            return;
        }

        var rentedCharBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(maxCharCount);

        try
        {
            var charCount = _decoder.GetChars(
                buffer.Array!,
                buffer.Offset,
                buffer.Count,
                rentedCharBuffer,
                0,
                flush: false
            );
            if (charCount > 0)
            {
                await _writer.WriteAsync(
                    rentedCharBuffer.AsMemory(0, charCount),
                    cancellationToken
                );
            }
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(rentedCharBuffer);
        }
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        FlushDecoder();
        _writer.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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

        if (finalCharCount <= SmallBufferThreshold)
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
                var actualCharCount = _decoder.GetChars(
                    emptyBytes,
                    0,
                    0,
                    rentedBuffer,
                    0,
                    flush: true
                );
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
        var emptyBytes = Array.Empty<byte>();
        var finalCharCount = _decoder.GetCharCount(emptyBytes, 0, 0, flush: true);
        if (finalCharCount == 0)
        {
            return;
        }

        var rentedBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(finalCharCount);

        try
        {
            var actualCharCount = _decoder.GetChars(emptyBytes, 0, 0, rentedBuffer, 0, flush: true);
            if (actualCharCount > 0)
            {
                await _writer.WriteAsync(
                    rentedBuffer.AsMemory(0, actualCharCount),
                    cancellationToken
                );
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
        if (!_disposed && disposing)
        {
            FlushDecoder();
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
