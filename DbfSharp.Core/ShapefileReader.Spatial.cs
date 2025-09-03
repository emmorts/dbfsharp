using DbfSharp.Core.Geometry;
using DbfSharp.Core.Spatial;

namespace DbfSharp.Core;

/// <summary>
/// Spatial indexing and query extensions for ShapefileReader
/// </summary>
public partial class ShapefileReader
{
    private SpatialIndex? _spatialIndex;
    private bool _spatialIndexBuilt;

    /// <summary>
    /// Gets a value indicating whether a spatial index has been built for this reader
    /// </summary>
    public bool HasSpatialIndex => _spatialIndex != null && _spatialIndexBuilt;

    /// <summary>
    /// Gets the spatial index, or null if not built
    /// </summary>
    public SpatialIndex? SpatialIndex => _spatialIndex;

    /// <summary>
    /// Builds a spatial index for efficient spatial queries
    /// </summary>
    /// <param name="maxEntries">Maximum entries per R-tree node</param>
    /// <param name="minEntries">Minimum entries per R-tree node</param>
    /// <param name="loadGeometry">Whether to load full geometry into index entries</param>
    /// <returns>Statistics about the built index</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public SpatialIndexStatistics BuildSpatialIndex(int maxEntries = 16, int minEntries = 4, bool loadGeometry = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));

        _spatialIndex = new SpatialIndex(maxEntries, minEntries);
        _spatialIndexBuilt = false;

        // Build index from all records
        var recordCount = 0;
        foreach (var record in Records)
        {
            if (!record.Geometry.IsEmpty)
            {
                var entry = new RTreeEntry(
                    record.Geometry.BoundingBox,
                    record.RecordNumber,
                    loadGeometry ? record.Geometry : null
                );
                
                _spatialIndex.Insert(entry);
                recordCount++;
            }
        }

        _spatialIndexBuilt = true;
        return _spatialIndex.GetStatistics();
    }

    /// <summary>
    /// Builds a spatial index asynchronously for efficient spatial queries
    /// </summary>
    /// <param name="maxEntries">Maximum entries per R-tree node</param>
    /// <param name="minEntries">Minimum entries per R-tree node</param>
    /// <param name="loadGeometry">Whether to load full geometry into index entries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task with statistics about the built index</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public async Task<SpatialIndexStatistics> BuildSpatialIndexAsync(
        int maxEntries = 16, 
        int minEntries = 4, 
        bool loadGeometry = false,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => BuildSpatialIndex(maxEntries, minEntries, loadGeometry), cancellationToken);
    }

    /// <summary>
    /// Searches for records that intersect with the specified bounding box
    /// </summary>
    /// <param name="boundingBox">The bounding box to search within</param>
    /// <param name="loadGeometry">Whether to load full geometry for results</param>
    /// <returns>Records that intersect with the bounding box</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileRecord> SearchRecords(BoundingBox boundingBox, bool loadGeometry = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));
        
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index must be built before performing spatial queries. Call BuildSpatialIndex() first.");
        }

        var entries = _spatialIndex!.Search(boundingBox);
        
        foreach (var entry in entries)
        {
            if (loadGeometry || entry.Shape == null)
            {
                // Load the actual record to get full geometry
                yield return GetRecord(entry.RecordNumber);
            }
            else
            {
                // Use cached geometry from index
                yield return new ShapefileRecord(entry.RecordNumber, entry.Shape, 0, 0);
            }
        }
    }

    /// <summary>
    /// Searches for features that intersect with the specified bounding box
    /// </summary>
    /// <param name="boundingBox">The bounding box to search within</param>
    /// <param name="loadGeometry">Whether to load full geometry for results</param>
    /// <returns>Features that intersect with the bounding box</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileFeature> SearchFeatures(BoundingBox boundingBox, bool loadGeometry = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));
        
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index must be built before performing spatial queries. Call BuildSpatialIndex() first.");
        }

        var entries = _spatialIndex!.Search(boundingBox);
        
        foreach (var entry in entries)
        {
            yield return GetFeature(entry.RecordNumber);
        }
    }

    /// <summary>
    /// Searches for records that contain the specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to search for</param>
    /// <returns>Records that contain the coordinate</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileRecord> SearchRecords(Coordinate coordinate)
    {
        var pointBox = new BoundingBox(coordinate.X, coordinate.Y, coordinate.X, coordinate.Y);
        return SearchRecords(pointBox).Where(record => record.Geometry.Contains(coordinate));
    }

    /// <summary>
    /// Searches for features that contain the specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to search for</param>
    /// <returns>Features that contain the coordinate</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileFeature> SearchFeatures(Coordinate coordinate)
    {
        var pointBox = new BoundingBox(coordinate.X, coordinate.Y, coordinate.X, coordinate.Y);
        return SearchFeatures(pointBox).Where(feature => feature.Geometry.Contains(coordinate));
    }

    /// <summary>
    /// Finds the nearest records to a specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to search near</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>The nearest records sorted by distance</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileRecord> FindNearestRecords(Coordinate coordinate, int maxResults = 10)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));
        
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index must be built before performing spatial queries. Call BuildSpatialIndex() first.");
        }

        var nearestEntries = _spatialIndex!.FindNearest(coordinate, maxResults);
        
        foreach (var entry in nearestEntries)
        {
            yield return GetRecord(entry.RecordNumber);
        }
    }

    /// <summary>
    /// Finds the nearest features to a specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to search near</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>The nearest features sorted by distance</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileFeature> FindNearestFeatures(Coordinate coordinate, int maxResults = 10)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));
        
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index must be built before performing spatial queries. Call BuildSpatialIndex() first.");
        }

        var nearestEntries = _spatialIndex!.FindNearest(coordinate, maxResults);
        
        foreach (var entry in nearestEntries)
        {
            yield return GetFeature(entry.RecordNumber);
        }
    }

    /// <summary>
    /// Performs a spatial query using a custom predicate
    /// </summary>
    /// <param name="searchArea">The bounding box to search within</param>
    /// <param name="predicate">Custom predicate to filter results</param>
    /// <returns>Records that match the predicate</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileRecord> SearchRecords(BoundingBox searchArea, Func<ShapefileRecord, bool> predicate)
    {
        return SearchRecords(searchArea).Where(predicate);
    }

    /// <summary>
    /// Performs a spatial query using a custom predicate
    /// </summary>
    /// <param name="searchArea">The bounding box to search within</param>
    /// <param name="predicate">Custom predicate to filter results</param>
    /// <returns>Features that match the predicate</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileFeature> SearchFeatures(BoundingBox searchArea, Func<ShapefileFeature, bool> predicate)
    {
        return SearchFeatures(searchArea).Where(predicate);
    }

    /// <summary>
    /// Finds records that have a specific spatial relationship with a given shape
    /// </summary>
    /// <param name="queryShape">The shape to compare against</param>
    /// <param name="relationship">The desired spatial relationship</param>
    /// <returns>Records that match the spatial relationship</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileRecord> FindRecordsByRelationship(Shape queryShape, SpatialRelationship relationship)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));
        
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index must be built before performing spatial queries. Call BuildSpatialIndex() first.");
        }

        // First get candidates that intersect with bounding box
        var candidates = SearchRecords(queryShape.BoundingBox);
        
        // Then filter by actual spatial relationship
        foreach (var record in candidates)
        {
            var actualRelationship = SpatialOperations.GetRelationship(queryShape, record.Geometry);
            if (actualRelationship == relationship)
            {
                yield return record;
            }
        }
    }

    /// <summary>
    /// Finds features that have a specific spatial relationship with a given shape
    /// </summary>
    /// <param name="queryShape">The shape to compare against</param>
    /// <param name="relationship">The desired spatial relationship</param>
    /// <returns>Features that match the spatial relationship</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public IEnumerable<ShapefileFeature> FindFeaturesByRelationship(Shape queryShape, SpatialRelationship relationship)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShapefileReader));
        
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index must be built before performing spatial queries. Call BuildSpatialIndex() first.");
        }

        // First get candidates that intersect with bounding box
        var candidates = SearchFeatures(queryShape.BoundingBox);
        
        // Then filter by actual spatial relationship
        foreach (var feature in candidates)
        {
            var actualRelationship = SpatialOperations.GetRelationship(queryShape, feature.Geometry);
            if (actualRelationship == relationship)
            {
                yield return feature;
            }
        }
    }

    /// <summary>
    /// Gets spatial index performance statistics
    /// </summary>
    /// <returns>Statistics about the spatial index structure</returns>
    /// <exception cref="InvalidOperationException">Thrown when spatial index is not built</exception>
    public SpatialIndexStatistics GetSpatialIndexStatistics()
    {
        if (!HasSpatialIndex)
        {
            throw new InvalidOperationException("Spatial index is not built");
        }

        return _spatialIndex!.GetStatistics();
    }

    /// <summary>
    /// Clears the spatial index to free memory
    /// </summary>
    public void ClearSpatialIndex()
    {
        _spatialIndex?.Clear();
        _spatialIndex = null;
        _spatialIndexBuilt = false;
    }

    /// <summary>
    /// Rebuilds the spatial index with new parameters
    /// </summary>
    /// <param name="maxEntries">Maximum entries per R-tree node</param>
    /// <param name="minEntries">Minimum entries per R-tree node</param>
    /// <param name="loadGeometry">Whether to load full geometry into index entries</param>
    /// <returns>Statistics about the rebuilt index</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed</exception>
    public SpatialIndexStatistics RebuildSpatialIndex(int maxEntries = 16, int minEntries = 4, bool loadGeometry = false)
    {
        ClearSpatialIndex();
        return BuildSpatialIndex(maxEntries, minEntries, loadGeometry);
    }
}
