using System.Collections;

namespace DbfSharp.Core;

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
    public struct Enumerator
    {
        private readonly DbfReader _reader;
        private readonly bool _skipDeleted;
        private readonly byte[] _recordBuffer;
        private uint _currentRecordIndex;

        internal Enumerator(DbfReader reader, bool skipDeleted)
        {
            _reader = reader;
            _skipDeleted = skipDeleted;
            _recordBuffer = new byte[reader.Header.RecordLength];
            _currentRecordIndex = 0;

            // Position stream at start of record data
            if (_reader.Stream.CanSeek)
            {
                _reader.Stream.Position = reader.Header.HeaderLength;
            }
        }

        /// <summary>
        /// Gets the current record
        /// </summary>
        public SpanDbfRecord Current => new(_reader, _recordBuffer.AsSpan());

        /// <summary>
        /// Advances to the next record
        /// </summary>
        /// <returns>True if a record was found, false if end of file</returns>
        public bool MoveNext()
        {
            while (_currentRecordIndex < _reader.Header.NumberOfRecords)
            {
                var bytesRead = _reader.Stream.Read(_recordBuffer, 0, _recordBuffer.Length);
                if (bytesRead != _recordBuffer.Length)
                {
                    return false; // End of file or incomplete record
                }

                if (_recordBuffer[0] == 0x1A) // EOF marker
                {
                    return false;
                }

                _currentRecordIndex++;

                // Skip deleted records if requested
                if (_skipDeleted && _recordBuffer[0] == '*')
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the enumerator (not supported for stream-based enumeration)
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException("Reset is not supported for stream-based enumeration");
        }

        /// <summary>
        /// Disposes the enumerator (no-op for struct)
        /// </summary>
        public void Dispose()
        {
            // No resources to dispose for struct enumerator
        }
    }
}

