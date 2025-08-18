using System.Buffers;

namespace DbfSharp.Core.Utils;

/// <summary>
/// Provides enumeration over DBF records using zero-allocation span-based access.
/// This struct-based enumerator avoids object allocations for the enumeration itself.
/// </summary>
public readonly struct SpanRecordEnumerable
{
    private readonly DbfReader _reader;
    private readonly bool _skipDeleted;

    internal SpanRecordEnumerable(DbfReader reader, bool skipDeleted)
    {
        _reader = reader;
        _skipDeleted = skipDeleted;
    }

    /// <summary>
    /// Gets the enumerator for this enumerable
    /// </summary>
    /// <returns>A struct-based enumerator</returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(_reader, _skipDeleted);
    }

    /// <summary>
    /// Struct-based enumerator that provides zero-allocation enumeration of span-based records
    /// </summary>
    public struct Enumerator : IDisposable
    {
        private readonly DbfReader _reader;
        private readonly bool _skipDeleted;
        private readonly byte[] _recordBuffer;
        private readonly bool _rentedBuffer;
        private uint _currentRecordIndex;

        internal Enumerator(DbfReader reader, bool skipDeleted)
        {
            _reader = reader;
            _skipDeleted = skipDeleted;

            // use ArrayPool for large record buffers to reduce allocations
            var recordLength = reader.Header.RecordLength;
            if (recordLength > 1024)
            {
                _recordBuffer = ArrayPool<byte>.Shared.Rent(recordLength);
                _rentedBuffer = true;
            }
            else
            {
                _recordBuffer = new byte[recordLength];
                _rentedBuffer = false;
            }

            _currentRecordIndex = 0;

            // position stream at start of record data (only for non-memory-mapped)
            if (_reader is { UseMemoryMapping: false, Stream.CanSeek: true })
            {
                _reader.Stream.Position = reader.Header.HeaderLength;
            }
        }

        /// <summary>
        /// Gets the current record
        /// </summary>
        public SpanDbfRecord Current =>
            new(_reader, _recordBuffer.AsSpan(0, _reader.Header.RecordLength));

        /// <summary>
        /// Advances to the next record
        /// </summary>
        /// <returns>True if a record was found, false if end of file</returns>
        public bool MoveNext()
        {
            var recordLength = _reader.Header.RecordLength;

            while (_currentRecordIndex < _reader.Header.NumberOfRecords)
            {
                bool success;

                if (_reader.UseMemoryMapping)
                {
                    var accessor = _reader.ChunkedMemoryMappedAccessor;
                    if (accessor == null)
                    {
                        return false;
                    }

                    var recordPosition =
                        _reader.Header.HeaderLength + _currentRecordIndex * recordLength;
                    if (recordPosition + recordLength > accessor.Capacity)
                    {
                        return false;
                    }

                    accessor.ReadArray(recordPosition, _recordBuffer, 0, recordLength);
                    success = true;
                }
                else
                {
                    var bytesRead = _reader.Stream.Read(_recordBuffer, 0, recordLength);
                    success = bytesRead == recordLength;
                }

                if (!success)
                {
                    return false; // end of file or incomplete record
                }

                if (_recordBuffer[0] == 0x1A) // EOF marker
                {
                    return false;
                }

                _currentRecordIndex++;

                // skip deleted records if requested
                if (_skipDeleted && _recordBuffer[0] == '*')
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Disposes the enumerator and returns rented buffer to ArrayPool if applicable
        /// </summary>
        public void Dispose()
        {
            if (_rentedBuffer && _recordBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_recordBuffer);
            }
        }
    }
}
