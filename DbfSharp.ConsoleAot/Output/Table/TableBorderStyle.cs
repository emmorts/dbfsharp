namespace DbfSharp.ConsoleAot.Output.Table;

/// <summary>
/// Defines the characters used to draw a table border.
/// </summary>
public record TableBorderStyle
{
    public char TopLeft { get; init; }
    public char TopRight { get; init; }
    public char BottomLeft { get; init; }
    public char BottomRight { get; init; }
    public char Horizontal { get; init; }
    public char Vertical { get; init; }
    public char LeftT { get; init; }
    public char RightT { get; init; }
    public char TopT { get; init; }
    public char BottomT { get; init; }
    public char Cross { get; init; }
}
