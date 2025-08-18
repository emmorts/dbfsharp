using System.Text;

namespace DbfSharp.ConsoleAot.Output.Table;

/// <summary>
/// High-performance table renderer with optimized string building and direct console output
/// </summary>
public sealed class TableRenderer
{
    private readonly string[] _headers;
    private readonly int[] _columnWidths;
    private readonly TableBorderStyle _style = TableBorderStyles.Rounded;
    private readonly int _totalWidth;

    public TableRenderer(string[] headers, int[] columnWidths)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        _columnWidths = columnWidths ?? throw new ArgumentNullException(nameof(columnWidths));

        if (headers.Length != columnWidths.Length)
        {
            throw new ArgumentException("Headers and column widths must have the same length");
        }

        _totalWidth = columnWidths.Sum() + columnWidths.Length * 3 + 1; // +3 for padding and borders, +1 for final border
    }

    /// <summary>
    /// Renders the complete table to a StringBuilder for better performance
    /// </summary>
    public void RenderToStringBuilder(List<string[]> records, StringBuilder output)
    {
        // Pre-allocate capacity based on estimated output size
        var estimatedSize = (_totalWidth + 2) * (records.Count + 4); // +4 for header, separators, etc.
        if (output.Capacity < estimatedSize)
        {
            output.EnsureCapacity(estimatedSize);
        }

        // Top border
        AppendBorderLine(output, _style.TopLeft, _style.TopT, _style.TopRight, _style.Horizontal);

        // Header
        if (_headers.Length > 0)
        {
            AppendDataRow(output, _headers, _style.Vertical);
            AppendBorderLine(output, _style.LeftT, _style.Cross, _style.RightT, _style.Horizontal);
        }

        // Data rows
        foreach (var row in records)
        {
            AppendDataRow(output, row, _style.Vertical);
        }

        // Bottom border
        AppendBorderLine(
            output,
            _style.BottomLeft,
            _style.BottomT,
            _style.BottomRight,
            _style.Horizontal
        );
    }

    /// <summary>
    /// Renders directly to console for optimal performance when writing to stdout
    /// </summary>
    public void RenderToConsole(List<string[]> records)
    {
        // Top border
        PrintBorderLine(_style.TopLeft, _style.TopT, _style.TopRight, _style.Horizontal);

        // Header
        if (_headers.Length > 0)
        {
            PrintDataRow(_headers, _style.Vertical);
            PrintBorderLine(_style.LeftT, _style.Cross, _style.RightT, _style.Horizontal);
        }

        // Data rows
        foreach (var row in records)
        {
            PrintDataRow(row, _style.Vertical);
        }

        // Bottom border
        PrintBorderLine(_style.BottomLeft, _style.BottomT, _style.BottomRight, _style.Horizontal);
    }

    /// <summary>
    /// Appends a border line to the output with optimal string building
    /// </summary>
    private void AppendBorderLine(
        StringBuilder output,
        char left,
        char mid,
        char right,
        char horizontal
    )
    {
        output.Append(left);

        for (var i = 0; i < _columnWidths.Length; i++)
        {
            output.Append(horizontal, _columnWidths[i] + 2); // +2 for padding
            if (i < _columnWidths.Length - 1)
            {
                output.Append(mid);
            }
        }

        output.AppendLine(right.ToString());
    }

    /// <summary>
    /// Appends a data row to the output with efficient padding
    /// </summary>
    private void AppendDataRow(StringBuilder output, string[] row, char vertical)
    {
        output.Append(vertical);

        for (var i = 0; i < _columnWidths.Length; i++)
        {
            var cell = i < row.Length ? row[i] ?? "" : "";
            output.Append(' ');
            output.Append(cell.PadRight(_columnWidths[i]));
            output.Append(' ');
            output.Append(vertical);
        }

        output.AppendLine();
    }

    /// <summary>
    /// Prints a border line directly to console for optimal performance
    /// </summary>
    private void PrintBorderLine(char left, char mid, char right, char horizontal)
    {
        Console.Write(left);
        for (var i = 0; i < _columnWidths.Length; i++)
        {
            Console.Write(new string(horizontal, _columnWidths[i] + 2)); // +2 for padding
            if (i < _columnWidths.Length - 1)
            {
                Console.Write(mid);
            }
        }
        Console.WriteLine(right);
    }

    /// <summary>
    /// Prints a data row directly to console for optimal performance
    /// </summary>
    private void PrintDataRow(string[] row, char vertical)
    {
        Console.Write(vertical);
        for (var i = 0; i < _columnWidths.Length; i++)
        {
            var cell = i < row.Length ? row[i] ?? "" : "";
            Console.Write($" {cell.PadRight(_columnWidths[i])} ");
            Console.Write(vertical);
        }
        Console.WriteLine();
    }
}
