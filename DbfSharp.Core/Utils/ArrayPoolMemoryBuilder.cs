using System.Buffers;

namespace DbfSharp.Core.Utils;

/// <summary>
/// Memory-efficient builder using ArrayPool for dynamic byte array construction
/// </summary>
internal sealed class ArrayPoolMemoryBuilder<T>(int initialCapacity = 1024) : IDisposable
{
    private T[]? _buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
    private bool _disposed;

    /// <summary>
    /// Gets the current length of the data in the builder.
    /// </summary>
    public int Length { get; private set; }

    public void Append(ReadOnlySpan<T> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (data.IsEmpty)
        {
            return;
        }

        EnsureCapacity(Length + data.Length);
        data.CopyTo(_buffer.AsSpan(Length));
        Length += data.Length;
    }

    public ReadOnlyMemory<T> ToReadOnlyMemory()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Length == 0)
        {
            return ReadOnlyMemory<T>.Empty;
        }

        var result = new T[Length];
        _buffer.AsSpan(0, Length).CopyTo(result);
        return new ReadOnlyMemory<T>(result);
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_buffer!.Length >= requiredCapacity)
        {
            return;
        }

        var newCapacity = Math.Max(requiredCapacity, _buffer.Length * 2);
        var newBuffer = ArrayPool<T>.Shared.Rent(newCapacity);

        _buffer.AsSpan(0, Length).CopyTo(newBuffer);

        ArrayPool<T>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_buffer != null)
        {
            ArrayPool<T>.Shared.Return(_buffer);
            _buffer = null;
        }

        _disposed = true;
    }
}
