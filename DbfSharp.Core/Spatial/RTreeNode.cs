using DbfSharp.Core.Geometry;

namespace DbfSharp.Core.Spatial;

/// <summary>
/// Represents a node in an R-tree spatial index
/// </summary>
internal class RTreeNode
{
    /// <summary>
    /// The bounding box that encompasses all child nodes or entries
    /// </summary>
    public BoundingBox BoundingBox { get; private set; }

    /// <summary>
    /// Child nodes (for internal nodes)
    /// </summary>
    public List<RTreeNode> Children { get; }

    /// <summary>
    /// Leaf entries (for leaf nodes)
    /// </summary>
    public List<RTreeEntry> Entries { get; }

    /// <summary>
    /// Gets a value indicating whether this is a leaf node
    /// </summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>
    /// Gets the number of children or entries in this node
    /// </summary>
    public int Count => IsLeaf ? Entries.Count : Children.Count;

    /// <summary>
    /// The parent node (null for root)
    /// </summary>
    public RTreeNode? Parent { get; set; }

    /// <summary>
    /// Initializes a new R-tree node
    /// </summary>
    public RTreeNode()
    {
        Children = [];
        Entries = [];
        BoundingBox = new BoundingBox(
            double.MaxValue,
            double.MaxValue,
            double.MinValue,
            double.MinValue
        );
    }

    /// <summary>
    /// Adds a child node to this internal node
    /// </summary>
    /// <param name="child">The child node to add</param>
    public void AddChild(RTreeNode child)
    {
        if (Entries.Count > 0)
            throw new InvalidOperationException("Cannot add child to node that has entries");

        Children.Add(child);
        child.Parent = this;
        UpdateBoundingBox();
    }

    /// <summary>
    /// Adds an entry to this leaf node
    /// </summary>
    /// <param name="entry">The entry to add</param>
    public void AddEntry(RTreeEntry entry)
    {
        if (Children.Count > 0)
            throw new InvalidOperationException("Cannot add entry to node that has children");

        Entries.Add(entry);
        UpdateBoundingBox();
    }

    /// <summary>
    /// Removes a child node from this internal node
    /// </summary>
    /// <param name="child">The child node to remove</param>
    public bool RemoveChild(RTreeNode child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
            UpdateBoundingBox();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes an entry from this leaf node
    /// </summary>
    /// <param name="entry">The entry to remove</param>
    public bool RemoveEntry(RTreeEntry entry)
    {
        if (Entries.Remove(entry))
        {
            UpdateBoundingBox();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates the bounding box to encompass all children or entries
    /// </summary>
    public void UpdateBoundingBox()
    {
        if (IsLeaf)
        {
            if (Entries.Count == 0)
            {
                BoundingBox = new BoundingBox(
                    double.MaxValue,
                    double.MaxValue,
                    double.MinValue,
                    double.MinValue
                );
                return;
            }

            var first = Entries[0].BoundingBox;
            var minX = first.MinX;
            var minY = first.MinY;
            var maxX = first.MaxX;
            var maxY = first.MaxY;

            foreach (var entry in Entries.Skip(1))
            {
                var bbox = entry.BoundingBox;
                minX = Math.Min(minX, bbox.MinX);
                minY = Math.Min(minY, bbox.MinY);
                maxX = Math.Max(maxX, bbox.MaxX);
                maxY = Math.Max(maxY, bbox.MaxY);
            }

            BoundingBox = new BoundingBox(minX, minY, maxX, maxY);
        }
        else
        {
            if (Children.Count == 0)
            {
                BoundingBox = new BoundingBox(
                    double.MaxValue,
                    double.MaxValue,
                    double.MinValue,
                    double.MinValue
                );
                return;
            }

            var first = Children[0].BoundingBox;
            var minX = first.MinX;
            var minY = first.MinY;
            var maxX = first.MaxX;
            var maxY = first.MaxY;

            foreach (var child in Children.Skip(1))
            {
                var bbox = child.BoundingBox;
                minX = Math.Min(minX, bbox.MinX);
                minY = Math.Min(minY, bbox.MinY);
                maxX = Math.Max(maxX, bbox.MaxX);
                maxY = Math.Max(maxY, bbox.MaxY);
            }

            BoundingBox = new BoundingBox(minX, minY, maxX, maxY);
        }
    }

    /// <summary>
    /// Calculates the area increase required to include the specified bounding box
    /// </summary>
    /// <param name="bbox">The bounding box to include</param>
    /// <returns>The area increase required</returns>
    public double CalculateAreaIncrease(BoundingBox bbox)
    {
        var union = BoundingBox.Union(bbox);
        return union.Area - BoundingBox.Area;
    }

    /// <summary>
    /// Finds all entries that intersect with the specified bounding box
    /// </summary>
    /// <param name="searchBox">The bounding box to search within</param>
    /// <param name="results">The list to add matching entries to</param>
    public void Search(BoundingBox searchBox, List<RTreeEntry> results)
    {
        if (!BoundingBox.Intersects(searchBox))
            return;

        if (IsLeaf)
        {
            foreach (var entry in Entries)
            {
                if (entry.BoundingBox.Intersects(searchBox))
                {
                    results.Add(entry);
                }
            }
        }
        else
        {
            foreach (var child in Children)
            {
                child.Search(searchBox, results);
            }
        }
    }

    /// <summary>
    /// Gets the depth of this node in the tree (0 for leaf)
    /// </summary>
    public int GetDepth()
    {
        if (IsLeaf)
            return 0;

        return Children.Max(child => child.GetDepth()) + 1;
    }

    public override string ToString()
    {
        var type = IsLeaf ? "Leaf" : "Internal";
        var count = IsLeaf ? Entries.Count : Children.Count;
        return $"{type} Node: {count} items, BBox: {BoundingBox}";
    }
}
