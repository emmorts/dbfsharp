using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Spatial;

/// <summary>
/// R-tree spatial index for efficient spatial queries on shapefile geometries
/// </summary>
public class SpatialIndex
{
    private const int DefaultMaxEntries = 16;
    private const int DefaultMinEntries = 4;

    private readonly int _maxEntries;
    private readonly int _minEntries;
    private RTreeNode? _root;
    private int _count;

    /// <summary>
    /// Gets the number of entries in the spatial index
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether the index is empty
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Gets the bounding box that encompasses all entries in the index
    /// </summary>
    public BoundingBox? BoundingBox => _root?.BoundingBox;

    /// <summary>
    /// Initializes a new spatial index with default parameters
    /// </summary>
    public SpatialIndex() : this(DefaultMaxEntries, DefaultMinEntries) { }

    /// <summary>
    /// Initializes a new spatial index with custom parameters
    /// </summary>
    /// <param name="maxEntries">Maximum entries per node</param>
    /// <param name="minEntries">Minimum entries per node</param>
    public SpatialIndex(int maxEntries, int minEntries)
    {
        if (maxEntries < 2)
            throw new ArgumentException("Max entries must be at least 2", nameof(maxEntries));
        if (minEntries < 1)
            throw new ArgumentException("Min entries must be at least 1", nameof(minEntries));
        if (minEntries > maxEntries / 2)
            throw new ArgumentException("Min entries cannot exceed half of max entries", nameof(minEntries));

        _maxEntries = maxEntries;
        _minEntries = minEntries;
    }

    /// <summary>
    /// Inserts a new entry into the spatial index
    /// </summary>
    /// <param name="entry">The entry to insert</param>
    public void Insert(RTreeEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        if (_root == null)
        {
            _root = new RTreeNode();
        }

        InsertEntry(_root, entry);
        _count++;
    }

    /// <summary>
    /// Inserts a shape into the spatial index
    /// </summary>
    /// <param name="shape">The shape to insert</param>
    /// <param name="recordNumber">The record number</param>
    /// <param name="userData">Optional user data</param>
    public void Insert(Shape shape, int recordNumber, object? userData = null)
    {
        var entry = RTreeEntry.FromShape(shape, recordNumber, userData);
        Insert(entry);
    }

    /// <summary>
    /// Searches for all entries that intersect with the specified bounding box
    /// </summary>
    /// <param name="searchBox">The bounding box to search within</param>
    /// <returns>All entries that intersect with the search box</returns>
    public List<RTreeEntry> Search(BoundingBox searchBox)
    {
        var results = new List<RTreeEntry>();
        _root?.Search(searchBox, results);
        return results;
    }

    /// <summary>
    /// Searches for entries that contain the specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to search for</param>
    /// <returns>All entries that contain the coordinate</returns>
    public List<RTreeEntry> Search(Coordinate coordinate)
    {
        // Create a point bounding box
        var searchBox = new BoundingBox(coordinate.X, coordinate.Y, coordinate.X, coordinate.Y);
        return Search(searchBox);
    }

    /// <summary>
    /// Finds the nearest entries to a specified coordinate
    /// </summary>
    /// <param name="coordinate">The coordinate to search near</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>The nearest entries sorted by distance</returns>
    public List<RTreeEntry> FindNearest(Coordinate coordinate, int maxResults = 10)
    {
        if (_root == null || maxResults <= 0)
            return [];

        var candidates = new List<(RTreeEntry Entry, double Distance)>();
        CollectNearestCandidates(_root, coordinate, candidates);

        return candidates
            .OrderBy(c => c.Distance)
            .Take(maxResults)
            .Select(c => c.Entry)
            .ToList();
    }

    /// <summary>
    /// Removes all entries from the spatial index
    /// </summary>
    public void Clear()
    {
        _root = null;
        _count = 0;
    }

    /// <summary>
    /// Gets statistics about the spatial index structure
    /// </summary>
    /// <returns>Index statistics</returns>
    public SpatialIndexStatistics GetStatistics()
    {
        if (_root == null)
            return new SpatialIndexStatistics(0, 0, 0, 0, 0);

        var leafCount = 0;
        var internalCount = 0;
        var maxDepth = 0;
        var totalEntries = 0;

        CountNodes(_root, 0, ref leafCount, ref internalCount, ref maxDepth, ref totalEntries);

        var avgEntriesPerLeaf = leafCount > 0 ? (double)totalEntries / leafCount : 0;

        return new SpatialIndexStatistics(
            leafCount,
            internalCount,
            maxDepth,
            totalEntries,
            avgEntriesPerLeaf
        );
    }

    private void InsertEntry(RTreeNode node, RTreeEntry entry)
    {
        if (node.IsLeaf)
        {
            node.AddEntry(entry);
            if (node.Count > _maxEntries)
            {
                SplitNode(node);
            }
        }
        else
        {
            // Find the child node that requires least enlargement
            var bestChild = node.Children.MinBy(child => child.CalculateAreaIncrease(entry.BoundingBox));
            InsertEntry(bestChild!, entry);
        }
    }

    private void SplitNode(RTreeNode node)
    {
        // Create a new node for the split
        var newNode = new RTreeNode();

        if (node.IsLeaf)
        {
            // Split leaf entries
            var entries = node.Entries.ToList();
            var (group1, group2) = SplitEntries(entries);

            // Clear original node and add first group
            node.Entries.Clear();
            foreach (var entry in group1)
            {
                node.AddEntry(entry);
            }

            // Add second group to new node
            foreach (var entry in group2)
            {
                newNode.AddEntry(entry);
            }
        }
        else
        {
            // Split child nodes
            var children = node.Children.ToList();
            var childEntries = children.Select(child => 
                new RTreeEntry(child.BoundingBox, -1, null, child)).ToList();
            var (group1, group2) = SplitEntries(childEntries);

            // Clear original node and add first group
            node.Children.Clear();
            foreach (var entry in group1)
            {
                var child = (RTreeNode)entry.UserData!;
                node.AddChild(child);
            }

            // Add second group to new node
            foreach (var entry in group2)
            {
                var child = (RTreeNode)entry.UserData!;
                newNode.AddChild(child);
            }
        }

        // Handle root split
        if (node.Parent == null)
        {
            var newRoot = new RTreeNode();
            newRoot.AddChild(node);
            newRoot.AddChild(newNode);
            _root = newRoot;
        }
        else
        {
            node.Parent.AddChild(newNode);
            if (node.Parent.Count > _maxEntries)
            {
                SplitNode(node.Parent);
            }
        }
    }

    private static (List<RTreeEntry> group1, List<RTreeEntry> group2) SplitEntries(List<RTreeEntry> entries)
    {
        // Quadratic split algorithm
        var bestPair = FindWorstPair(entries);
        var seed1 = bestPair.entry1;
        var seed2 = bestPair.entry2;

        var group1 = new List<RTreeEntry> { seed1 };
        var group2 = new List<RTreeEntry> { seed2 };
        var remaining = entries.Where(e => e != seed1 && e != seed2).ToList();

        while (remaining.Count > 0)
        {
            var bbox1 = GetBoundingBox(group1);
            var bbox2 = GetBoundingBox(group2);

            var bestEntry = remaining.MaxBy(entry =>
            {
                var increase1 = bbox1.Union(entry.BoundingBox).Area - bbox1.Area;
                var increase2 = bbox2.Union(entry.BoundingBox).Area - bbox2.Area;
                return Math.Abs(increase1 - increase2);
            });

            remaining.Remove(bestEntry!);

            var increase1 = bbox1.Union(bestEntry!.BoundingBox).Area - bbox1.Area;
            var increase2 = bbox2.Union(bestEntry!.BoundingBox).Area - bbox2.Area;

            if (increase1 < increase2)
            {
                group1.Add(bestEntry!);
            }
            else
            {
                group2.Add(bestEntry!);
            }
        }

        return (group1, group2);
    }

    private static (RTreeEntry entry1, RTreeEntry entry2) FindWorstPair(List<RTreeEntry> entries)
    {
        var maxWaste = double.MinValue;
        var worstPair = (entries[0], entries[1]);

        for (int i = 0; i < entries.Count - 1; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                var entry1 = entries[i];
                var entry2 = entries[j];
                var union = entry1.BoundingBox.Union(entry2.BoundingBox);
                var waste = union.Area - entry1.BoundingBox.Area - entry2.BoundingBox.Area;

                if (waste > maxWaste)
                {
                    maxWaste = waste;
                    worstPair = (entry1, entry2);
                }
            }
        }

        return worstPair;
    }

    private static BoundingBox GetBoundingBox(List<RTreeEntry> entries)
    {
        if (entries.Count == 0)
            throw new ArgumentException("Cannot get bounding box of empty entries");

        var first = entries[0].BoundingBox;
        var minX = first.MinX;
        var minY = first.MinY;
        var maxX = first.MaxX;
        var maxY = first.MaxY;

        foreach (var entry in entries.Skip(1))
        {
            var bbox = entry.BoundingBox;
            minX = Math.Min(minX, bbox.MinX);
            minY = Math.Min(minY, bbox.MinY);
            maxX = Math.Max(maxX, bbox.MaxX);
            maxY = Math.Max(maxY, bbox.MaxY);
        }

        return new BoundingBox(minX, minY, maxX, maxY);
    }

    private void CollectNearestCandidates(RTreeNode node, Coordinate coordinate, List<(RTreeEntry Entry, double Distance)> candidates)
    {
        if (node.IsLeaf)
        {
            foreach (var entry in node.Entries)
            {
                var distance = CalculateMinDistance(coordinate, entry.BoundingBox);
                candidates.Add((entry, distance));
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                CollectNearestCandidates(child, coordinate, candidates);
            }
        }
    }

    private static double CalculateMinDistance(Coordinate point, BoundingBox box)
    {
        var dx = Math.Max(0, Math.Max(box.MinX - point.X, point.X - box.MaxX));
        var dy = Math.Max(0, Math.Max(box.MinY - point.Y, point.Y - box.MaxY));
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static void CountNodes(RTreeNode node, int depth, ref int leafCount, ref int internalCount, ref int maxDepth, ref int totalEntries)
    {
        maxDepth = Math.Max(maxDepth, depth);

        if (node.IsLeaf)
        {
            leafCount++;
            totalEntries += node.Count;
        }
        else
        {
            internalCount++;
            foreach (var child in node.Children)
            {
                CountNodes(child, depth + 1, ref leafCount, ref internalCount, ref maxDepth, ref totalEntries);
            }
        }
    }

    public override string ToString()
    {
        var stats = GetStatistics();
        return $"SpatialIndex: {Count} entries, {stats.LeafCount} leaves, {stats.InternalCount} internal nodes, depth {stats.MaxDepth}";
    }
}

/// <summary>
/// Statistics about a spatial index structure
/// </summary>
public readonly struct SpatialIndexStatistics
{
    public int LeafCount { get; }
    public int InternalCount { get; }
    public int MaxDepth { get; }
    public int TotalEntries { get; }
    public double AverageEntriesPerLeaf { get; }

    public SpatialIndexStatistics(int leafCount, int internalCount, int maxDepth, int totalEntries, double averageEntriesPerLeaf)
    {
        LeafCount = leafCount;
        InternalCount = internalCount;
        MaxDepth = maxDepth;
        TotalEntries = totalEntries;
        AverageEntriesPerLeaf = averageEntriesPerLeaf;
    }

    public override string ToString()
    {
        return $"Leaves: {LeafCount}, Internal: {InternalCount}, Depth: {MaxDepth}, " +
               $"Entries: {TotalEntries}, Avg/Leaf: {AverageEntriesPerLeaf:F1}";
    }
}
