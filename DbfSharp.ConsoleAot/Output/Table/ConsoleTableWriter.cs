namespace DbfSharp.ConsoleAot.Output.Table;

/// <summary>
/// A utility class to print nicely formatted plain text tables to the console,
/// inspired by Spectre.Console but AOT-compatible.
/// </summary>
public class ConsoleTableWriter
{
    private readonly List<string[]> _rows = [];
    private readonly string[] _headers;
    private readonly string? _title;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleTableWriter"/> class.
    /// </summary>
    /// <param name="title">The title of the table, displayed above it.</param>
    /// <param name="headers">The column headers for the table.</param>
    public ConsoleTableWriter(string? title, params string[] headers)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        _title = title;
    }

    /// <summary>
    /// Adds a row of data to the table.
    /// </summary>
    /// <param name="row">The cells for the row. The number of cells must match the number of headers.</param>
    public void AddRow(params string[] row)
    {
        if (row.Length != _headers.Length)
        {
            throw new ArgumentException("Row cell count must match header count.");
        }
        _rows.Add(row);
    }

    /// <summary>
    /// Prints the complete table to the console using the specified border style.
    /// </summary>
    /// <param name="style">The border style to use for drawing.</param>
    public void Print(TableBorderStyle style)
    {
        if (_headers.Length == 0 && _rows.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_title))
        {
            Console.WriteLine(_title);
        }

        var columnWidths = CalculateColumnWidths();

        // Top border
        PrintLine(style.TopLeft, style.TopT, style.TopRight, style.Horizontal, columnWidths);

        // Header
        if (_headers.Length > 0 && !string.IsNullOrWhiteSpace(_headers[0]))
        {
            PrintRow(_headers, style.Vertical, columnWidths);
            // Header separator
            PrintLine(style.LeftT, style.Cross, style.RightT, style.Horizontal, columnWidths);
        }

        // Rows
        foreach (var row in _rows)
        {
            PrintRow(row, style.Vertical, columnWidths);
        }

        // Bottom border
        PrintLine(style.BottomLeft, style.BottomT, style.BottomRight, style.Horizontal, columnWidths);
    }

    private int[] CalculateColumnWidths()
    {
        var columnWidths = new int[_headers.Length];
        for (var i = 0; i < _headers.Length; i++)
        {
            columnWidths[i] = _headers[i].Length;
        }

        foreach (var row in _rows)
        {
            for (var i = 0; i < row.Length; i++)
            {
                if (row[i].Length > columnWidths[i])
                {
                    columnWidths[i] = row[i].Length;
                }
            }
        }
        return columnWidths;
    }

    private static void PrintLine(char left, char mid, char right, char horizontal, IReadOnlyList<int> widths)
    {
        Console.Write(left);
        for (var i = 0; i < widths.Count; i++)
        {
            Console.Write(new string(horizontal, widths[i] + 2)); // +2 for padding
            if (i < widths.Count - 1)
            {
                Console.Write(mid);
            }
        }
        Console.WriteLine(right);
    }

    private static void PrintRow(string[] row, char vertical, int[] widths)
    {
        Console.Write(vertical);
        for (var i = 0; i < row.Length; i++)
        {
            var cell = row[i] ?? "";
            Console.Write($" {cell.PadRight(widths[i])} ");
            Console.Write(vertical);
        }
        Console.WriteLine();
    }
}
