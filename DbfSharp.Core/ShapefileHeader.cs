using System.Buffers.Binary;
using DbfSharp.Core.Geometry;

namespace DbfSharp.Core;

/// <summary>
/// Represents the header of a shapefile (.shp file) containing metadata about the shapefile format and extent
/// </summary>
public sealed class ShapefileHeader
{
    /// <summary>
    /// The expected file code for valid shapefiles (9994 in big-endian)
    /// </summary>
    public const int ExpectedFileCode = 9994;

    /// <summary>
    /// The expected version number for shapefiles (1000)
    /// </summary>
    public const int ExpectedVersion = 1000;

    /// <summary>
    /// The size of the shapefile header in bytes
    /// </summary>
    public const int Size = 100;

    /// <summary>
    /// Gets the file code (should be 9994 for valid shapefiles)
    /// </summary>
    public int FileCode { get; }

    /// <summary>
    /// Gets the unused bytes (positions 4-23 in header, should be zero)
    /// </summary>
    public byte[] Unused { get; }

    /// <summary>
    /// Gets the file length in 16-bit words (including the header)
    /// </summary>
    public int FileLength { get; }

    /// <summary>
    /// Gets the shapefile version (should be 1000)
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the primary shape type for all shapes in this file
    /// </summary>
    public ShapeType ShapeType { get; }

    /// <summary>
    /// Gets the minimum X coordinate (bounding box)
    /// </summary>
    public double XMin { get; }

    /// <summary>
    /// Gets the minimum Y coordinate (bounding box)
    /// </summary>
    public double YMin { get; }

    /// <summary>
    /// Gets the maximum X coordinate (bounding box)
    /// </summary>
    public double XMax { get; }

    /// <summary>
    /// Gets the maximum Y coordinate (bounding box)
    /// </summary>
    public double YMax { get; }

    /// <summary>
    /// Gets the minimum Z coordinate (elevation), or null if not applicable
    /// </summary>
    public double? ZMin { get; }

    /// <summary>
    /// Gets the maximum Z coordinate (elevation), or null if not applicable
    /// </summary>
    public double? ZMax { get; }

    /// <summary>
    /// Gets the minimum M coordinate (measure), or null if not applicable
    /// </summary>
    public double? MMin { get; }

    /// <summary>
    /// Gets the maximum M coordinate (measure), or null if not applicable
    /// </summary>
    public double? MMax { get; }

    /// <summary>
    /// Initializes a new shapefile header
    /// </summary>
    /// <param name="fileCode">The file code (should be 9994)</param>
    /// <param name="unused">The unused bytes (positions 4-23)</param>
    /// <param name="fileLength">The file length in 16-bit words</param>
    /// <param name="version">The shapefile version (should be 1000)</param>
    /// <param name="shapeType">The primary shape type</param>
    /// <param name="xMin">The minimum X coordinate</param>
    /// <param name="yMin">The minimum Y coordinate</param>
    /// <param name="xMax">The maximum X coordinate</param>
    /// <param name="yMax">The maximum Y coordinate</param>
    /// <param name="zMin">The minimum Z coordinate (optional)</param>
    /// <param name="zMax">The maximum Z coordinate (optional)</param>
    /// <param name="mMin">The minimum M coordinate (optional)</param>
    /// <param name="mMax">The maximum M coordinate (optional)</param>
    public ShapefileHeader(
        int fileCode,
        byte[] unused,
        int fileLength,
        int version,
        ShapeType shapeType,
        double xMin,
        double yMin,
        double xMax,
        double yMax,
        double? zMin = null,
        double? zMax = null,
        double? mMin = null,
        double? mMax = null
    )
    {
        FileCode = fileCode;
        Unused = unused ?? throw new ArgumentNullException(nameof(unused));
        FileLength = fileLength;
        Version = version;
        ShapeType = shapeType;
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
        ZMin = zMin;
        ZMax = zMax;
        MMin = mMin;
        MMax = mMax;
    }

    /// <summary>
    /// Gets the file length in bytes (FileLength * 2)
    /// </summary>
    public long FileLengthInBytes => (long)FileLength * 2;

    /// <summary>
    /// Gets the bounding box for all shapes in this file
    /// </summary>
    public BoundingBox BoundingBox => new(XMin, YMin, XMax, YMax, ZMin, ZMax, MMin, MMax);

    /// <summary>
    /// Gets a value indicating whether this shapefile contains 3D shapes with Z coordinates
    /// </summary>
    public bool HasZ => ShapeType.HasZ();

    /// <summary>
    /// Gets a value indicating whether this shapefile contains measured shapes with M coordinates
    /// </summary>
    public bool HasM => ShapeType.HasM();

    /// <summary>
    /// Gets a value indicating whether the header appears to be valid
    /// </summary>
    public bool IsValid =>
        FileCode == ExpectedFileCode
        && Version == ExpectedVersion
        && Enum.IsDefined(typeof(ShapeType), ShapeType)
        && XMin <= XMax
        && YMin <= YMax
        && (!HasZ || (ZMin.HasValue && ZMax.HasValue && ZMin <= ZMax))
        && (!HasM || (MMin.HasValue && MMax.HasValue && MMin <= MMax));

    /// <summary>
    /// Reads a shapefile header from the specified stream
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the shapefile</param>
    /// <returns>The parsed shapefile header</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    /// <exception cref="ArgumentException">Thrown when stream is not readable or seekable</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream doesn't contain enough data</exception>
    /// <exception cref="FormatException">Thrown when the header format is invalid</exception>
    public static ShapefileHeader Read(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }

        var headerBytes = new byte[Size];
        var bytesRead = stream.Read(headerBytes, 0, Size);

        if (bytesRead != Size)
        {
            throw new EndOfStreamException(
                $"Expected {Size} bytes for shapefile header, got {bytesRead}"
            );
        }

        return Parse(headerBytes);
    }

    /// <summary>
    /// Reads a shapefile header from the specified binary reader
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the shapefile</param>
    /// <returns>The parsed shapefile header</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream doesn't contain enough data</exception>
    /// <exception cref="FormatException">Thrown when the header format is invalid</exception>
    public static ShapefileHeader Read(BinaryReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        var headerBytes = reader.ReadBytes(Size);
        if (headerBytes.Length != Size)
        {
            throw new EndOfStreamException(
                $"Expected {Size} bytes for shapefile header, got {headerBytes.Length}"
            );
        }

        return Parse(headerBytes);
    }

    /// <summary>
    /// Parses a shapefile header from the specified byte array
    /// </summary>
    /// <param name="headerBytes">The header bytes (must be exactly 100 bytes)</param>
    /// <returns>The parsed shapefile header</returns>
    /// <exception cref="ArgumentNullException">Thrown when headerBytes is null</exception>
    /// <exception cref="ArgumentException">Thrown when headerBytes is not exactly 100 bytes</exception>
    /// <exception cref="FormatException">Thrown when the header format is invalid</exception>
    public static ShapefileHeader Parse(byte[] headerBytes)
    {
        if (headerBytes == null)
        {
            throw new ArgumentNullException(nameof(headerBytes));
        }

        if (headerBytes.Length != Size)
        {
            throw new ArgumentException(
                $"Header must be exactly {Size} bytes, got {headerBytes.Length}",
                nameof(headerBytes)
            );
        }

        return Parse(headerBytes.AsSpan());
    }

    /// <summary>
    /// Parses a shapefile header from the specified byte span
    /// </summary>
    /// <param name="headerBytes">The header bytes (must be exactly 100 bytes)</param>
    /// <returns>The parsed shapefile header</returns>
    /// <exception cref="ArgumentException">Thrown when headerBytes is not exactly 100 bytes</exception>
    /// <exception cref="FormatException">Thrown when the header format is invalid</exception>
    public static ShapefileHeader Parse(ReadOnlySpan<byte> headerBytes)
    {
        if (headerBytes.Length != Size)
        {
            throw new ArgumentException(
                $"Header must be exactly {Size} bytes, got {headerBytes.Length}",
                nameof(headerBytes)
            );
        }

        // Parse header fields
        // Bytes 0-3: File Code (big-endian integer, should be 9994)
        var fileCode = BinaryPrimitives.ReadInt32BigEndian(headerBytes[0..4]);

        // Bytes 4-23: Unused (20 bytes, should be zero)
        var unused = headerBytes[4..24].ToArray();

        // Bytes 24-27: File Length in 16-bit words (big-endian integer)
        var fileLength = BinaryPrimitives.ReadInt32BigEndian(headerBytes[24..28]);

        // Bytes 28-31: Version (little-endian integer, should be 1000)
        var version = BinaryPrimitives.ReadInt32LittleEndian(headerBytes[28..32]);

        // Bytes 32-35: Shape Type (little-endian integer)
        var shapeTypeValue = BinaryPrimitives.ReadInt32LittleEndian(headerBytes[32..36]);

        // Validate basic header values
        if (fileCode != ExpectedFileCode)
        {
            throw new FormatException(
                $"Invalid shapefile file code: expected {ExpectedFileCode}, got {fileCode}"
            );
        }

        if (version != ExpectedVersion)
        {
            throw new FormatException(
                $"Unsupported shapefile version: expected {ExpectedVersion}, got {version}"
            );
        }

        if (!Enum.IsDefined(typeof(ShapeType), shapeTypeValue))
        {
            throw new FormatException($"Unknown shape type: {shapeTypeValue}");
        }

        var shapeType = (ShapeType)shapeTypeValue;

        // Bytes 36-67: Bounding Box (8 little-endian doubles: Xmin, Ymin, Xmax, Ymax)
        var xMin = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[36..44]);
        var yMin = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[44..52]);
        var xMax = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[52..60]);
        var yMax = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[60..68]);

        // Bytes 68-83: Z range (2 little-endian doubles: Zmin, Zmax) - optional
        double? zMin = null;
        double? zMax = null;
        if (shapeType.HasZ())
        {
            var zMinValue = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[68..76]);
            var zMaxValue = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[76..84]);

            // Check for valid Z values (shapefile spec uses 0.0 for unused values)
            if (
                !double.IsNaN(zMinValue)
                && !double.IsNaN(zMaxValue)
                && !(zMinValue == 0.0 && zMaxValue == 0.0)
            )
            {
                zMin = zMinValue;
                zMax = zMaxValue;
            }
        }

        // Bytes 84-99: M range (2 little-endian doubles: Mmin, Mmax) - optional
        double? mMin = null;
        double? mMax = null;
        if (shapeType.HasM())
        {
            var mMinValue = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[84..92]);
            var mMaxValue = BinaryPrimitives.ReadDoubleLittleEndian(headerBytes[92..100]);

            // Check for valid M values (shapefile spec uses 0.0 for unused values)
            if (
                !double.IsNaN(mMinValue)
                && !double.IsNaN(mMaxValue)
                && !(mMinValue == 0.0 && mMaxValue == 0.0)
            )
            {
                mMin = mMinValue;
                mMax = mMaxValue;
            }
        }

        // Validate bounding box
        if (xMin > xMax)
        {
            throw new FormatException($"Invalid bounding box: XMin ({xMin}) > XMax ({xMax})");
        }

        if (yMin > yMax)
        {
            throw new FormatException($"Invalid bounding box: YMin ({yMin}) > YMax ({yMax})");
        }

        if (zMin.HasValue && zMax.HasValue && zMin > zMax)
        {
            throw new FormatException($"Invalid Z range: ZMin ({zMin}) > ZMax ({zMax})");
        }

        if (mMin.HasValue && mMax.HasValue && mMin > mMax)
        {
            throw new FormatException($"Invalid M range: MMin ({mMin}) > MMax ({mMax})");
        }

        return new ShapefileHeader(
            fileCode,
            unused,
            fileLength,
            version,
            shapeType,
            xMin,
            yMin,
            xMax,
            yMax,
            zMin,
            zMax,
            mMin,
            mMax
        );
    }

    /// <summary>
    /// Gets validation errors for this header, if any
    /// </summary>
    /// <returns>A collection of validation error messages</returns>
    public IEnumerable<string> GetValidationErrors()
    {
        if (FileCode != ExpectedFileCode)
        {
            yield return $"Invalid file code: expected {ExpectedFileCode}, got {FileCode}";
        }

        if (Version != ExpectedVersion)
        {
            yield return $"Invalid version: expected {ExpectedVersion}, got {Version}";
        }

        if (!Enum.IsDefined(typeof(ShapeType), ShapeType))
        {
            yield return $"Unknown shape type: {(int)ShapeType}";
        }

        if (XMin > XMax)
        {
            yield return $"Invalid X range: min ({XMin}) > max ({XMax})";
        }

        if (YMin > YMax)
        {
            yield return $"Invalid Y range: min ({YMin}) > max ({YMax})";
        }

        if (HasZ && (!ZMin.HasValue || !ZMax.HasValue))
        {
            yield return "Shape type indicates Z coordinates but Z range is not specified";
        }

        if (ZMin.HasValue && ZMax.HasValue && ZMin > ZMax)
        {
            yield return $"Invalid Z range: min ({ZMin}) > max ({ZMax})";
        }

        if (HasM && (!MMin.HasValue || !MMax.HasValue))
        {
            yield return "Shape type indicates M coordinates but M range is not specified";
        }

        if (MMin.HasValue && MMax.HasValue && MMin > MMax)
        {
            yield return $"Invalid M range: min ({MMin}) > max ({MMax})";
        }

        if (FileLength <= 0)
        {
            yield return $"Invalid file length: {FileLength}";
        }
    }

    public override string ToString()
    {
        var bounds = $"[{XMin:F6}, {YMin:F6}, {XMax:F6}, {YMax:F6}]";
        var zInfo = HasZ && ZMin.HasValue && ZMax.HasValue ? $" Z:[{ZMin:F6}, {ZMax:F6}]" : "";
        var mInfo = HasM && MMin.HasValue && MMax.HasValue ? $" M:[{MMin:F6}, {MMax:F6}]" : "";

        return $"Shapefile Header: {ShapeType.GetDescription()}, "
            + $"Length: {FileLengthInBytes:N0} bytes, "
            + $"Bounds: {bounds}{zInfo}{mInfo}";
    }
}
