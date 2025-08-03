namespace DbfSharp.Core.Enums;

/// <summary>
/// Represents the different DBF file format versions based on the version byte in the header.
/// Based on the mapping from http://www.dbf2002.com/dbf-file-format.html
/// </summary>
public enum DbfVersion : byte
{
    /// <summary>dBASE II / FoxBASE</summary>
    DBase2 = 0x02,

    /// <summary>FoxBASE+/dBase III plus, no memory</summary>
    DBase3Plus = 0x03,

    /// <summary>Visual FoxPro</summary>
    VisualFoxPro = 0x30,

    /// <summary>Visual FoxPro, autoincrement enabled</summary>
    VisualFoxProAutoIncrement = 0x31,

    /// <summary>Visual FoxPro with field type Varchar or Varbinary</summary>
    VisualFoxProVarchar = 0x32,

    /// <summary>dBASE IV SQL table files, no memo</summary>
    DBase4SqlTable = 0x43,

    /// <summary>dBASE IV SQL system files, no memo</summary>
    DBase4SqlSystem = 0x63,

    /// <summary>FoxBASE+/dBASE III PLUS, with memo</summary>
    DBase3PlusWithMemo = 0x83,

    /// <summary>dBASE IV with memo</summary>
    DBase4WithMemo = 0x8B,

    /// <summary>dBASE IV SQL table files, with memo</summary>
    DBase4SqlTableWithMemo = 0xCB,

    /// <summary>FoxPro 2.x (or earlier) with memo</summary>
    FoxPro2WithMemo = 0xF5,

    /// <summary>HiPer-Six format with SMT memo file</summary>
    HiPerSix = 0xE5,

    /// <summary>FoxBASE</summary>
    FoxBaseLegacy = 0xFB,

    /// <summary>Unknown version</summary>
    Unknown = 0xFF,
}

/// <summary>
/// Extension methods for DbfVersion enum
/// </summary>
public static class DbfVersionExtensions
{
    /// <summary>
    /// Gets a human-readable description of the DBF version
    /// </summary>
    /// <param name="version">The DBF version</param>
    /// <returns>A descriptive string</returns>
    public static string GetDescription(this DbfVersion version)
    {
        return version switch
        {
            DbfVersion.DBase2 => "dBASE II / FoxBASE",
            DbfVersion.DBase3Plus => "FoxBASE+/dBase III plus, no memory",
            DbfVersion.VisualFoxPro => "Visual FoxPro",
            DbfVersion.VisualFoxProAutoIncrement => "Visual FoxPro, autoincrement enabled",
            DbfVersion.VisualFoxProVarchar => "Visual FoxPro with field type Varchar or Varbinary",
            DbfVersion.DBase4SqlTable => "dBASE IV SQL table files, no memo",
            DbfVersion.DBase4SqlSystem => "dBASE IV SQL system files, no memo",
            DbfVersion.DBase3PlusWithMemo => "FoxBASE+/dBASE III PLUS, with memo",
            DbfVersion.DBase4WithMemo => "dBASE IV with memo",
            DbfVersion.DBase4SqlTableWithMemo => "dBASE IV SQL table files, with memo",
            DbfVersion.FoxPro2WithMemo => "FoxPro 2.x (or earlier) with memo",
            DbfVersion.HiPerSix => "HiPer-Six format with SMT memo file",
            DbfVersion.FoxBaseLegacy => "FoxBASE",
            _ => $"Unknown (0x{(byte)version:X2})",
        };
    }

    /// <summary>
    /// Determines if this DBF version supports memo fields
    /// </summary>
    /// <param name="version">The DBF version</param>
    /// <returns>True if memo fields are supported</returns>
    public static bool SupportsMemoFields(this DbfVersion version)
    {
        return version switch
        {
            DbfVersion.DBase3PlusWithMemo => true,
            DbfVersion.DBase4WithMemo => true,
            DbfVersion.DBase4SqlTableWithMemo => true,
            DbfVersion.FoxPro2WithMemo => true,
            DbfVersion.HiPerSix => true,
            DbfVersion.VisualFoxPro => true,
            DbfVersion.VisualFoxProAutoIncrement => true,
            DbfVersion.VisualFoxProVarchar => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines if this is a Visual FoxPro version
    /// </summary>
    /// <param name="version">The DBF version</param>
    /// <returns>True if this is a Visual FoxPro version</returns>
    public static bool IsVisualFoxPro(this DbfVersion version)
    {
        return version switch
        {
            DbfVersion.VisualFoxPro => true,
            DbfVersion.VisualFoxProAutoIncrement => true,
            DbfVersion.VisualFoxProVarchar => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines if this is an older DBF format (dBASE II/III era)
    /// </summary>
    /// <param name="version">The DBF version</param>
    /// <returns>True if this is an older format</returns>
    public static bool IsLegacyFormat(this DbfVersion version)
    {
        return version switch
        {
            DbfVersion.DBase2 => true,
            DbfVersion.DBase3Plus => true,
            DbfVersion.DBase3PlusWithMemo => true,
            _ => false,
        };
    }

    /// <summary>
    /// Converts a raw byte value to a DbfVersion enum value
    /// </summary>
    /// <param name="value">The raw byte value from the DBF header</param>
    /// <returns>The corresponding DbfVersion, or Unknown if not recognized</returns>
    public static DbfVersion FromByte(byte value)
    {
        if (Enum.IsDefined(typeof(DbfVersion), value))
        {
            return (DbfVersion)value;
        }

        return DbfVersion.Unknown;
    }
}
