using System.Buffers.Binary;
using DbfSharp.Core.Geometry;
using DbfSharp.Core.Parsing;
using DbfSharp.Core.Utils;

namespace DbfSharp.Core;

/// <summary>
/// Provides high-performance reading capabilities for ESRI Shapefiles with integrated DBF attribute support
/// </summary>
/// <remarks>
/// <para>
/// The ShapefileReader handles the complete shapefile dataset including .shp (geometry), .shx (index),
/// and .dbf (attributes) files. It provides both streaming enumeration and random access patterns
/// for efficient processing of geographic data.
/// </para>
/// <para>
/// **Thread Safety**: This class is **not thread-safe**. Each thread should use its own ShapefileReader instance.
/// Do not share a single ShapefileReader instance across multiple threads without external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Read a complete shapefile with attributes
/// using var reader = ShapefileReader.Create("data.shp");
/// foreach (var feature in reader.Features)
/// {
///     Console.WriteLine($"Geometry: {feature.Geometry}");
///     if (feature.HasAttributes)
///     {
///         Console.WriteLine($"Name: {feature.GetAttributeAsString("NAME")}");
///     }
/// }
///
/// // Read geometry only (when .dbf is not available)
/// using var reader = ShapefileReader.Create(shpStream, shxStream);
/// foreach (var record in reader.Records)
/// {
///     Console.WriteLine($"Record {record.RecordNumber}: {record.Geometry}");
/// }
/// </code>
/// </example>
public sealed partial class ShapefileReader : IDisposable
{
    private readonly Stream _shpStream;
    private readonly Stream? _shxStream;
    private readonly DbfReader? _dbfReader;
    private readonly ShapefileIndex? _index;
    private readonly bool _ownsStreams;
    private bool _disposed;

    /// <summary>
    /// Gets the shapefile metadata including projection and encoding information
    /// </summary>
    public ShapefileMetadata? Metadata { get; }

    /// <summary>
    /// Gets the shapefile header containing metadata and extent information
    /// </summary>
    public ShapefileHeader Header { get; }

    /// <summary>
    /// Gets the total number of records in the shapefile
    /// </summary>
    public int RecordCount => _index?.RecordCount ?? 0;

    /// <summary>
    /// Gets a value indicating whether this reader has an index (.shx file) for random access
    /// </summary>
    public bool HasIndex => _index != null;

    /// <summary>
    /// Gets a value indicating whether this reader has attribute data (.dbf file)
    /// </summary>
    public bool HasAttributes => _dbfReader != null;

    /// <summary>
    /// Gets the DBF reader for attribute data, or null if not available
    /// </summary>
    public DbfReader? DbfReader => _dbfReader;

    /// <summary>
    /// Gets the shapefile index, or null if not available
    /// </summary>
    public ShapefileIndex? Index => _index;

    /// <summary>
    /// Gets the primary shape type for this shapefile
    /// </summary>
    public ShapeType ShapeType => Header.ShapeType;

    /// <summary>
    /// Gets the bounding box encompassing all shapes in this file
    /// </summary>
    public BoundingBox BoundingBox => Header.BoundingBox;

    /// <summary>
    /// Gets the file name or source description
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Initializes a new ShapefileReader instance
    /// </summary>
    private ShapefileReader(
        Stream shpStream,
        Stream? shxStream,
        DbfReader? dbfReader,
        ShapefileHeader header,
        ShapefileIndex? index,
        bool ownsStreams,
        string source,
        ShapefileMetadata? metadata = null
    )
    {
        _shpStream = shpStream;
        _shxStream = shxStream;
        _dbfReader = dbfReader;
        Header = header;
        _index = index;
        _ownsStreams = ownsStreams;
        Source = source;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a shapefile reader from a .shp file path, automatically detecting related files
    /// </summary>
    /// <param name="shpFilePath">Path to the .shp file</param>
    /// <returns>A new ShapefileReader instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when shpFilePath is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the .shp file is not found</exception>
    /// <exception cref="FormatException">Thrown when the shapefile format is invalid</exception>
    public static ShapefileReader Create(string shpFilePath)
    {
        if (string.IsNullOrEmpty(shpFilePath))
        {
            throw new ArgumentNullException(nameof(shpFilePath));
        }

        if (!File.Exists(shpFilePath))
        {
            throw new FileNotFoundException($"Shapefile not found: {shpFilePath}");
        }

        // Use ShapefileDetector to find all related files including .prj and .cpg
        var components = ShapefileDetector.DetectAndValidateComponents(shpFilePath);
        var metadata = ShapefileDetector.GetMetadata(components);

        // Open streams
        var shpStream = new FileStream(shpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Stream? shxStream = null;
        DbfReader? dbfReader = null;

        try
        {
            // Try to open index file
            if (!string.IsNullOrEmpty(components.ShxPath))
            {
                shxStream = new FileStream(
                    components.ShxPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );
            }

            // Try to open attribute file with encoding from .cpg file
            if (!string.IsNullOrEmpty(components.DbfPath))
            {
                var dbfOptions = new DbfReaderOptions();

                // Use encoding from .cpg file if available and valid
                if (metadata.HasValidEncoding)
                {
                    dbfOptions = dbfOptions with { Encoding = metadata.Encoding };
                }

                dbfReader = DbfReader.Create(components.DbfPath, dbfOptions);
            }

            return Create(
                shpStream,
                shxStream,
                dbfReader,
                ownsStreams: true,
                Path.GetFileName(shpFilePath),
                metadata
            );
        }
        catch
        {
            shpStream?.Dispose();
            shxStream?.Dispose();
            dbfReader?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a shapefile reader from streams
    /// </summary>
    /// <param name="shpStream">The .shp file stream</param>
    /// <param name="shxStream">The .shx file stream (optional)</param>
    /// <param name="dbfReader">The DBF reader for attributes (optional)</param>
    /// <param name="ownsStreams">Whether the reader should dispose the streams when disposed</param>
    /// <param name="source">A description of the source (for display purposes)</param>
    /// <param name="metadata">Optional shapefile metadata including projection and encoding information</param>
    /// <returns>A new ShapefileReader instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when shpStream is null</exception>
    /// <exception cref="ArgumentException">Thrown when shpStream is not readable</exception>
    /// <exception cref="FormatException">Thrown when the shapefile format is invalid</exception>
    public static ShapefileReader Create(
        Stream shpStream,
        Stream? shxStream = null,
        DbfReader? dbfReader = null,
        bool ownsStreams = false,
        string source = "stream",
        ShapefileMetadata? metadata = null
    )
    {
        if (shpStream == null)
        {
            throw new ArgumentNullException(nameof(shpStream));
        }

        if (!shpStream.CanRead)
        {
            throw new ArgumentException("Shapefile stream must be readable", nameof(shpStream));
        }

        if (shxStream is { CanRead: false })
        {
            throw new ArgumentException("Index stream must be readable", nameof(shxStream));
        }

        try
        {
            // Read shapefile header
            var header = ShapefileHeader.Read(shpStream);

            // Read index if available
            ShapefileIndex? index = null;
            if (shxStream != null)
            {
                index = ShapefileIndex.Read(shxStream);

                // Validate index against shapefile header
                var validationErrors = index.ValidateAgainstShapefileHeader(header).ToList();
                if (validationErrors.Count > 0)
                {
                    // Log warnings but continue (some shapefiles have minor inconsistencies)
                    // In a real implementation, you might want to use a logger here
                }
            }

            // Validate DBF record count if available
            if (dbfReader != null && index != null)
            {
                if (dbfReader.RecordCount != index.RecordCount)
                {
                    // Log warning but continue - some shapefiles have mismatched record counts
                    // This is common when records are deleted but not removed from all files
                }
            }

            return new ShapefileReader(
                shpStream,
                shxStream,
                dbfReader,
                header,
                index,
                ownsStreams,
                source,
                metadata
            );
        }
        catch
        {
            if (ownsStreams)
            {
                shpStream?.Dispose();
                shxStream?.Dispose();
                dbfReader?.Dispose();
            }
            throw;
        }
    }

    /// <summary>
    /// Gets an enumerable collection of shapefile records (geometry only)
    /// </summary>
    /// <remarks>
    /// This property provides streaming access to geometry records. If an index is available,
    /// records can be accessed in any order. Without an index, records are read sequentially.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public IEnumerable<ShapefileRecord> Records
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));

            if (HasIndex)
            {
                return EnumerateRecordsWithIndex();
            }
            else
            {
                return EnumerateRecordsSequentially();
            }
        }
    }

    /// <summary>
    /// Gets an enumerable collection of shapefile features (geometry + attributes)
    /// </summary>
    /// <remarks>
    /// This property combines geometry records with corresponding attribute records.
    /// If no DBF reader is available, features will have null attributes.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public IEnumerable<ShapefileFeature> Features
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));

            return EnumerateFeatures();
        }
    }

    /// <summary>
    /// Gets a specific record by record number (1-based)
    /// </summary>
    /// <param name="recordNumber">The record number (1-based)</param>
    /// <returns>The shapefile record</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when record number is out of range</exception>
    /// <exception cref="InvalidOperationException">Thrown when no index is available for random access</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public ShapefileRecord GetRecord(int recordNumber)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));

        if (!HasIndex)
        {
            throw new InvalidOperationException("Random access requires an index file (.shx)");
        }

        if (recordNumber <= 0 || recordNumber > RecordCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordNumber),
                $"Record number {recordNumber} is out of range [1, {RecordCount}]"
            );
        }

        var recordIndex = recordNumber - 1;
        var recordInfo = _index!.GetRecordInfo(recordIndex);

        return ReadRecordAtOffset(recordInfo.ByteOffset, recordNumber);
    }

    /// <summary>
    /// Gets a specific feature by record number (1-based)
    /// </summary>
    /// <param name="recordNumber">The record number (1-based)</param>
    /// <returns>The shapefile feature</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when record number is out of range</exception>
    /// <exception cref="InvalidOperationException">Thrown when no index is available for random access</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public ShapefileFeature GetFeature(int recordNumber)
    {
        var record = GetRecord(recordNumber);

        DbfRecord? attributes = null;
        if (HasAttributes && recordNumber <= _dbfReader!.RecordCount)
        {
            try
            {
                attributes = _dbfReader.Records.ElementAtOrDefault(recordNumber - 1);
            }
            catch
            {
                // Ignore attribute read errors and continue with geometry only
            }
        }

        return new ShapefileFeature(record, attributes);
    }

    /// <summary>
    /// Enumerates records using the index for efficient access
    /// </summary>
    private IEnumerable<ShapefileRecord> EnumerateRecordsWithIndex()
    {
        // Convert ReadOnlySpan to array to avoid yield boundary issues
        var recordInfoArray = _index!.GetAllRecordInfo().ToArray();

        for (var i = 0; i < recordInfoArray.Length; i++)
        {
            var recordInfo = recordInfoArray[i];
            var recordNumber = i + 1;

            if (recordInfo.HasContent)
            {
                yield return ReadRecordAtOffset(recordInfo.ByteOffset, recordNumber);
            }
            else
            {
                // Empty record - yield null shape
                yield return new ShapefileRecord(
                    recordNumber,
                    NullShape.Instance,
                    recordInfo.ByteOffset,
                    recordInfo.TotalRecordLengthInBytes
                );
            }
        }
    }

    /// <summary>
    /// Enumerates records sequentially without using an index
    /// </summary>
    private IEnumerable<ShapefileRecord> EnumerateRecordsSequentially()
    {
        _shpStream.Position = ShapefileHeader.Size; // Skip header
        var recordNumber = 1;

        using var reader = new BinaryReader(
            _shpStream,
            System.Text.Encoding.ASCII,
            leaveOpen: true
        );

        while (_shpStream.Position < _shpStream.Length)
        {
            var recordStartPosition = _shpStream.Position;

            // Read record header (8 bytes)
            if (_shpStream.Position + 8 > _shpStream.Length)
            {
                break;
            }

            var headerBytes = reader.ReadBytes(8);
            if (headerBytes.Length != 8)
            {
                break;
            }

            // Parse record header
            var recordNumberFromFile = BinaryPrimitives.ReadInt32BigEndian(
                headerBytes.AsSpan(0, 4)
            );
            var contentLength = BinaryPrimitives.ReadInt32BigEndian(headerBytes.AsSpan(4, 4));

            if (contentLength < 0 || _shpStream.Position + contentLength * 2 > _shpStream.Length)
            {
                break;
            }

            // Read shape data
            var shapeDataBytes = reader.ReadBytes(contentLength * 2);
            if (shapeDataBytes.Length != contentLength * 2)
            {
                break;
            }

            // Parse geometry - handle exceptions by skipping malformed records
            Shape geometry;
            try
            {
                geometry =
                    contentLength > 0 ? ShapeParser.ParseShape(shapeDataBytes) : NullShape.Instance;
            }
            catch (FormatException)
            {
                // Skip malformed record but continue processing
                recordNumber++;
                continue;
            }

            var totalRecordLength = 8 + contentLength * 2;
            yield return new ShapefileRecord(
                recordNumber,
                geometry,
                recordStartPosition,
                totalRecordLength
            );

            recordNumber++;
        }
    }

    /// <summary>
    /// Enumerates features by combining geometry and attribute data
    /// </summary>
    private IEnumerable<ShapefileFeature> EnumerateFeatures()
    {
        var geometryRecords = Records.ToList(); // Cache geometry records

        if (!HasAttributes)
        {
            // No attributes - return features with geometry only
            foreach (var record in geometryRecords)
            {
                yield return new ShapefileFeature(record, null);
            }
        }
        else
        {
            // Combine geometry with attributes
            var attributeRecords = _dbfReader!.Records.ToList();

            for (var i = 0; i < geometryRecords.Count; i++)
            {
                var geometryRecord = geometryRecords[i];
                var attributes =
                    i < attributeRecords.Count ? attributeRecords[i] : (DbfRecord?)null;

                yield return new ShapefileFeature(geometryRecord, attributes);
            }
        }
    }

    /// <summary>
    /// Reads a record at the specified byte offset
    /// </summary>
    private ShapefileRecord ReadRecordAtOffset(long byteOffset, int recordNumber)
    {
        _shpStream.Position = byteOffset;

        using var reader = new BinaryReader(
            _shpStream,
            System.Text.Encoding.ASCII,
            leaveOpen: true
        );

        // Read record header (8 bytes)
        var headerBytes = reader.ReadBytes(8);
        if (headerBytes.Length != 8)
        {
            throw new FormatException($"Incomplete record header at offset {byteOffset}");
        }

        // Parse record header
        var recordNumberFromFile = BinaryPrimitives.ReadInt32BigEndian(headerBytes.AsSpan(0, 4));
        var contentLength = BinaryPrimitives.ReadInt32BigEndian(headerBytes.AsSpan(4, 4));

        if (contentLength < 0)
        {
            throw new FormatException(
                $"Invalid content length {contentLength} for record {recordNumber}"
            );
        }

        // Read shape data
        var shapeDataBytes = reader.ReadBytes(contentLength * 2);
        if (shapeDataBytes.Length != contentLength * 2)
        {
            throw new FormatException($"Incomplete shape data for record {recordNumber}");
        }

        // Parse geometry
        var geometry =
            contentLength > 0 ? ShapeParser.ParseShape(shapeDataBytes) : NullShape.Instance;

        var totalRecordLength = 8 + contentLength * 2;
        return new ShapefileRecord(recordNumber, geometry, byteOffset, totalRecordLength);
    }

    /// <summary>
    /// Gets validation errors for this shapefile, if any
    /// </summary>
    /// <returns>A collection of validation error messages</returns>
    public IEnumerable<string> GetValidationErrors()
    {
        foreach (var error in Header.GetValidationErrors())
        {
            yield return $"Header: {error}";
        }

        if (HasIndex)
        {
            foreach (var error in _index!.GetValidationErrors())
            {
                yield return $"Index: {error}";
            }

            foreach (var error in _index.ValidateAgainstShapefileHeader(Header))
            {
                yield return $"Index consistency: {error}";
            }
        }

        // Note: DBF validation is handled by the DbfReader itself
    }

    /// <summary>
    /// Releases all resources used by the ShapefileReader
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownsStreams)
        {
            _shpStream?.Dispose();
            _shxStream?.Dispose();
            _dbfReader?.Dispose();
        }

        _index?.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Returns a string representation of the ShapefileReader
    /// </summary>
    /// <returns>A string that represents the current ShapefileReader</returns>
    public override string ToString()
    {
        var indexInfo = HasIndex ? $", indexed" : ", sequential";
        var attributeInfo = HasAttributes
            ? $", {_dbfReader!.RecordCount} attribute records"
            : ", no attributes";

        return $"Shapefile '{Source}': {Header.ShapeType.GetDescription()}, "
            + $"{RecordCount} records{indexInfo}{attributeInfo}";
    }
}
