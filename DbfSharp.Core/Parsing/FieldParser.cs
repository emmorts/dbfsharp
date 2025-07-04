using System.Globalization;
using System.Text;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Memo;

namespace DbfSharp.Core.Parsing;

/// <summary>
/// Default field parser implementation supporting all standard DBF field types
/// Based on the Python dbfread field_parser.py implementation
/// </summary>
public class FieldParser : FieldParserBase
{
    private static readonly
        Dictionary<FieldType, Func<FieldParser, DbfField, ReadOnlySpan<byte>, IMemoFile?, Encoding, DbfReaderOptions,
            object?>> Parsers;

    static FieldParser()
    {
        Parsers =
            new Dictionary<FieldType, Func<FieldParser, DbfField, ReadOnlySpan<byte>, IMemoFile?, Encoding,
                DbfReaderOptions, object?>>
            {
                { FieldType.Character, (p, f, d, m, e, o) => p.ParseCharacter(f, d, e, o) },
                { FieldType.Date, (p, f, d, m, e, o) => p.ParseDate(f, d, e, o) },
                { FieldType.Float, (p, f, d, m, e, o) => p.ParseFloat(f, d, e, o) },
                { FieldType.Integer, (p, f, d, m, e, o) => p.ParseInteger(f, d, e, o) },
                { FieldType.Logical, (p, f, d, m, e, o) => p.ParseLogical(f, d, e, o) },
                { FieldType.Memo, (p, f, d, m, e, o) => p.ParseMemo(f, d, m, e, o) },
                { FieldType.Numeric, (p, f, d, m, e, o) => p.ParseNumeric(f, d, e, o) },
                { FieldType.Double, (p, f, d, m, e, o) => p.ParseDouble(f, d, e, o) },
                { FieldType.Timestamp, (p, f, d, m, e, o) => p.ParseTimestamp(f, d, e, o) },
                { FieldType.Currency, (p, f, d, m, e, o) => p.ParseCurrency(f, d, e, o) },
                { FieldType.Binary, (p, f, d, m, e, o) => p.ParseBinary(f, d, m, e, o) },
                { FieldType.General, (p, f, d, m, e, o) => ParseGeneral(f, d, m, e, o) },
                { FieldType.Picture, (p, f, d, m, e, o) => p.ParsePicture(f, d, m, e, o) },
                { FieldType.Varchar, (p, f, d, m, e, o) => p.ParseVarchar(f, d, e, o) },
                { FieldType.Autoincrement, (p, f, d, m, e, o) => p.ParseAutoincrement(f, d, e, o) },
                { FieldType.TimestampAlternate, (p, f, d, m, e, o) => p.ParseTimestamp(f, d, e, o) },
                { FieldType.Flags, (p, f, d, m, e, o) => p.ParseFlags(f, d, e, o) },
            };
    }

    /// <summary>
    /// Gets the DBF version this parser is working with
    /// </summary>
    public DbfVersion DbfVersion { get; }

    /// <summary>
    /// Initializes a new instance of FieldParser
    /// </summary>
    /// <param name="dbfVersion">The DBF version for context-sensitive parsing</param>
    public FieldParser(DbfVersion dbfVersion = DbfVersion.Unknown)
    {
        DbfVersion = dbfVersion;
    }

    /// <summary>
    /// Determines if this parser can handle the specified field type and DBF version
    /// </summary>
    public override bool CanParse(FieldType fieldType, DbfVersion dbfVersion)
    {
        return Parsers.ContainsKey(fieldType);
    }

    /// <summary>
    /// Parses field data into a .NET object
    /// </summary>
    public override object? Parse(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, Encoding encoding,
        DbfReaderOptions options)
    {
        if (options.RawMode)
        {
            return data.ToArray();
        }

        if (Parsers.TryGetValue(field.Type, out var parser))
        {
            try
            {
                return parser(this, field, data, memoFile, encoding, options);
            }
            catch (Exception ex) when (!(ex is FieldParsingException))
            {
                if (options.ValidateFields)
                {
                    throw new FieldParsingException(field, data,
                        $"Failed to parse {field.Type} field '{field.Name}': {ex.Message}", ex);
                }

                return new InvalidValue(data.ToArray(), field, ex.Message);
            }
        }

        throw new NotSupportedException($"Field type {field.Type} is not supported");
    }

    /// <summary>
    /// Parses a character field (C)
    /// </summary>
    private object? ParseCharacter(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.IsEmpty)
            return string.Empty;

        var trimmed = options.TrimStrings ? TrimEnd(data) : data;
        return DecodeText(trimmed, encoding, options);
    }

    /// <summary>
    /// Parses a date field (D) - format YYYYMMDD
    /// </summary>
    private object? ParseDate(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.Length != 8)
            throw new FormatException($"Date field must be 8 bytes, got {data.Length}");

        if (IsNullValue(data))
            return null;

        var dateString = Encoding.ASCII.GetString(data);

        if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        throw new FormatException($"Invalid date format: '{dateString}'");
    }

    /// <summary>
    /// Parses a float field (F)
    /// </summary>
    private object? ParseFloat(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        var trimmed = TrimPadding(data);
        if (IsNullValue(trimmed))
            return null;

        var text = Encoding.ASCII.GetString(trimmed);
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new FormatException($"Invalid float format: '{text}'");
    }

    /// <summary>
    /// Parses an integer field (I) - 32-bit little-endian
    /// </summary>
    private object? ParseInteger(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.Length != 4)
            throw new FormatException($"Integer field must be 4 bytes, got {data.Length}");

        return BitConverter.ToInt32(data);
    }

    /// <summary>
    /// Parses a logical field (L) - T/F/Y/N/?/ 
    /// </summary>
    private object? ParseLogical(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.Length != 1)
            throw new FormatException($"Logical field must be 1 byte, got {data.Length}");

        return data[0] switch
        {
            (byte)'T' or (byte)'t' or (byte)'Y' or (byte)'y' => true,
            (byte)'F' or (byte)'f' or (byte)'N' or (byte)'n' => false,
            (byte)'?' or (byte)' ' or 0 => null,
            _ => throw new FormatException($"Invalid logical value: 0x{data[0]:X2}")
        };
    }

    /// <summary>
    /// Parses a memo field (M) - index to memo file
    /// </summary>
    private object? ParseMemo(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, Encoding encoding,
        DbfReaderOptions options)
    {
        var memoIndex = ParseMemoIndex(data);
        if (memoIndex == 0 || memoFile == null)
            return null;

        var memoData = memoFile.GetMemo(memoIndex);
        if (memoData == null)
            return null;

        // Handle binary memo data
        if (memoData is BinaryMemo)
            return memoData;

        // Convert text memo to string
        return DecodeText(memoData, encoding, options);
    }

    /// <summary>
    /// Parses a numeric field (N) - can be integer or decimal
    /// </summary>
    private object? ParseNumeric(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        var trimmed = TrimPadding(data);
        if (IsNullValue(trimmed))
            return null;

        var text = Encoding.ASCII.GetString(trimmed);

        // Replace comma with dot for decimal separator (some locales use comma)
        text = text.Replace(',', '.');

        // Try integer first if no decimal places
        if (field.ActualDecimalCount == 0)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
            {
                return intResult;
            }
        }

        // Try decimal
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalResult))
        {
            return decimalResult;
        }

        throw new FormatException($"Invalid numeric format: '{text}'");
    }

    /// <summary>
    /// Parses a double precision float field (O)
    /// </summary>
    private object? ParseDouble(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.Length != 8)
            throw new FormatException($"Double field must be 8 bytes, got {data.Length}");

        return BitConverter.ToDouble(data);
    }

    /// <summary>
    /// Parses a timestamp field (T or @) - Julian day + milliseconds
    /// </summary>
    private object? ParseTimestamp(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.Length != 8)
            throw new FormatException($"Timestamp field must be 8 bytes, got {data.Length}");

        if (IsNullValue(data))
            return null;

        // Extract Julian day (first 4 bytes) and milliseconds (last 4 bytes)
        var julianDay = BitConverter.ToUInt32(data.Slice(0, 4));
        var milliseconds = BitConverter.ToUInt32(data.Slice(4, 4));

        if (julianDay == 0)
            return null;

        // Convert Julian day to DateTime
        // Julian day offset to convert to .NET DateTime (proleptic Gregorian calendar)
        const int julianOffset = 1721425;

        try
        {
            var baseDate = DateTime.FromOADate(julianDay - julianOffset);
            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            return baseDate.Add(timeSpan);
        }
        catch (ArgumentException)
        {
            throw new FormatException($"Invalid timestamp: Julian day {julianDay}, milliseconds {milliseconds}");
        }
    }

    /// <summary>
    /// Parses a currency field (Y) - 64-bit integer with 4 decimal places
    /// </summary>
    private object? ParseCurrency(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        if (data.Length != 8)
            throw new FormatException($"Currency field must be 8 bytes, got {data.Length}");

        var value = BitConverter.ToInt64(data);
        return (decimal)value / 10000m;
    }

    /// <summary>
    /// Parses a binary field (B) - interpretation depends on DBF version
    /// </summary>
    private object? ParseBinary(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, Encoding encoding,
        DbfReaderOptions options)
    {
        // Visual FoxPro uses B for double precision float
        if (DbfVersion.IsVisualFoxPro())
        {
            if (data.Length == 8)
                return BitConverter.ToDouble(data);
        }

        // Other versions use B for memo index
        var memoIndex = ParseMemoIndex(data);
        if (memoIndex == 0 || memoFile == null)
            return null;

        return memoFile.GetMemo(memoIndex);
    }

    /// <summary>
    /// Parses a general/OLE field (G) - binary data in memo file
    /// </summary>
    private static byte[]? ParseGeneral(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, Encoding encoding,
        DbfReaderOptions options)
    {
        var memoIndex = ParseMemoIndex(data);
        if (memoIndex == 0 || memoFile == null)
            return null;

        return memoFile.GetMemo(memoIndex);
    }

    /// <summary>
    /// Parses a picture field (P) - binary image data in memo file
    /// </summary>
    private object? ParsePicture(DbfField field, ReadOnlySpan<byte> data, IMemoFile? memoFile, Encoding encoding,
        DbfReaderOptions options)
    {
        var memoIndex = ParseMemoIndex(data);
        if (memoIndex == 0 || memoFile == null)
            return null;

        return memoFile.GetMemo(memoIndex);
    }

    /// <summary>
    /// Parses a varchar field (V) - variable length character field
    /// </summary>
    private object? ParseVarchar(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        // Varchar fields are treated like character fields
        return ParseCharacter(field, data, encoding, options);
    }

    /// <summary>
    /// Parses an autoincrement field (+) - 32-bit integer
    /// </summary>
    private object? ParseAutoincrement(DbfField field, ReadOnlySpan<byte> data, Encoding encoding,
        DbfReaderOptions options)
    {
        return ParseInteger(field, data, encoding, options);
    }

    /// <summary>
    /// Parses a flags field (0) - binary flags
    /// </summary>
    private object? ParseFlags(DbfField field, ReadOnlySpan<byte> data, Encoding encoding, DbfReaderOptions options)
    {
        return data.ToArray();
    }
}