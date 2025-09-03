using System.Buffers.Binary;

namespace DbfSharp.Core;

/// <summary>
/// Represents the index file (.shx) for a shapefile, providing efficient random access to records
/// </summary>
public sealed class ShapefileIndex : IDisposable
{
    private readonly RecordInfo[] _recordInfos;
    private bool _disposed;

    /// <summary>
    /// Represents information about a single record in the shapefile index
    /// </summary>
    public readonly struct RecordInfo
    {
        /// <summary>
        /// The byte offset of the record in the .shp file (in 16-bit words from the beginning of the file)
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The length of the record content in 16-bit words (not including the 8-byte record header)
        /// </summary>
        public int ContentLength { get; }

        /// <summary>
        /// Initializes a new record info structure
        /// </summary>
        /// <param name="offset">The record offset in 16-bit words</param>
        /// <param name="contentLength">The record content length in 16-bit words</param>
        public RecordInfo(int offset, int contentLength)
        {
            Offset = offset;
            ContentLength = contentLength;
        }

        /// <summary>
        /// Gets the byte offset of the record in the .shp file
        /// </summary>
        public long ByteOffset => (long)Offset * 2;

        /// <summary>
        /// Gets the content length in bytes (not including the 8-byte record header)
        /// </summary>
        public int ContentLengthInBytes => ContentLength * 2;

        /// <summary>
        /// Gets the total record length in bytes (including the 8-byte record header)
        /// </summary>
        public int TotalRecordLengthInBytes => ContentLengthInBytes + 8;

        /// <summary>
        /// Gets a value indicating whether this record has content (non-zero length)
        /// </summary>
        public bool HasContent => ContentLength > 0;

        /// <summary>
        /// Returns a string representation of the RecordInfo
        /// </summary>
        /// <returns>A string that represents the current RecordInfo</returns>
        public override string ToString()
        {
            return $"Record at offset {ByteOffset:N0}, content length {ContentLengthInBytes:N0} bytes";
        }
    }

    /// <summary>
    /// Gets the header information from the index file
    /// </summary>
    public ShapefileHeader Header { get; }

    /// <summary>
    /// Gets the number of records in the shapefile
    /// </summary>
    public int RecordCount => _recordInfos.Length;

    /// <summary>
    /// Initializes a new shapefile index
    /// </summary>
    /// <param name="header">The shapefile header</param>
    /// <param name="recordInfos">The array of record information</param>
    private ShapefileIndex(ShapefileHeader header, RecordInfo[] recordInfos)
    {
        Header = header;
        _recordInfos = recordInfos;
    }

    /// <summary>
    /// Gets the record information for the specified record number
    /// </summary>
    /// <param name="recordNumber">The zero-based record number</param>
    /// <returns>The record information</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the record number is out of range</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the index has been disposed</exception>
    public RecordInfo GetRecordInfo(int recordNumber)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileIndex));

        if (recordNumber < 0 || recordNumber >= _recordInfos.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordNumber),
                $"Record number {recordNumber} is out of range [0, {_recordInfos.Length - 1}]"
            );
        }

        return _recordInfos[recordNumber];
    }

    /// <summary>
    /// Gets all record information as a read-only span
    /// </summary>
    /// <returns>A read-only span of all record information</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the index has been disposed</exception>
    public ReadOnlySpan<RecordInfo> GetAllRecordInfo()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileIndex));
        return _recordInfos.AsSpan();
    }

    /// <summary>
    /// Finds records within the specified byte range in the .shp file
    /// </summary>
    /// <param name="startOffset">The start byte offset (inclusive)</param>
    /// <param name="endOffset">The end byte offset (exclusive)</param>
    /// <returns>The record numbers that fall within the specified range</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the index has been disposed</exception>
    public IEnumerable<int> FindRecordsInRange(long startOffset, long endOffset)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileIndex));

        for (int i = 0; i < _recordInfos.Length; i++)
        {
            var recordInfo = _recordInfos[i];
            var recordStart = recordInfo.ByteOffset;
            var recordEnd = recordStart + recordInfo.TotalRecordLengthInBytes;

            // Check if record overlaps with the specified range
            if (recordStart < endOffset && recordEnd > startOffset)
            {
                yield return i;
            }
        }
    }

    /// <summary>
    /// Gets statistics about the record sizes in this index
    /// </summary>
    /// <returns>Statistics about record sizes</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the index has been disposed</exception>
    public RecordSizeStatistics GetRecordSizeStatistics()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileIndex));

        if (_recordInfos.Length == 0)
        {
            return new RecordSizeStatistics(0, 0, 0, 0, 0);
        }

        var sizes = _recordInfos.Select(r => r.ContentLengthInBytes).ToArray();
        Array.Sort(sizes);

        var min = sizes[0];
        var max = sizes[^1];
        var average = sizes.Average();
        var median =
            sizes.Length % 2 == 0
                ? (sizes[sizes.Length / 2 - 1] + sizes[sizes.Length / 2]) / 2.0
                : sizes[sizes.Length / 2];
        var total = sizes.Sum();

        return new RecordSizeStatistics(min, max, average, median, total);
    }

    /// <summary>
    /// Record size statistics
    /// </summary>
    public readonly struct RecordSizeStatistics
    {
        /// <summary>
        /// Gets the minimum record size in bytes
        /// </summary>
        public int MinSize { get; }

        /// <summary>
        /// Gets the maximum record size in bytes
        /// </summary>
        public int MaxSize { get; }

        /// <summary>
        /// Gets the average record size in bytes
        /// </summary>
        public double AverageSize { get; }

        /// <summary>
        /// Gets the median record size in bytes
        /// </summary>
        public double MedianSize { get; }

        /// <summary>
        /// Gets the total size of all records in bytes
        /// </summary>
        public long TotalSize { get; }

        /// <summary>
        /// Initializes a new RecordSizeStatistics structure
        /// </summary>
        /// <param name="minSize">The minimum record size in bytes</param>
        /// <param name="maxSize">The maximum record size in bytes</param>
        /// <param name="averageSize">The average record size in bytes</param>
        /// <param name="medianSize">The median record size in bytes</param>
        /// <param name="totalSize">The total size of all records in bytes</param>
        public RecordSizeStatistics(
            int minSize,
            int maxSize,
            double averageSize,
            double medianSize,
            long totalSize
        )
        {
            MinSize = minSize;
            MaxSize = maxSize;
            AverageSize = averageSize;
            MedianSize = medianSize;
            TotalSize = totalSize;
        }

        /// <summary>
        /// Returns a string representation of the RecordSizeStatistics
        /// </summary>
        /// <returns>A string that represents the current RecordSizeStatistics</returns>
        public override string ToString()
        {
            return $"Record sizes: Min={MinSize:N0}, Max={MaxSize:N0}, Avg={AverageSize:F1}, Median={MedianSize:F1}, Total={TotalSize:N0} bytes";
        }
    }

    /// <summary>
    /// Reads a shapefile index from the specified stream
    /// </summary>
    /// <param name="stream">The stream containing the .shx file data</param>
    /// <returns>The parsed shapefile index</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    /// <exception cref="ArgumentException">Thrown when stream is not readable</exception>
    /// <exception cref="FormatException">Thrown when the index format is invalid</exception>
    public static ShapefileIndex Read(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }

        // Read the header (same format as .shp file)
        var header = ShapefileHeader.Read(stream);

        // Calculate the number of records based on file size
        // Header is 100 bytes, each record index entry is 8 bytes
        var indexDataLength = header.FileLengthInBytes - ShapefileHeader.Size;
        if (indexDataLength < 0)
        {
            throw new FormatException(
                "Invalid index file: file length is smaller than header size"
            );
        }

        var recordCount = (int)(indexDataLength / 8);
        if (indexDataLength % 8 != 0)
        {
            throw new FormatException(
                $"Invalid index file: remaining data length {indexDataLength} is not divisible by 8"
            );
        }

        // Read all record index entries
        var recordInfos = new RecordInfo[recordCount];
        var buffer = new byte[8];

        for (int i = 0; i < recordCount; i++)
        {
            var bytesRead = stream.Read(buffer, 0, 8);
            if (bytesRead != 8)
            {
                throw new FormatException(
                    $"Unexpected end of stream reading record {i}: expected 8 bytes, got {bytesRead}"
                );
            }

            // Record index entry format:
            // Bytes 0-3: Record offset in 16-bit words (big-endian)
            // Bytes 4-7: Record content length in 16-bit words (big-endian)
            var offset = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0, 4));
            var contentLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(4, 4));

            // Validate record info
            if (offset < 0)
            {
                throw new FormatException(
                    $"Record {i}: Invalid offset {offset} (must be non-negative)"
                );
            }

            if (contentLength < 0)
            {
                throw new FormatException(
                    $"Record {i}: Invalid content length {contentLength} (must be non-negative)"
                );
            }

            recordInfos[i] = new RecordInfo(offset, contentLength);
        }

        return new ShapefileIndex(header, recordInfos);
    }

    /// <summary>
    /// Reads a shapefile index from the specified binary reader
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the .shx file</param>
    /// <returns>The parsed shapefile index</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null</exception>
    /// <exception cref="FormatException">Thrown when the index format is invalid</exception>
    public static ShapefileIndex Read(BinaryReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        // Read the header (same format as .shp file)
        var header = ShapefileHeader.Read(reader);

        // Calculate the number of records based on file size
        var indexDataLength = header.FileLengthInBytes - ShapefileHeader.Size;
        if (indexDataLength < 0)
        {
            throw new FormatException(
                "Invalid index file: file length is smaller than header size"
            );
        }

        var recordCount = (int)(indexDataLength / 8);
        if (indexDataLength % 8 != 0)
        {
            throw new FormatException(
                $"Invalid index file: remaining data length {indexDataLength} is not divisible by 8"
            );
        }

        // Read all record index entries
        var recordInfos = new RecordInfo[recordCount];

        for (int i = 0; i < recordCount; i++)
        {
            try
            {
                // Record index entry format:
                // Bytes 0-3: Record offset in 16-bit words (big-endian)
                // Bytes 4-7: Record content length in 16-bit words (big-endian)
                var offsetBytes = reader.ReadBytes(4);
                var lengthBytes = reader.ReadBytes(4);

                if (offsetBytes.Length != 4 || lengthBytes.Length != 4)
                {
                    throw new FormatException($"Unexpected end of stream reading record {i}");
                }

                var offset = BinaryPrimitives.ReadInt32BigEndian(offsetBytes);
                var contentLength = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);

                // Validate record info
                if (offset < 0)
                {
                    throw new FormatException(
                        $"Record {i}: Invalid offset {offset} (must be non-negative)"
                    );
                }

                if (contentLength < 0)
                {
                    throw new FormatException(
                        $"Record {i}: Invalid content length {contentLength} (must be non-negative)"
                    );
                }

                recordInfos[i] = new RecordInfo(offset, contentLength);
            }
            catch (EndOfStreamException)
            {
                throw new FormatException($"Unexpected end of stream reading record {i}");
            }
        }

        return new ShapefileIndex(header, recordInfos);
    }

    /// <summary>
    /// Reads a shapefile index from the specified file path
    /// </summary>
    /// <param name="indexFilePath">The path to the .shx file</param>
    /// <returns>The parsed shapefile index</returns>
    /// <exception cref="ArgumentNullException">Thrown when indexFilePath is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="FormatException">Thrown when the index format is invalid</exception>
    public static ShapefileIndex ReadFromFile(string indexFilePath)
    {
        if (string.IsNullOrEmpty(indexFilePath))
        {
            throw new ArgumentNullException(nameof(indexFilePath));
        }

        if (!File.Exists(indexFilePath))
        {
            throw new FileNotFoundException($"Index file not found: {indexFilePath}");
        }

        using var fileStream = new FileStream(
            indexFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
        return Read(fileStream);
    }

    /// <summary>
    /// Validates the consistency of this index against a shapefile header
    /// </summary>
    /// <param name="shapefileHeader">The header from the corresponding .shp file</param>
    /// <returns>A collection of validation errors, if any</returns>
    public IEnumerable<string> ValidateAgainstShapefileHeader(ShapefileHeader shapefileHeader)
    {
        if (shapefileHeader == null)
        {
            yield return "Shapefile header is null";
            yield break;
        }

        // Check that the headers match
        if (Header.FileCode != shapefileHeader.FileCode)
        {
            yield return $"File code mismatch: index={Header.FileCode}, shapefile={shapefileHeader.FileCode}";
        }

        if (Header.Version != shapefileHeader.Version)
        {
            yield return $"Version mismatch: index={Header.Version}, shapefile={shapefileHeader.Version}";
        }

        if (Header.ShapeType != shapefileHeader.ShapeType)
        {
            yield return $"Shape type mismatch: index={Header.ShapeType}, shapefile={shapefileHeader.ShapeType}";
        }

        // Check bounding box consistency (with some tolerance for floating point precision)
        const double tolerance = 1e-10;

        if (Math.Abs(Header.XMin - shapefileHeader.XMin) > tolerance)
        {
            yield return $"XMin mismatch: index={Header.XMin}, shapefile={shapefileHeader.XMin}";
        }

        if (Math.Abs(Header.YMin - shapefileHeader.YMin) > tolerance)
        {
            yield return $"YMin mismatch: index={Header.YMin}, shapefile={shapefileHeader.YMin}";
        }

        if (Math.Abs(Header.XMax - shapefileHeader.XMax) > tolerance)
        {
            yield return $"XMax mismatch: index={Header.XMax}, shapefile={shapefileHeader.XMax}";
        }

        if (Math.Abs(Header.YMax - shapefileHeader.YMax) > tolerance)
        {
            yield return $"YMax mismatch: index={Header.YMax}, shapefile={shapefileHeader.YMax}";
        }

        // Check record offsets don't exceed file bounds
        foreach (var (recordInfo, recordNumber) in _recordInfos.Select((r, i) => (r, i)))
        {
            var recordEnd = recordInfo.ByteOffset + recordInfo.TotalRecordLengthInBytes;
            if (recordEnd > shapefileHeader.FileLengthInBytes)
            {
                yield return $"Record {recordNumber}: end offset {recordEnd} exceeds shapefile length {shapefileHeader.FileLengthInBytes}";
            }
        }
    }

    /// <summary>
    /// Gets validation errors for this index, if any
    /// </summary>
    /// <returns>A collection of validation error messages</returns>
    public IEnumerable<string> GetValidationErrors()
    {
        foreach (var error in Header.GetValidationErrors())
        {
            yield return $"Header: {error}";
        }

        // Check for overlapping records
        var sortedRecords = _recordInfos
            .Select((info, index) => new { Info = info, Index = index })
            .OrderBy(r => r.Info.ByteOffset)
            .ToArray();

        for (int i = 1; i < sortedRecords.Length; i++)
        {
            var prev = sortedRecords[i - 1];
            var current = sortedRecords[i];

            var prevEnd = prev.Info.ByteOffset + prev.Info.TotalRecordLengthInBytes;
            if (prevEnd > current.Info.ByteOffset)
            {
                yield return $"Records {prev.Index} and {current.Index} overlap: "
                    + $"record {prev.Index} ends at {prevEnd}, record {current.Index} starts at {current.Info.ByteOffset}";
            }
        }

        // Check for invalid offsets or lengths
        for (int i = 0; i < _recordInfos.Length; i++)
        {
            var info = _recordInfos[i];

            if (info.Offset < 0)
            {
                yield return $"Record {i}: Invalid offset {info.Offset} (must be non-negative)";
            }

            if (info.ContentLength < 0)
            {
                yield return $"Record {i}: Invalid content length {info.ContentLength} (must be non-negative)";
            }

            if (info.ByteOffset < ShapefileHeader.Size)
            {
                yield return $"Record {i}: Offset {info.ByteOffset} overlaps with header (must be >= {ShapefileHeader.Size})";
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the ShapefileIndex
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    /// <summary>
    /// Returns a string representation of the ShapefileIndex
    /// </summary>
    /// <returns>A string that represents the current ShapefileIndex</returns>
    public override string ToString()
    {
        var sizeStats = RecordCount > 0 ? GetRecordSizeStatistics() : (RecordSizeStatistics?)null;
        var statsInfo = sizeStats.HasValue ? $", {sizeStats}" : "";

        return $"Shapefile Index: {RecordCount:N0} records{statsInfo}";
    }
}
