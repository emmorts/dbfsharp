using System.Buffers.Binary;
using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Parsing;

/// <summary>
/// Provides parsing functionality for shapefile geometry data
/// </summary>
public static class ShapeParser
{
    /// <summary>
    /// Parses a shape from the given shape data bytes
    /// </summary>
    /// <param name="shapeData">The shape data bytes from a shapefile record</param>
    /// <returns>The parsed shape geometry</returns>
    /// <exception cref="ArgumentException">Thrown when shapeData is invalid</exception>
    /// <exception cref="FormatException">Thrown when the shape data format is invalid</exception>
    public static Shape ParseShape(ReadOnlySpan<byte> shapeData)
    {
        if (shapeData.Length < 4)
        {
            throw new ArgumentException(
                "Shape data must be at least 4 bytes for shape type",
                nameof(shapeData)
            );
        }

        // First 4 bytes are the shape type (little-endian)
        var shapeTypeValue = BinaryPrimitives.ReadInt32LittleEndian(shapeData[0..4]);

        if (!Enum.IsDefined(typeof(ShapeType), shapeTypeValue))
        {
            throw new FormatException($"Unknown shape type: {shapeTypeValue}");
        }

        var shapeType = (ShapeType)shapeTypeValue;

        return shapeType switch
        {
            ShapeType.NullShape => NullShape.Instance,
            ShapeType.Point => ParsePoint(shapeData[4..]),
            ShapeType.PolyLine => ParsePolyLine(shapeData[4..]),
            ShapeType.Polygon => ParsePolygon(shapeData[4..]),
            ShapeType.MultiPoint => ParseMultiPoint(shapeData[4..]),
            ShapeType.PointZ => ParsePointZ(shapeData[4..]),
            ShapeType.PolyLineZ => ParsePolyLineZ(shapeData[4..]),
            ShapeType.PolygonZ => ParsePolygonZ(shapeData[4..]),
            ShapeType.MultiPointZ => ParseMultiPointZ(shapeData[4..]),
            ShapeType.PointM => ParsePointM(shapeData[4..]),
            ShapeType.PolyLineM => ParsePolyLineM(shapeData[4..]),
            ShapeType.PolygonM => ParsePolygonM(shapeData[4..]),
            ShapeType.MultiPointM => ParseMultiPointM(shapeData[4..]),
            ShapeType.MultiPatch => ParseMultiPatch(shapeData[4..]),
            _ => throw new FormatException($"Unsupported shape type: {shapeType}"),
        };
    }

    /// <summary>
    /// Parses a 2D point shape
    /// </summary>
    private static Point ParsePoint(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            throw new FormatException("Point data must be at least 16 bytes (X, Y coordinates)");
        }

        var x = BinaryPrimitives.ReadDoubleLittleEndian(data[0..8]);
        var y = BinaryPrimitives.ReadDoubleLittleEndian(data[8..16]);

        return new Point(x, y);
    }

    /// <summary>
    /// Parses a 3D point shape with Z coordinate
    /// </summary>
    private static Point ParsePointZ(ReadOnlySpan<byte> data)
    {
        if (data.Length < 24)
        {
            throw new FormatException(
                "PointZ data must be at least 24 bytes (X, Y, Z coordinates)"
            );
        }

        var x = BinaryPrimitives.ReadDoubleLittleEndian(data[0..8]);
        var y = BinaryPrimitives.ReadDoubleLittleEndian(data[8..16]);
        var z = BinaryPrimitives.ReadDoubleLittleEndian(data[16..24]);

        // Check if there's also an M value (total 32 bytes)
        double? m = null;
        if (data.Length >= 32)
        {
            var mValue = BinaryPrimitives.ReadDoubleLittleEndian(data[24..32]);
            if (!double.IsNaN(mValue))
            {
                m = mValue;
            }
        }

        return new Point(x, y, z, m);
    }

    /// <summary>
    /// Parses a point shape with M (measure) coordinate
    /// </summary>
    private static Point ParsePointM(ReadOnlySpan<byte> data)
    {
        if (data.Length < 24)
        {
            throw new FormatException(
                "PointM data must be at least 24 bytes (X, Y, M coordinates)"
            );
        }

        var x = BinaryPrimitives.ReadDoubleLittleEndian(data[0..8]);
        var y = BinaryPrimitives.ReadDoubleLittleEndian(data[8..16]);
        var m = BinaryPrimitives.ReadDoubleLittleEndian(data[16..24]);

        return new Point(x, y, null, double.IsNaN(m) ? null : m);
    }

    /// <summary>
    /// Parses a multipoint shape
    /// </summary>
    private static MultiPoint ParseMultiPoint(ReadOnlySpan<byte> data)
    {
        if (data.Length < 32)
        {
            throw new FormatException(
                "MultiPoint data must be at least 32 bytes (bounding box + point count)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of points
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        offset += 4;

        if (numPoints < 0)
        {
            throw new FormatException($"Invalid number of points: {numPoints}");
        }

        if (data.Length < offset + numPoints * 16)
        {
            throw new FormatException($"MultiPoint data too short for {numPoints} points");
        }

        var coordinates = new Coordinate[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            coordinates[i] = new Coordinate(x, y);
            offset += 16;
        }

        return new MultiPoint(coordinates);
    }

    /// <summary>
    /// Parses a multipoint shape with Z coordinates
    /// </summary>
    private static MultiPoint ParseMultiPointZ(ReadOnlySpan<byte> data)
    {
        if (data.Length < 32)
        {
            throw new FormatException(
                "MultiPointZ data must be at least 32 bytes (bounding box + point count)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of points
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        offset += 4;

        if (numPoints < 0)
        {
            throw new FormatException($"Invalid number of points: {numPoints}");
        }

        var minRequiredLength = offset + numPoints * 16 + 16 + numPoints * 8; // XY points + Z range + Z values
        if (data.Length < minRequiredLength)
        {
            throw new FormatException($"MultiPointZ data too short for {numPoints} points");
        }

        // Read XY coordinates
        var coordinates = new List<Coordinate>(numPoints);
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            coordinates.Add(new Coordinate(x, y));
            offset += 16;
        }

        // Skip Z range (16 bytes: Zmin, Zmax)
        offset += 16;

        // Read Z coordinates
        for (int i = 0; i < numPoints; i++)
        {
            var z = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var coord = coordinates[i];
            coordinates[i] = new Coordinate(coord.X, coord.Y, z);
            offset += 8;
        }

        // Check for M values (optional)
        if (data.Length >= offset + 16 + numPoints * 8) // M range + M values
        {
            // Skip M range
            offset += 16;

            // Read M coordinates
            for (int i = 0; i < numPoints; i++)
            {
                var m = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                var coord = coordinates[i];
                coordinates[i] = new Coordinate(
                    coord.X,
                    coord.Y,
                    coord.Z,
                    double.IsNaN(m) ? null : m
                );
                offset += 8;
            }
        }

        return new MultiPoint(coordinates);
    }

    /// <summary>
    /// Parses a multipoint shape with M coordinates
    /// </summary>
    private static MultiPoint ParseMultiPointM(ReadOnlySpan<byte> data)
    {
        if (data.Length < 32)
        {
            throw new FormatException(
                "MultiPointM data must be at least 32 bytes (bounding box + point count)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of points
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        offset += 4;

        if (numPoints < 0)
        {
            throw new FormatException($"Invalid number of points: {numPoints}");
        }

        var minRequiredLength = offset + numPoints * 16 + 16 + numPoints * 8; // XY points + M range + M values
        if (data.Length < minRequiredLength)
        {
            throw new FormatException($"MultiPointM data too short for {numPoints} points");
        }

        // Read XY coordinates
        var coordinates = new List<Coordinate>(numPoints);
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            coordinates.Add(new Coordinate(x, y));
            offset += 16;
        }

        // Skip M range (16 bytes: Mmin, Mmax)
        offset += 16;

        // Read M coordinates
        for (int i = 0; i < numPoints; i++)
        {
            var m = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var coord = coordinates[i];
            coordinates[i] = new Coordinate(coord.X, coord.Y, null, double.IsNaN(m) ? null : m);
            offset += 8;
        }

        return new MultiPoint(coordinates);
    }

    /// <summary>
    /// Parses a polyline shape
    /// </summary>
    private static PolyLine ParsePolyLine(ReadOnlySpan<byte> data)
    {
        if (data.Length < 40)
        {
            throw new FormatException(
                "PolyLine data must be at least 40 bytes (bounding box + part/point counts)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of parts and points
        var numParts = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..(offset + 8)]);
        offset += 8;

        if (numParts < 0 || numPoints < 0)
        {
            throw new FormatException(
                $"Invalid part/point counts: parts={numParts}, points={numPoints}"
            );
        }

        var minRequiredLength = offset + numParts * 4 + numPoints * 16; // part indices + points
        if (data.Length < minRequiredLength)
        {
            throw new FormatException(
                $"PolyLine data too short for {numParts} parts and {numPoints} points"
            );
        }

        // Read part start indices
        var partStarts = new int[numParts];
        for (int i = 0; i < numParts; i++)
        {
            partStarts[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            offset += 4;
        }

        // Read all points
        var allPoints = new Coordinate[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            allPoints[i] = new Coordinate(x, y);
            offset += 16;
        }

        // Split points into parts
        var parts = SplitIntoParts(allPoints, partStarts);

        return new PolyLine(parts);
    }

    /// <summary>
    /// Parses a polyline shape with Z coordinates
    /// </summary>
    private static PolyLine ParsePolyLineZ(ReadOnlySpan<byte> data)
    {
        return ParsePolyLineWithZM(data, hasZ: true, hasM: false);
    }

    /// <summary>
    /// Parses a polyline shape with M coordinates
    /// </summary>
    private static PolyLine ParsePolyLineM(ReadOnlySpan<byte> data)
    {
        return ParsePolyLineWithZM(data, hasZ: false, hasM: true);
    }

    /// <summary>
    /// Parses a polygon shape
    /// </summary>
    private static Polygon ParsePolygon(ReadOnlySpan<byte> data)
    {
        if (data.Length < 40)
        {
            throw new FormatException(
                "Polygon data must be at least 40 bytes (bounding box + part/point counts)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of parts and points
        var numParts = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..(offset + 8)]);
        offset += 8;

        if (numParts < 0 || numPoints < 0)
        {
            throw new FormatException(
                $"Invalid part/point counts: parts={numParts}, points={numPoints}"
            );
        }

        var minRequiredLength = offset + numParts * 4 + numPoints * 16; // part indices + points
        if (data.Length < minRequiredLength)
        {
            throw new FormatException(
                $"Polygon data too short for {numParts} parts and {numPoints} points"
            );
        }

        // Read part start indices
        var partStarts = new int[numParts];
        for (int i = 0; i < numParts; i++)
        {
            partStarts[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            offset += 4;
        }

        // Read all points
        var allPoints = new Coordinate[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            allPoints[i] = new Coordinate(x, y);
            offset += 16;
        }

        // Split points into rings
        var rings = SplitIntoParts(allPoints, partStarts);

        return new Polygon(rings);
    }

    /// <summary>
    /// Parses a polygon shape with Z coordinates
    /// </summary>
    private static Polygon ParsePolygonZ(ReadOnlySpan<byte> data)
    {
        return ParsePolygonWithZM(data, hasZ: true, hasM: false);
    }

    /// <summary>
    /// Parses a polygon shape with M coordinates
    /// </summary>
    private static Polygon ParsePolygonM(ReadOnlySpan<byte> data)
    {
        return ParsePolygonWithZM(data, hasZ: false, hasM: true);
    }

    /// <summary>
    /// Helper method to parse polylines with Z and/or M coordinates
    /// </summary>
    private static PolyLine ParsePolyLineWithZM(ReadOnlySpan<byte> data, bool hasZ, bool hasM)
    {
        if (data.Length < 40)
        {
            throw new FormatException(
                "PolyLine data must be at least 40 bytes (bounding box + part/point counts)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of parts and points
        var numParts = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..(offset + 8)]);
        offset += 8;

        if (numParts < 0 || numPoints < 0)
        {
            throw new FormatException(
                $"Invalid part/point counts: parts={numParts}, points={numPoints}"
            );
        }

        // Calculate minimum required length
        var minLength = offset + numParts * 4 + numPoints * 16; // Part indices + XY coordinates
        if (hasZ)
        {
            minLength += 16 + numPoints * 8; // Z range + Z values
        }

        if (hasM)
        {
            minLength += 16 + numPoints * 8; // M range + M values
        }

        if (data.Length < minLength)
        {
            throw new FormatException(
                $"PolyLine data too short for {numParts} parts and {numPoints} points with Z/M"
            );
        }

        // Read part start indices
        var partStarts = new int[numParts];
        for (int i = 0; i < numParts; i++)
        {
            partStarts[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            offset += 4;
        }

        // Read XY coordinates
        var coordinates = new List<Coordinate>(numPoints);
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            coordinates.Add(new Coordinate(x, y));
            offset += 16;
        }

        // Read Z coordinates if present
        if (hasZ)
        {
            // Skip Z range
            offset += 16;

            for (int i = 0; i < numPoints; i++)
            {
                var z = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                var coord = coordinates[i];
                coordinates[i] = new Coordinate(coord.X, coord.Y, z);
                offset += 8;
            }
        }

        // Read M coordinates if present
        if (hasM)
        {
            // Skip M range
            offset += 16;

            for (int i = 0; i < numPoints; i++)
            {
                var m = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                var coord = coordinates[i];
                coordinates[i] = new Coordinate(
                    coord.X,
                    coord.Y,
                    coord.Z,
                    double.IsNaN(m) ? null : m
                );
                offset += 8;
            }
        }

        // Split points into parts
        var parts = SplitIntoParts(coordinates.ToArray(), partStarts);

        return new PolyLine(parts);
    }

    /// <summary>
    /// Helper method to parse polygons with Z and/or M coordinates
    /// </summary>
    private static Polygon ParsePolygonWithZM(ReadOnlySpan<byte> data, bool hasZ, bool hasM)
    {
        if (data.Length < 40)
        {
            throw new FormatException(
                "Polygon data must be at least 40 bytes (bounding box + part/point counts)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of parts and points
        var numParts = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..(offset + 8)]);
        offset += 8;

        if (numParts < 0 || numPoints < 0)
        {
            throw new FormatException(
                $"Invalid part/point counts: parts={numParts}, points={numPoints}"
            );
        }

        // Calculate minimum required length
        var minLength = offset + numParts * 4 + numPoints * 16; // Part indices + XY coordinates
        if (hasZ)
        {
            minLength += 16 + numPoints * 8; // Z range + Z values
        }

        if (hasM)
        {
            minLength += 16 + numPoints * 8; // M range + M values
        }

        if (data.Length < minLength)
        {
            throw new FormatException(
                $"Polygon data too short for {numParts} parts and {numPoints} points with Z/M"
            );
        }

        // Read part start indices
        var partStarts = new int[numParts];
        for (int i = 0; i < numParts; i++)
        {
            partStarts[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            offset += 4;
        }

        // Read XY coordinates
        var coordinates = new List<Coordinate>(numPoints);
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            coordinates.Add(new Coordinate(x, y));
            offset += 16;
        }

        // Read Z coordinates if present
        if (hasZ)
        {
            // Skip Z range
            offset += 16;

            for (int i = 0; i < numPoints; i++)
            {
                var z = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                var coord = coordinates[i];
                coordinates[i] = new Coordinate(coord.X, coord.Y, z);
                offset += 8;
            }
        }

        // Read M coordinates if present
        if (hasM)
        {
            // Skip M range
            offset += 16;

            for (int i = 0; i < numPoints; i++)
            {
                var m = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                var coord = coordinates[i];
                coordinates[i] = new Coordinate(
                    coord.X,
                    coord.Y,
                    coord.Z,
                    double.IsNaN(m) ? null : m
                );
                offset += 8;
            }
        }

        // Split points into rings
        var rings = SplitIntoParts(coordinates.ToArray(), partStarts);

        return new Polygon(rings);
    }

    /// <summary>
    /// Parses a MultiPatch shape with complex 3D surface geometry
    /// </summary>
    private static MultiPatch ParseMultiPatch(ReadOnlySpan<byte> data)
    {
        if (data.Length < 40)
        {
            throw new FormatException(
                "MultiPatch data must be at least 40 bytes (bounding box + part/point counts)"
            );
        }

        // Skip bounding box (32 bytes: Xmin, Ymin, Xmax, Ymax)
        var offset = 32;

        // Read number of parts and points
        var numParts = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
        var numPoints = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..(offset + 8)]);
        offset += 8;

        if (numParts < 0 || numPoints < 0)
        {
            throw new FormatException(
                $"Invalid part/point counts: parts={numParts}, points={numPoints}"
            );
        }

        // Calculate minimum required length
        var minLength = offset + numParts * 4 + numParts * 4 + numPoints * 16; // Part indices + part types + XY coordinates
        minLength += 16 + numPoints * 8; // Z range + Z values (required for MultiPatch)

        if (data.Length < minLength)
        {
            throw new FormatException(
                $"MultiPatch data too short for {numParts} parts and {numPoints} points"
            );
        }

        // Read part start indices
        var partStarts = new int[numParts];
        for (int i = 0; i < numParts; i++)
        {
            partStarts[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            offset += 4;
        }

        // Read part types
        var partTypes = new PatchType[numParts];
        for (int i = 0; i < numParts; i++)
        {
            var partTypeValue = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            if (!Enum.IsDefined(typeof(PatchType), partTypeValue))
            {
                throw new FormatException($"Unknown patch type: {partTypeValue}");
            }
            partTypes[i] = (PatchType)partTypeValue;
            offset += 4;
        }

        // Read XY coordinates
        var coordinates = new List<Coordinate>(numPoints);
        for (int i = 0; i < numPoints; i++)
        {
            var x = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var y = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
            coordinates.Add(new Coordinate(x, y));
            offset += 16;
        }

        // Skip Z range (16 bytes)
        offset += 16;

        // Read Z coordinates (required for MultiPatch)
        for (int i = 0; i < numPoints; i++)
        {
            var z = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
            var coord = coordinates[i];
            coordinates[i] = new Coordinate(coord.X, coord.Y, z);
            offset += 8;
        }

        // Check for M values (optional)
        if (data.Length >= offset + 16 + numPoints * 8) // M range + M values
        {
            // Skip M range
            offset += 16;

            // Read M coordinates
            for (int i = 0; i < numPoints; i++)
            {
                var m = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                var coord = coordinates[i];
                coordinates[i] = new Coordinate(
                    coord.X,
                    coord.Y,
                    coord.Z,
                    double.IsNaN(m) ? null : m
                );
                offset += 8;
            }
        }

        // Split coordinates into patch parts
        var parts = new List<PatchPart>();
        for (int partIndex = 0; partIndex < partStarts.Length; partIndex++)
        {
            var startIndex = partStarts[partIndex];
            var endIndex =
                partIndex < partStarts.Length - 1 ? partStarts[partIndex + 1] : coordinates.Count;

            if (startIndex < 0 || startIndex >= coordinates.Count)
            {
                throw new FormatException($"Invalid part start index: {startIndex}");
            }

            if (endIndex <= startIndex || endIndex > coordinates.Count)
            {
                throw new FormatException(
                    $"Invalid part end index: {endIndex} (start: {startIndex})"
                );
            }

            var partCoordinates = new List<Coordinate>();
            for (int i = startIndex; i < endIndex; i++)
            {
                partCoordinates.Add(coordinates[i]);
            }

            parts.Add(new PatchPart(partTypes[partIndex], partCoordinates));
        }

        return new MultiPatch(parts);
    }

    /// <summary>
    /// Helper method to split coordinates into parts based on part start indices
    /// </summary>
    private static IEnumerable<IEnumerable<Coordinate>> SplitIntoParts(
        Coordinate[] allCoordinates,
        int[] partStarts
    )
    {
        var parts = new List<List<Coordinate>>();

        for (int partIndex = 0; partIndex < partStarts.Length; partIndex++)
        {
            var startIndex = partStarts[partIndex];
            var endIndex =
                partIndex < partStarts.Length - 1
                    ? partStarts[partIndex + 1]
                    : allCoordinates.Length;

            if (startIndex < 0 || startIndex >= allCoordinates.Length)
            {
                throw new FormatException($"Invalid part start index: {startIndex}");
            }

            if (endIndex <= startIndex || endIndex > allCoordinates.Length)
            {
                throw new FormatException(
                    $"Invalid part end index: {endIndex} (start: {startIndex})"
                );
            }

            var partCoordinates = new List<Coordinate>();
            for (int i = startIndex; i < endIndex; i++)
            {
                partCoordinates.Add(allCoordinates[i]);
            }

            parts.Add(partCoordinates);
        }

        return parts;
    }
}
