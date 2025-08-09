using System.Runtime.CompilerServices;
using System.Text;
using DbfSharp.Core.Memo;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Exceptions;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Core;

/// <summary>
/// A zero-allocation, span-based DBF record that provides direct access to field data
/// without materializing objects until explicitly requested. This ref struct cannot
/// cross await boundaries but provides maximum performance for synchronous processing.
/// </summary>
public readonly ref struct SpanDbfRecord
{
    private readonly ReadOnlySpan<byte> _recordData;
    private readonly DbfReader _reader;
    private readonly bool _isDeleted;

    /// <summary>
    /// Initializes a new span-based DBF record
    /// </summary>
    /// <param name="reader">The DbfReader that owns this record</param>
    /// <param name="recordBuffer">The raw record bytes including deletion flag</param>
    internal SpanDbfRecord(DbfReader reader, ReadOnlySpan<byte> recordBuffer)
    {
        _reader = reader;
        _isDeleted = recordBuffer[0] == '*';
        _recordData = recordBuffer[1..]; // Skip deletion flag
    }

    /// <summary>
    /// Gets whether this record is marked as deleted
    /// </summary>
    public bool IsDeleted => _isDeleted;

    /// <summary>
    /// Gets the number of fields in this record
    /// </summary>
    public int FieldCount => _reader.Fields.Count;

    /// <summary>
    /// Gets the field names
    /// </summary>
    public IReadOnlyList<string> FieldNames => _reader.FieldNames;

    /// <summary>
    /// Gets the raw field data as a span of bytes without any parsing or allocation
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>Raw field bytes</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetFieldBytes(int index)
    {
        if (index < 0 || index >= _reader.Fields.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var field = _reader.Fields[index];
        var useAddressBasedParsing = _reader.Header.DbfVersion.IsVisualFoxPro() && _reader.HasMeaningfulAddresses();

        if (useAddressBasedParsing)
        {
            var fieldOffset = (int)field.Address - 1; // Address is 1-based, convert to 0-based
            var fieldLength = field.ActualLength;
            
            if (_recordData.Length < fieldOffset + fieldLength)
            {
                return ReadOnlySpan<byte>.Empty;
            }
            
            return _recordData.Slice(fieldOffset, fieldLength);
        }
        else
        {
            // Sequential parsing
            var offset = 0;
            for (var i = 0; i < index; i++)
            {
                offset += _reader.Fields[i].ActualLength;
            }
            
            var length = field.ActualLength;
            if (_recordData.Length < offset + length)
            {
                return ReadOnlySpan<byte>.Empty;
            }
            
            return _recordData.Slice(offset, length);
        }
    }

    /// <summary>
    /// Gets the raw field data as a span of bytes without any parsing or allocation
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>Raw field bytes</returns>
    /// <exception cref="ArgumentException">Thrown when field name is not found</exception>
    public ReadOnlySpan<byte> GetFieldBytes(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
        {
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        }
        return GetFieldBytes(index);
    }

    /// <summary>
    /// Tries to get the raw field data as a span of bytes
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <param name="fieldBytes">The field bytes if successful</param>
    /// <returns>True if the field was found and valid</returns>
    public bool TryGetFieldBytes(int index, out ReadOnlySpan<byte> fieldBytes)
    {
        if (index < 0 || index >= _reader.Fields.Count)
        {
            fieldBytes = default;
            return false;
        }

        fieldBytes = GetFieldBytes(index);
        return true;
    }

    /// <summary>
    /// Tries to get the raw field data as a span of bytes
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="fieldBytes">The field bytes if successful</param>
    /// <returns>True if the field was found and valid</returns>
    public bool TryGetFieldBytes(string fieldName, out ReadOnlySpan<byte> fieldBytes)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
        {
            fieldBytes = default;
            return false;
        }

        fieldBytes = GetFieldBytes(index);
        return true;
    }

    /// <summary>
    /// Parses a field value using the DBF reader's field parser (may allocate)
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>The parsed field value</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    public object? GetValue(int index)
    {
        if (index < 0 || index >= _reader.Fields.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var field = _reader.Fields[index];
        var fieldBytes = GetFieldBytes(index);
        
        if (fieldBytes.IsEmpty)
        {
            return new InvalidValue(Array.Empty<byte>(), field, "Field data is empty or truncated");
        }

        try
        {
            return _reader.FieldParser.Parse(field, fieldBytes, _reader.MemoFile, _reader.Encoding, _reader.Options);
        }
        catch (Exception ex)
        {
            if (_reader.Options.ValidateFields)
            {
                throw new FieldParseException(field.Name, field.Type.ToString(), fieldBytes.ToArray(),
                    $"Failed to parse field: {ex.Message}");
            }

            return new InvalidValue(fieldBytes.ToArray(), field, ex.Message);
        }
    }

    /// <summary>
    /// Parses a field value using the DBF reader's field parser (may allocate)
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>The parsed field value</returns>
    /// <exception cref="ArgumentException">Thrown when field name is not found</exception>
    public object? GetValue(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
        {
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        }
        return GetValue(index);
    }

    #region Typed Accessors

    /// <summary>
    /// Gets a field as a string, with zero-allocation trimming for performance
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>The string value, or null if field is empty</returns>
    public string? GetString(int index)
    {
        var fieldBytes = GetFieldBytes(index);
        if (fieldBytes.IsEmpty)
            return null;

        // Trim null bytes and spaces for DBF character fields
        var trimmed = TrimDbfString(fieldBytes);
        if (trimmed.IsEmpty)
            return string.Empty;

        return _reader.Encoding.GetString(trimmed);
    }

    /// <summary>
    /// Gets a field as a string, with zero-allocation trimming for performance
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>The string value, or null if field is empty</returns>
    public string? GetString(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        return GetString(index);
    }

    /// <summary>
    /// Parses a numeric field directly from bytes
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>The integer value, or null if field is empty/invalid</returns>
    public int? GetInt32(int index)
    {
        var field = _reader.Fields[index];
        var fieldBytes = GetFieldBytes(index);
        if (fieldBytes.IsEmpty)
            return null;

        // For binary integer fields (I type), read directly
        if (field.Type == FieldType.Integer && fieldBytes.Length == 4)
        {
            return BitConverter.ToInt32(fieldBytes);
        }

        // For text-based numeric fields, parse from ASCII
        var trimmed = TrimDbfString(fieldBytes);
        if (trimmed.IsEmpty)
            return null;

        var text = Encoding.ASCII.GetString(trimmed);
        if (text == "-")  // Special case: lone dash represents null
            return null;

        return int.TryParse(text, out var result) ? result : null;
    }

    /// <summary>
    /// Parses a numeric field directly from bytes
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>The integer value, or null if field is empty/invalid</returns>
    public int? GetInt32(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        return GetInt32(index);
    }

    /// <summary>
    /// Parses a decimal field directly from bytes
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>The decimal value, or null if field is empty/invalid</returns>
    public decimal? GetDecimal(int index)
    {
        var fieldBytes = GetFieldBytes(index);
        if (fieldBytes.IsEmpty)
            return null;

        var trimmed = TrimDbfString(fieldBytes);
        if (trimmed.IsEmpty)
            return null;

        var text = Encoding.ASCII.GetString(trimmed);
        if (text == "-")  // Special case: lone dash represents null
            return null;

        // Replace comma with dot for decimal separator
        text = text.Replace(',', '.');

        return decimal.TryParse(text, System.Globalization.NumberStyles.Number, 
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// Parses a decimal field directly from bytes
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>The decimal value, or null if field is empty/invalid</returns>
    public decimal? GetDecimal(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        return GetDecimal(index);
    }

    /// <summary>
    /// Parses a date field directly from bytes (YYYYMMDD format)
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>The DateTime value, or null if field is empty/invalid</returns>
    public DateTime? GetDateTime(int index)
    {
        var fieldBytes = GetFieldBytes(index);
        if (fieldBytes.IsEmpty || fieldBytes.Length != 8)
            return null;

        // Check if field is all spaces or nulls (empty date)
        var isEmpty = true;
        foreach (var b in fieldBytes)
        {
            if (b != 0 && b != 32) // not null or space
            {
                isEmpty = false;
                break;
            }
        }
        
        if (isEmpty)
            return null;

        var dateString = Encoding.ASCII.GetString(fieldBytes);
        return DateTime.TryParseExact(dateString, "yyyyMMdd", 
            System.Globalization.CultureInfo.InvariantCulture, 
            System.Globalization.DateTimeStyles.None, out var date) ? date : null;
    }

    /// <summary>
    /// Parses a date field directly from bytes (YYYYMMDD format)
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>The DateTime value, or null if field is empty/invalid</returns>
    public DateTime? GetDateTime(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        return GetDateTime(index);
    }

    /// <summary>
    /// Parses a logical field directly from bytes
    /// </summary>
    /// <param name="index">Zero-based field index</param>
    /// <returns>The boolean value, or null if field is empty/invalid</returns>
    public bool? GetBoolean(int index)
    {
        var fieldBytes = GetFieldBytes(index);
        if (fieldBytes.IsEmpty)
            return null;

        return fieldBytes[0] switch
        {
            (byte)'T' or (byte)'t' or (byte)'Y' or (byte)'y' => true,
            (byte)'F' or (byte)'f' or (byte)'N' or (byte)'n' => false,
            (byte)'?' or (byte)' ' or 0 => null,
            _ => null
        };
    }

    /// <summary>
    /// Parses a logical field directly from bytes
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>The boolean value, or null if field is empty/invalid</returns>
    public bool? GetBoolean(string fieldName)
    {
        var index = _reader.GetFieldIndex(fieldName);
        if (index < 0)
            throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
        return GetBoolean(index);
    }

    #endregion

    /// <summary>
    /// Trims DBF string field data (null bytes and spaces from end, spaces from start)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> TrimDbfString(ReadOnlySpan<byte> data)
    {
        var trimmed = data;

        // Trim from end: nulls, spaces, asterisks (padding characters)
        while (trimmed.Length > 0)
        {
            var lastByte = trimmed[^1];
            if (lastByte is 0 or 32 or 42) // null, space, asterisk
            {
                trimmed = trimmed[..^1];
            }
            else
            {
                break;
            }
        }

        // Trim from start: nulls and spaces only
        while (trimmed.Length > 0)
        {
            var firstByte = trimmed[0];
            if (firstByte is 0 or 32) // null, space
            {
                trimmed = trimmed[1..];
            }
            else
            {
                break;
            }
        }

        return trimmed;
    }

    /// <summary>
    /// Gets the field name for a given index
    /// </summary>
    /// <param name="index">The zero-based field index</param>
    /// <returns>The field name</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    public string GetFieldName(int index)
    {
        if (index < 0 || index >= _reader.FieldNames.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _reader.FieldNames[index];
    }

    /// <summary>
    /// Checks if a field with the specified name exists
    /// </summary>
    /// <param name="fieldName">The field name to check</param>
    /// <returns>True if the field exists, false otherwise</returns>
    public bool HasField(string fieldName)
    {
        return !string.IsNullOrEmpty(fieldName) && _reader.GetFieldIndex(fieldName) >= 0;
    }
}