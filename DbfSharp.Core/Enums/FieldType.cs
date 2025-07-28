namespace DbfSharp.Core.Enums;

/// <summary>
/// Represents the different field types supported in DBF files
/// </summary>
public enum FieldType : byte
{
    /// <summary>Character string (C)</summary>
    Character = (byte)'C',

    /// <summary>Date (D) - stored as YYYYMMDD</summary>
    Date = (byte)'D',

    /// <summary>Floating point number (F)</summary>
    Float = (byte)'F',

    /// <summary>General/OLE field (G) - stored in memo file</summary>
    General = (byte)'G',

    /// <summary>Integer (I) - 32-bit signed integer</summary>
    Integer = (byte)'I',

    /// <summary>Logical/Boolean (L) - T/F/Y/N/?</summary>
    Logical = (byte)'L',

    /// <summary>Memo (M) - variable length text stored in memo file</summary>
    Memo = (byte)'M',

    /// <summary>Numeric (N) - can be integer or floating point</summary>
    Numeric = (byte)'N',

    /// <summary>Double precision floating point (O)</summary>
    Double = (byte)'O',

    /// <summary>Picture/Binary (P) - binary data stored in memo file</summary>
    Picture = (byte)'P',

    /// <summary>Timestamp (T) - date and time</summary>
    Timestamp = (byte)'T',

    /// <summary>Currency (Y) - 64-bit integer with 4 decimal places</summary>
    Currency = (byte)'Y',

    /// <summary>Binary (B) - can be memo index or double precision depending on version</summary>
    Binary = (byte)'B',

    /// <summary>Varchar (V) - variable length character field (Visual FoxPro)</summary>
    Varchar = (byte)'V',

    /// <summary>Autoincrement field (+) - incrementing integer</summary>
    Autoincrement = (byte)'+',

    /// <summary>Timestamp field (@) - alternative timestamp representation</summary>
    TimestampAlternate = (byte)'@',

    /// <summary>Flags field (0) - binary flags</summary>
    Flags = (byte)'0',
}

/// <summary>
/// Extension methods for FieldType enum
/// </summary>
public static class FieldTypeExtensions
{
    /// <summary>
    /// Gets a human-readable description of the field type
    /// </summary>
    /// <param name="fieldType">The field type</param>
    /// <returns>A descriptive string</returns>
    public static string GetDescription(this FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Character => "Character",
            FieldType.Date => "Date",
            FieldType.Float => "Float",
            FieldType.General => "General/OLE",
            FieldType.Integer => "Integer",
            FieldType.Logical => "Logical",
            FieldType.Memo => "Memo",
            FieldType.Numeric => "Numeric",
            FieldType.Double => "Double",
            FieldType.Picture => "Picture",
            FieldType.Timestamp => "Timestamp",
            FieldType.Currency => "Currency",
            FieldType.Binary => "Binary",
            FieldType.Varchar => "Varchar",
            FieldType.Autoincrement => "Autoincrement",
            FieldType.TimestampAlternate => "Timestamp (Alt)",
            FieldType.Flags => "Flags",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// Determines if this field type uses memo files for data storage
    /// </summary>
    /// <param name="fieldType">The field type</param>
    /// <returns>True if the field type uses memo files</returns>
    public static bool UsesMemoFile(this FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Memo => true,
            FieldType.General => true,
            FieldType.Picture => true,
            FieldType.Binary => true, //C an use memo file depending on version
            _ => false,
        };
    }

    /// <summary>
    /// Gets the expected .NET type for this field type
    /// </summary>
    /// <param name="fieldType">The field type</param>
    /// <returns>The corresponding .NET type</returns>
    public static Type GetExpectedNetType(this FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Character => typeof(string),
            FieldType.Date => typeof(DateTime?),
            FieldType.Float => typeof(float?),
            FieldType.General => typeof(byte[]),
            FieldType.Integer => typeof(int),
            FieldType.Logical => typeof(bool?),
            FieldType.Memo => typeof(string),
            FieldType.Numeric => typeof(decimal?),
            FieldType.Double => typeof(double),
            FieldType.Picture => typeof(byte[]),
            FieldType.Timestamp => typeof(DateTime?),
            FieldType.Currency => typeof(decimal),
            FieldType.Binary => typeof(object), // Can be double or byte[] depending on version
            FieldType.Varchar => typeof(string),
            FieldType.Autoincrement => typeof(int),
            FieldType.TimestampAlternate => typeof(DateTime?),
            FieldType.Flags => typeof(byte[]),
            _ => typeof(object),
        };
    }

    /// <summary>
    /// Determines if this field type supports null values
    /// </summary>
    /// <param name="fieldType">The field type</param>
    /// <returns>True if the field type can contain null values</returns>
    public static bool SupportsNull(this FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Integer => false,
            FieldType.Currency => false,
            FieldType.Double => false,
            FieldType.Autoincrement => false,
            _ => true,
        };
    }

    /// <summary>
    /// Converts a raw byte value to a FieldType enum value
    /// </summary>
    /// <param name="value">The raw byte value from the DBF field definition</param>
    /// <returns>The corresponding FieldType, or null if not recognized</returns>
    public static FieldType? FromByte(byte value)
    {
        if (Enum.IsDefined(typeof(FieldType), value))
        {
            return (FieldType)value;
        }
        return null;
    }

    /// <summary>
    /// Converts a character to a FieldType enum value
    /// </summary>
    /// <param name="c">The character representing the field type</param>
    /// <returns>The corresponding FieldType, or null if not recognized</returns>
    public static FieldType? FromChar(char c)
    {
        return FromByte((byte)c);
    }
}
