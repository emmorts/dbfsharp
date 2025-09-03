using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Projection;

/// <summary>
/// Provides coordinate transformation services for geometric data
/// </summary>
public static class TransformationEngine
{
    /// <summary>
    /// Transforms a shape from one coordinate system to another
    /// </summary>
    /// <param name="shape">The shape to transform</param>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>A new shape with transformed coordinates</returns>
    public static Shape Transform(
        Shape shape,
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (shape == null)
        {
            throw new ArgumentNullException(nameof(shape));
        }

        if (sourceCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(sourceCoordinateSystem));
        }

        if (targetCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(targetCoordinateSystem));
        }

        var transformation = new CoordinateTransformation(
            sourceCoordinateSystem,
            targetCoordinateSystem
        );
        return shape.Transform(transformation.CreateTransformFunction());
    }

    /// <summary>
    /// Transforms a shape using EPSG codes
    /// </summary>
    /// <param name="shape">The shape to transform</param>
    /// <param name="sourceEpsgCode">The source EPSG code</param>
    /// <param name="targetEpsgCode">The target EPSG code</param>
    /// <returns>A new shape with transformed coordinates</returns>
    public static Shape Transform(Shape shape, int sourceEpsgCode, int targetEpsgCode)
    {
        if (shape == null)
        {
            throw new ArgumentNullException(nameof(shape));
        }

        var sourceCs = EpsgDatabase.CreateProjectionFile(sourceEpsgCode);
        if (sourceCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {sourceEpsgCode}",
                nameof(sourceEpsgCode)
            );
        }

        var targetCs = EpsgDatabase.CreateProjectionFile(targetEpsgCode);
        if (targetCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {targetEpsgCode}",
                nameof(targetEpsgCode)
            );
        }

        return Transform(shape, sourceCs, targetCs);
    }

    /// <summary>
    /// Transforms multiple shapes from one coordinate system to another
    /// </summary>
    /// <param name="shapes">The shapes to transform</param>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>New shapes with transformed coordinates</returns>
    public static IEnumerable<Shape> Transform(
        IEnumerable<Shape> shapes,
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (shapes == null)
        {
            throw new ArgumentNullException(nameof(shapes));
        }

        if (sourceCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(sourceCoordinateSystem));
        }

        if (targetCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(targetCoordinateSystem));
        }

        var transformation = new CoordinateTransformation(
            sourceCoordinateSystem,
            targetCoordinateSystem
        );
        var transformFunc = transformation.CreateTransformFunction();

        return shapes.Select(shape => shape.Transform(transformFunc));
    }

    /// <summary>
    /// Transforms multiple shapes using EPSG codes
    /// </summary>
    /// <param name="shapes">The shapes to transform</param>
    /// <param name="sourceEpsgCode">The source EPSG code</param>
    /// <param name="targetEpsgCode">The target EPSG code</param>
    /// <returns>New shapes with transformed coordinates</returns>
    public static IEnumerable<Shape> Transform(
        IEnumerable<Shape> shapes,
        int sourceEpsgCode,
        int targetEpsgCode
    )
    {
        if (shapes == null)
        {
            throw new ArgumentNullException(nameof(shapes));
        }

        var sourceCs = EpsgDatabase.CreateProjectionFile(sourceEpsgCode);
        if (sourceCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {sourceEpsgCode}",
                nameof(sourceEpsgCode)
            );
        }

        var targetCs = EpsgDatabase.CreateProjectionFile(targetEpsgCode);
        if (targetCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {targetEpsgCode}",
                nameof(targetEpsgCode)
            );
        }

        return Transform(shapes, sourceCs, targetCs);
    }

    /// <summary>
    /// Transforms a coordinate from one coordinate system to another
    /// </summary>
    /// <param name="coordinate">The coordinate to transform</param>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>The transformed coordinate</returns>
    public static Coordinate Transform(
        Coordinate coordinate,
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (sourceCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(sourceCoordinateSystem));
        }

        if (targetCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(targetCoordinateSystem));
        }

        var transformation = new CoordinateTransformation(
            sourceCoordinateSystem,
            targetCoordinateSystem
        );
        return transformation.Transform(coordinate);
    }

    /// <summary>
    /// Transforms a coordinate using EPSG codes
    /// </summary>
    /// <param name="coordinate">The coordinate to transform</param>
    /// <param name="sourceEpsgCode">The source EPSG code</param>
    /// <param name="targetEpsgCode">The target EPSG code</param>
    /// <returns>The transformed coordinate</returns>
    public static Coordinate Transform(
        Coordinate coordinate,
        int sourceEpsgCode,
        int targetEpsgCode
    )
    {
        var sourceCs = EpsgDatabase.CreateProjectionFile(sourceEpsgCode);
        if (sourceCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {sourceEpsgCode}",
                nameof(sourceEpsgCode)
            );
        }

        var targetCs = EpsgDatabase.CreateProjectionFile(targetEpsgCode);
        if (targetCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {targetEpsgCode}",
                nameof(targetEpsgCode)
            );
        }

        return Transform(coordinate, sourceCs, targetCs);
    }

    /// <summary>
    /// Transforms multiple coordinates from one coordinate system to another
    /// </summary>
    /// <param name="coordinates">The coordinates to transform</param>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>The transformed coordinates</returns>
    public static IEnumerable<Coordinate> Transform(
        IEnumerable<Coordinate> coordinates,
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (coordinates == null)
        {
            throw new ArgumentNullException(nameof(coordinates));
        }

        if (sourceCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(sourceCoordinateSystem));
        }

        if (targetCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(targetCoordinateSystem));
        }

        var transformation = new CoordinateTransformation(
            sourceCoordinateSystem,
            targetCoordinateSystem
        );
        return transformation.Transform(coordinates);
    }

    /// <summary>
    /// Creates a transformation between two coordinate systems
    /// </summary>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>A coordinate transformation object</returns>
    public static CoordinateTransformation CreateTransformation(
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (sourceCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(sourceCoordinateSystem));
        }

        if (targetCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(targetCoordinateSystem));
        }

        return new CoordinateTransformation(sourceCoordinateSystem, targetCoordinateSystem);
    }

    /// <summary>
    /// Creates a transformation using EPSG codes
    /// </summary>
    /// <param name="sourceEpsgCode">The source EPSG code</param>
    /// <param name="targetEpsgCode">The target EPSG code</param>
    /// <returns>A coordinate transformation object</returns>
    public static CoordinateTransformation CreateTransformation(
        int sourceEpsgCode,
        int targetEpsgCode
    )
    {
        var sourceCs = EpsgDatabase.CreateProjectionFile(sourceEpsgCode);
        if (sourceCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {sourceEpsgCode}",
                nameof(sourceEpsgCode)
            );
        }

        var targetCs = EpsgDatabase.CreateProjectionFile(targetEpsgCode);
        if (targetCs == null)
        {
            throw new ArgumentException(
                $"Unknown EPSG code: {targetEpsgCode}",
                nameof(targetEpsgCode)
            );
        }

        return new CoordinateTransformation(sourceCs, targetCs);
    }

    /// <summary>
    /// Checks if a transformation between two coordinate systems is supported
    /// </summary>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>True if the transformation is supported</returns>
    public static bool IsTransformationSupported(
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (sourceCoordinateSystem == null || targetCoordinateSystem == null)
        {
            return false;
        }

        try
        {
            var transformation = new CoordinateTransformation(
                sourceCoordinateSystem,
                targetCoordinateSystem
            );
            return transformation.IsValid
                && transformation.TransformationType != TransformationType.Unsupported;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Transforms a bounding box from one coordinate system to another
    /// </summary>
    /// <param name="boundingBox">The bounding box to transform</param>
    /// <param name="sourceCoordinateSystem">The source coordinate system</param>
    /// <param name="targetCoordinateSystem">The target coordinate system</param>
    /// <returns>A new bounding box with transformed coordinates</returns>
    public static BoundingBox Transform(
        BoundingBox boundingBox,
        ProjectionFile sourceCoordinateSystem,
        ProjectionFile targetCoordinateSystem
    )
    {
        if (sourceCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(sourceCoordinateSystem));
        }

        if (targetCoordinateSystem == null)
        {
            throw new ArgumentNullException(nameof(targetCoordinateSystem));
        }

        if (boundingBox.IsEmpty)
        {
            return new BoundingBox(0, 0, 0, 0); // Create an empty bounding box
        }

        var transformation = new CoordinateTransformation(
            sourceCoordinateSystem,
            targetCoordinateSystem
        );

        // Transform all corners of the bounding box to ensure we capture the full extent
        var corners = new[]
        {
            new Coordinate(boundingBox.MinX, boundingBox.MinY, boundingBox.MinZ, boundingBox.MinM),
            new Coordinate(boundingBox.MinX, boundingBox.MaxY, boundingBox.MinZ, boundingBox.MinM),
            new Coordinate(boundingBox.MaxX, boundingBox.MinY, boundingBox.MinZ, boundingBox.MinM),
            new Coordinate(boundingBox.MaxX, boundingBox.MaxY, boundingBox.MinZ, boundingBox.MinM),
        };

        if (boundingBox.HasZ)
        {
            // Add corners with max Z values for 3D bounding boxes
            var cornersWithMaxZ = new[]
            {
                new Coordinate(
                    boundingBox.MinX,
                    boundingBox.MinY,
                    boundingBox.MaxZ,
                    boundingBox.MinM
                ),
                new Coordinate(
                    boundingBox.MinX,
                    boundingBox.MaxY,
                    boundingBox.MaxZ,
                    boundingBox.MinM
                ),
                new Coordinate(
                    boundingBox.MaxX,
                    boundingBox.MinY,
                    boundingBox.MaxZ,
                    boundingBox.MinM
                ),
                new Coordinate(
                    boundingBox.MaxX,
                    boundingBox.MaxY,
                    boundingBox.MaxZ,
                    boundingBox.MinM
                ),
            };
            corners = corners.Concat(cornersWithMaxZ).ToArray();
        }

        var transformedCorners = transformation.Transform(corners).ToList();

        if (transformedCorners.Count == 0)
        {
            return new BoundingBox(0, 0, 0, 0); // Create an empty bounding box
        }

        return BoundingBox.FromCoordinates(transformedCorners);
    }
}
