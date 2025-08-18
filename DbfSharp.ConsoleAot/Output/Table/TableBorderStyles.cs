namespace DbfSharp.ConsoleAot.Output.Table;

/// <summary>
/// Provides predefined table border styles.
/// </summary>
public static class TableBorderStyles
{
    /// <summary>
    /// A rounded border style using box-drawing characters, similar to Spectre.Console.
    /// </summary>
    public static TableBorderStyle Rounded { get; } =
        new()
        {
            TopLeft = '╭',
            TopRight = '╮',
            BottomLeft = '╰',
            BottomRight = '╯',
            Horizontal = '─',
            Vertical = '│',
            LeftT = '├',
            RightT = '┤',
            TopT = '┬',
            BottomT = '┴',
            Cross = '┼',
        };

    /// <summary>
    /// A simple border style using standard ASCII characters.
    /// </summary>
    public static TableBorderStyle Ascii { get; } =
        new()
        {
            TopLeft = '+',
            TopRight = '+',
            BottomLeft = '+',
            BottomRight = '+',
            Horizontal = '-',
            Vertical = '|',
            LeftT = '+',
            RightT = '+',
            TopT = '+',
            BottomT = '+',
            Cross = '+',
        };
}
