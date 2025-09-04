using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Spatial;

/// <summary>
/// Spatial relationship enumeration for geometry comparisons
/// </summary>
public enum SpatialRelationship
{
    /// <summary>
    /// Geometries do not intersect
    /// </summary>
    Disjoint,

    /// <summary>
    /// Geometries intersect
    /// </summary>
    Intersects,

    /// <summary>
    /// First geometry completely contains the second
    /// </summary>
    Contains,

    /// <summary>
    /// First geometry is completely within the second
    /// </summary>
    Within,

    /// <summary>
    /// Geometries intersect but neither contains the other
    /// </summary>
    Overlaps,

    /// <summary>
    /// Geometries touch at their boundaries
    /// </summary>
    Touches,

    /// <summary>
    /// Geometries cross each other
    /// </summary>
    Crosses,

    /// <summary>
    /// Geometries are topologically equal
    /// </summary>
    Equal,
}

/// <summary>
/// Provides spatial operations and relationship testing for shapefile geometries
/// </summary>
public static class SpatialOperations
{
    /// <summary>
    /// Tests if two shapes intersect (have any spatial overlap)
    /// </summary>
    /// <param name="shape1">The first shape</param>
    /// <param name="shape2">The second shape</param>
    /// <returns>True if the shapes intersect</returns>
    public static bool Intersects(Shape shape1, Shape shape2)
    {
        if (shape1 == null || shape2 == null)
            return false;

        if (shape1.IsEmpty || shape2.IsEmpty)
            return false;

        // Quick bounding box test first
        if (!shape1.BoundingBox.Intersects(shape2.BoundingBox))
            return false;

        // For simple cases, bounding box intersection is sufficient
        if (shape1 is Point || shape2 is Point)
        {
            return IntersectsWithPoint(shape1, shape2);
        }

        // For more complex geometries, we need detailed geometric tests
        return IntersectsDetailed(shape1, shape2);
    }

    /// <summary>
    /// Tests if the first shape completely contains the second shape
    /// </summary>
    /// <param name="container">The potentially containing shape</param>
    /// <param name="contained">The potentially contained shape</param>
    /// <returns>True if the first shape contains the second</returns>
    public static bool Contains(Shape container, Shape contained)
    {
        if (container == null || contained == null)
            return false;

        if (container.IsEmpty || contained.IsEmpty)
            return false;

        // Container's bounding box must contain the contained shape's bounding box
        if (!ContainsBoundingBox(container.BoundingBox, contained.BoundingBox))
            return false;

        // For point containment, we can use efficient tests
        if (contained is Point point)
        {
            return ContainsPoint(container, point);
        }

        // For more complex containment, we need detailed geometric tests
        return ContainsDetailed(container, contained);
    }

    /// <summary>
    /// Tests if the first shape is completely within the second shape
    /// </summary>
    /// <param name="inner">The potentially inner shape</param>
    /// <param name="outer">The potentially outer shape</param>
    /// <returns>True if the first shape is within the second</returns>
    public static bool Within(Shape inner, Shape outer)
    {
        return Contains(outer, inner);
    }

    /// <summary>
    /// Tests if two shapes overlap (intersect but neither contains the other)
    /// </summary>
    /// <param name="shape1">The first shape</param>
    /// <param name="shape2">The second shape</param>
    /// <returns>True if the shapes overlap</returns>
    public static bool Overlaps(Shape shape1, Shape shape2)
    {
        if (!Intersects(shape1, shape2))
            return false;

        return !Contains(shape1, shape2) && !Contains(shape2, shape1);
    }

    /// <summary>
    /// Tests if two shapes touch at their boundaries but do not overlap
    /// </summary>
    /// <param name="shape1">The first shape</param>
    /// <param name="shape2">The second shape</param>
    /// <returns>True if the shapes touch</returns>
    public static bool Touches(Shape shape1, Shape shape2)
    {
        if (shape1 == null || shape2 == null)
            return false;

        if (shape1.IsEmpty || shape2.IsEmpty)
            return false;

        // Quick bounding box test - shapes must intersect to touch
        if (!shape1.BoundingBox.Intersects(shape2.BoundingBox))
            return false;

        // For now, implement basic touch detection using boundary analysis
        return TouchesDetailed(shape1, shape2);
    }

    /// <summary>
    /// Tests if two shapes cross each other (intersect at interior points)
    /// </summary>
    /// <param name="shape1">The first shape</param>
    /// <param name="shape2">The second shape</param>
    /// <returns>True if the shapes cross</returns>
    public static bool Crosses(Shape shape1, Shape shape2)
    {
        if (!Intersects(shape1, shape2))
            return false;

        // Crossing implies intersection but not containment or touching only
        return !Contains(shape1, shape2) && !Contains(shape2, shape1) && !Touches(shape1, shape2);
    }

    /// <summary>
    /// Calculates the minimum distance between two shapes
    /// </summary>
    /// <param name="shape1">The first shape</param>
    /// <param name="shape2">The second shape</param>
    /// <returns>The minimum distance between the shapes</returns>
    public static double Distance(Shape shape1, Shape shape2)
    {
        if (shape1 == null || shape2 == null)
            return double.PositiveInfinity;

        if (Intersects(shape1, shape2))
            return 0.0;

        return CalculateMinDistance(shape1, shape2);
    }

    /// <summary>
    /// Determines the spatial relationship between two shapes
    /// </summary>
    /// <param name="shape1">The first shape</param>
    /// <param name="shape2">The second shape</param>
    /// <returns>The spatial relationship</returns>
    public static SpatialRelationship GetRelationship(Shape shape1, Shape shape2)
    {
        if (shape1 == null || shape2 == null)
            return SpatialRelationship.Disjoint;

        if (shape1.IsEmpty || shape2.IsEmpty)
            return SpatialRelationship.Disjoint;

        if (!shape1.BoundingBox.Intersects(shape2.BoundingBox))
            return SpatialRelationship.Disjoint;

        if (AreTopologicallyEqual(shape1, shape2))
            return SpatialRelationship.Equal;

        if (Contains(shape1, shape2))
            return SpatialRelationship.Contains;

        if (Contains(shape2, shape1))
            return SpatialRelationship.Within;

        if (Touches(shape1, shape2))
            return SpatialRelationship.Touches;

        if (Crosses(shape1, shape2))
            return SpatialRelationship.Crosses;

        if (Intersects(shape1, shape2))
            return SpatialRelationship.Overlaps;

        return SpatialRelationship.Disjoint;
    }

    // Private helper methods

    private static bool IntersectsWithPoint(Shape shape1, Shape shape2)
    {
        if (shape1 is Point point1 && shape2 is Point point2)
        {
            return point1.Coordinate.Equals(point2.Coordinate);
        }

        var point = shape1 is Point p1 ? p1 : (Point)shape2;
        var other = shape1 is Point ? shape2 : shape1;

        return ContainsPoint(other, point);
    }

    private static bool IntersectsDetailed(Shape shape1, Shape shape2)
    {
        // For now, implement basic intersection using bounding box tests
        // This is a simplified implementation that can be enhanced with more precise geometric algorithms
        return shape1.BoundingBox.Intersects(shape2.BoundingBox);
    }

    private static bool ContainsBoundingBox(BoundingBox container, BoundingBox contained)
    {
        return container.MinX <= contained.MinX
            && container.MinY <= contained.MinY
            && container.MaxX >= contained.MaxX
            && container.MaxY >= contained.MaxY;
    }

    private static bool ContainsPoint(Shape container, Point point)
    {
        if (!container.BoundingBox.Contains(point.Coordinate))
            return false;

        return container switch
        {
            Point containerPoint => containerPoint.Coordinate.Equals(point.Coordinate),
            Polygon polygon => IsPointInPolygon(point.Coordinate, polygon),
            PolyLine polyLine => IsPointOnPolyLine(point.Coordinate, polyLine),
            MultiPoint multiPoint => multiPoint
                .GetCoordinates()
                .Any(p => p.Equals(point.Coordinate)),
            _ => false,
        };
    }

    private static bool ContainsDetailed(Shape container, Shape contained)
    {
        // Simplified implementation - can be enhanced with more precise algorithms
        if (contained is Point point)
        {
            return ContainsPoint(container, point);
        }

        // For complex geometries, check if all coordinates are contained
        var containedCoords = contained.GetCoordinates();
        return containedCoords.All(coord =>
        {
            var testPoint = new Point(coord);
            return ContainsPoint(container, testPoint);
        });
    }

    private static bool TouchesDetailed(Shape shape1, Shape shape2)
    {
        // Simplified touch detection - can be enhanced with boundary analysis
        // For now, return false as this requires complex geometric algorithms
        return false;
    }

    private static double CalculateMinDistance(Shape shape1, Shape shape2)
    {
        // Calculate minimum distance between all coordinate pairs
        var coords1 = shape1.GetCoordinates();
        var coords2 = shape2.GetCoordinates();

        var minDistance = double.PositiveInfinity;

        foreach (var c1 in coords1)
        {
            foreach (var c2 in coords2)
            {
                var distance = c1.DistanceTo(c2);
                minDistance = Math.Min(minDistance, distance);
            }
        }

        return minDistance;
    }

    private static bool AreTopologicallyEqual(Shape shape1, Shape shape2)
    {
        // Basic equality check - can be enhanced with more sophisticated comparison
        if (shape1.GetType() != shape2.GetType())
            return false;

        if (!shape1.BoundingBox.Equals(shape2.BoundingBox))
            return false;

        var coords1 = shape1.GetCoordinates().ToArray();
        var coords2 = shape2.GetCoordinates().ToArray();

        if (coords1.Length != coords2.Length)
            return false;

        return coords1.SequenceEqual(coords2);
    }

    private static bool IsPointInPolygon(Coordinate point, Polygon polygon)
    {
        // Ray casting algorithm for point-in-polygon test
        var inside = false;
        var coords = polygon.GetCoordinates().ToArray();

        for (int i = 0, j = coords.Length - 1; i < coords.Length; j = i++)
        {
            var xi = coords[i].X;
            var yi = coords[i].Y;
            var xj = coords[j].X;
            var yj = coords[j].Y;

            if (
                ((yi > point.Y) != (yj > point.Y))
                && (point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi)
            )
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool IsPointOnPolyLine(Coordinate point, PolyLine polyLine)
    {
        const double tolerance = 1e-10;
        var coords = polyLine.GetCoordinates().ToArray();

        for (var i = 0; i < coords.Length - 1; i++)
        {
            if (IsPointOnLineSegment(point, coords[i], coords[i + 1], tolerance))
                return true;
        }

        return false;
    }

    private static bool IsPointOnLineSegment(
        Coordinate point,
        Coordinate lineStart,
        Coordinate lineEnd,
        double tolerance
    )
    {
        // Calculate distance from point to line segment
        var A = point.X - lineStart.X;
        var B = point.Y - lineStart.Y;
        var C = lineEnd.X - lineStart.X;
        var D = lineEnd.Y - lineStart.Y;

        var dot = A * C + B * D;
        var lenSq = C * C + D * D;

        if (lenSq == 0) // Line segment is a point
            return Math.Abs(A) < tolerance && Math.Abs(B) < tolerance;

        var param = dot / lenSq;

        double xx,
            yy;
        if (param < 0)
        {
            xx = lineStart.X;
            yy = lineStart.Y;
        }
        else if (param > 1)
        {
            xx = lineEnd.X;
            yy = lineEnd.Y;
        }
        else
        {
            xx = lineStart.X + param * C;
            yy = lineStart.Y + param * D;
        }

        var dx = point.X - xx;
        var dy = point.Y - yy;
        return Math.Sqrt(dx * dx + dy * dy) < tolerance;
    }
}

/// <summary>
/// Extension methods for spatial operations on shapes
/// </summary>
public static class SpatialExtensions
{
    /// <summary>
    /// Tests if this shape intersects with another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>True if the shapes intersect</returns>
    public static bool Intersects(this Shape shape, Shape other)
    {
        return SpatialOperations.Intersects(shape, other);
    }

    /// <summary>
    /// Tests if this shape contains another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>True if this shape contains the other</returns>
    public static bool Contains(this Shape shape, Shape other)
    {
        return SpatialOperations.Contains(shape, other);
    }

    /// <summary>
    /// Tests if this shape is within another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>True if this shape is within the other</returns>
    public static bool Within(this Shape shape, Shape other)
    {
        return SpatialOperations.Within(shape, other);
    }

    /// <summary>
    /// Tests if this shape overlaps with another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>True if the shapes overlap</returns>
    public static bool Overlaps(this Shape shape, Shape other)
    {
        return SpatialOperations.Overlaps(shape, other);
    }

    /// <summary>
    /// Tests if this shape touches another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>True if the shapes touch</returns>
    public static bool Touches(this Shape shape, Shape other)
    {
        return SpatialOperations.Touches(shape, other);
    }

    /// <summary>
    /// Tests if this shape crosses another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>True if the shapes cross</returns>
    public static bool Crosses(this Shape shape, Shape other)
    {
        return SpatialOperations.Crosses(shape, other);
    }

    /// <summary>
    /// Calculates the minimum distance to another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>The minimum distance between the shapes</returns>
    public static double DistanceTo(this Shape shape, Shape other)
    {
        return SpatialOperations.Distance(shape, other);
    }

    /// <summary>
    /// Gets the spatial relationship to another shape
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="other">The other shape</param>
    /// <returns>The spatial relationship</returns>
    public static SpatialRelationship GetRelationshipTo(this Shape shape, Shape other)
    {
        return SpatialOperations.GetRelationship(shape, other);
    }

    /// <summary>
    /// Tests if this shape contains a specific coordinate
    /// </summary>
    /// <param name="shape">This shape</param>
    /// <param name="coordinate">The coordinate to test</param>
    /// <returns>True if this shape contains the coordinate</returns>
    public static bool Contains(this Shape shape, Coordinate coordinate)
    {
        var point = new Point(coordinate);
        return Contains(shape, point);
    }
}
