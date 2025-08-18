using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using DbfSharp.Core.Memo;

namespace DbfSharp.Core;

/// <summary>
/// Represents a single record from a DBF file with high-performance access patterns
/// </summary>
public readonly record struct DbfRecord : IEnumerable<KeyValuePair<string, object?>>
{
    private readonly DbfReader _reader;
    private readonly object?[] _values;

    /// <summary>
    /// Initializes a new DbfRecord
    /// </summary>
    /// <param name="reader">The DbfReader that owns this record.</param>
    /// <param name="values">The field values for this record.</param>
    internal DbfRecord(DbfReader reader, object?[] values)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// Gets the number of fields in this record
    /// </summary>
    public int FieldCount => _values?.Length ?? 0;

    /// <summary>
    /// Gets the field names
    /// </summary>
    public IReadOnlyList<string> FieldNames => _reader.FieldNames;

    /// <summary>
    /// Gets the field values
    /// </summary>
    public ReadOnlySpan<object?> Values => _values;

    /// <summary>
    /// Gets a value by field index
    /// </summary>
    /// <param name="index">The zero-based field index</param>
    /// <returns>The field value</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    public object? this[int index]
    {
        get
        {
            if (_values == null || index < 0 || index >= _values.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _values[index];
        }
    }

    /// <summary>
    /// Gets a value by field name
    /// </summary>
    /// <param name="fieldName">The field name (case-sensitive by default)</param>
    /// <returns>The field value</returns>
    /// <exception cref="ArgumentException">Thrown when field name is not found</exception>
    public object? this[string fieldName]
    {
        get
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "Field name cannot be null or empty",
                    nameof(fieldName)
                );
            }

            var index = GetFieldIndex(fieldName);
            if (index < 0)
            {
                throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));
            }

            return _values[index];
        }
    }

    #region Type-Specific Getters

    /// <summary>
    /// Gets the value of the specified field as a string.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The string value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a string.</exception>
    public string? GetString(int index)
    {
        return (string?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a string.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The string value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a string.</exception>
    public string? GetString(string fieldName)
    {
        return (string?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable boolean.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The nullable boolean value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable boolean.</exception>
    public bool? GetBoolean(int index)
    {
        return (bool?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable boolean.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The nullable boolean value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable boolean.</exception>
    public bool? GetBoolean(string fieldName)
    {
        return (bool?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable DateTime.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The nullable DateTime value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable DateTime.</exception>
    public DateTime? GetDateTime(int index)
    {
        return (DateTime?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable DateTime.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The nullable DateTime value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable DateTime.</exception>
    public DateTime? GetDateTime(string fieldName)
    {
        return (DateTime?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable decimal.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The nullable decimal value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable decimal.</exception>
    public decimal? GetDecimal(int index)
    {
        return (decimal?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable decimal.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The nullable decimal value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable decimal.</exception>
    public decimal? GetDecimal(string fieldName)
    {
        return (decimal?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable double.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The nullable double value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable double.</exception>
    public double? GetDouble(int index)
    {
        return (double?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable double.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The nullable double value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable double.</exception>
    public double? GetDouble(string fieldName)
    {
        return (double?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable float.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The nullable float value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable float.</exception>
    public float? GetFloat(int index)
    {
        return (float?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable float.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The nullable float value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable float.</exception>
    public float? GetFloat(string fieldName)
    {
        return (float?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable Int32.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The nullable Int32 value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable Int32.</exception>
    public int? GetInt32(int index)
    {
        return (int?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a nullable Int32.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The nullable Int32 value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a nullable Int32.</exception>
    public int? GetInt32(string fieldName)
    {
        return (int?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a byte array.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The byte array value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a byte array.</exception>
    public byte[]? GetByteArray(int index)
    {
        return (byte[]?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a byte array.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The byte array value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a byte array.</exception>
    public byte[]? GetByteArray(string fieldName)
    {
        return (byte[]?)this[fieldName];
    }

    /// <summary>
    /// Gets the value of the specified field as a MemoData object.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The MemoData value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a MemoData object.</exception>
    public MemoData? GetMemo(int index)
    {
        return (MemoData?)this[index];
    }

    /// <summary>
    /// Gets the value of the specified field as a MemoData object.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The MemoData value of the field.</returns>
    /// <exception cref="InvalidCastException">Thrown if the field value is not a MemoData object.</exception>
    public MemoData? GetMemo(string fieldName)
    {
        return (MemoData?)this[fieldName];
    }

    #endregion

    /// <summary>
    /// Tries to get a value by field name
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The field value if found</param>
    /// <returns>True if the field was found, false otherwise</returns>
    public bool TryGetValue(string fieldName, [MaybeNullWhen(false)] out object? value)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            value = null;
            return false;
        }

        var index = GetFieldIndex(fieldName);
        if (index < 0)
        {
            value = null;
            return false;
        }

        value = _values[index];
        return true;
    }

    /// <summary>
    /// Tries to get a value by field index
    /// </summary>
    /// <param name="index">The zero-based field index</param>
    /// <param name="value">The field value if found</param>
    /// <returns>True if the index was valid, false otherwise</returns>
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out object? value)
    {
        if (_values != null && index >= 0 && index < _values.Length)
        {
            value = _values[index];
            return true;
        }

        value = null;
        return false;
    }

    #region Type-Specific TryGet...

    /// <summary>
    /// Tries to get the value of the specified field as a string.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <param name="value">When this method returns, contains the string value of the field, if the retrieval succeeds; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns>true if the value is successfully retrieved and is of the specified type; otherwise, false.</returns>
    public bool TryGetString(int index, [MaybeNullWhen(false)] out string? value)
    {
        if (TryGetValue(index, out var rawValue) && rawValue is string stringValue)
        {
            value = stringValue;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Tries to get the value of the specified field as a string.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="value">When this method returns, contains the string value of the field, if the retrieval succeeds; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns>true if the value is successfully retrieved and is of the specified type; otherwise, false.</returns>
    public bool TryGetString(string fieldName, [MaybeNullWhen(false)] out string? value)
    {
        if (TryGetValue(fieldName, out var rawValue) && rawValue is string stringValue)
        {
            value = stringValue;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Tries to get the value of the specified field as a nullable boolean.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <param name="value">When this method returns, contains the boolean value of the field, if the retrieval succeeds; otherwise, the default value. This parameter is passed uninitialized.</param>
    /// <returns>true if the value is successfully retrieved and is of the specified type; otherwise, false.</returns>
    public bool TryGetBoolean(int index, [MaybeNullWhen(false)] out bool? value)
    {
        if (TryGetValue(index, out var rawValue) && rawValue is bool boolValue)
        {
            value = boolValue;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Tries to get the value of the specified field as a nullable boolean.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="value">When this method returns, contains the boolean value of the field, if the retrieval succeeds; otherwise, the default value. This parameter is passed uninitialized.</param>
    /// <returns>true if the value is successfully retrieved and is of the specified type; otherwise, false.</returns>
    public bool TryGetBoolean(string fieldName, [MaybeNullWhen(false)] out bool? value)
    {
        if (TryGetValue(fieldName, out var rawValue) && rawValue is bool boolValue)
        {
            value = boolValue;
            return true;
        }
        value = null;
        return false;
    }

    #endregion

    /// <summary>
    /// Checks if a field with the specified name exists
    /// </summary>
    /// <param name="fieldName">The field name to check</param>
    /// <returns>True if the field exists, false otherwise</returns>
    public bool HasField(string fieldName)
    {
        return !string.IsNullOrEmpty(fieldName) && GetFieldIndex(fieldName) >= 0;
    }

    /// <summary>
    /// Gets the field name for a given index
    /// </summary>
    /// <param name="index">The zero-based field index</param>
    /// <returns>The field name</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    public string GetFieldName(int index)
    {
        if (_reader.FieldNames == null || index < 0 || index >= _reader.FieldNames.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _reader.FieldNames[index];
    }

    /// <summary>
    /// Creates a dictionary representation of this record
    /// </summary>
    /// <returns>A dictionary with field names as keys and values as values</returns>
    public Dictionary<string, object?> ToDictionary()
    {
        if (_values == null)
        {
            return new Dictionary<string, object?>();
        }

        var result = new Dictionary<string, object?>(_reader.FieldNames.Count);
        for (var i = 0; i < _reader.FieldNames.Count; i++)
        {
            result[_reader.FieldNames[i]] = _values[i];
        }

        return result;
    }

    /// <summary>
    /// Creates a read-only dictionary representation of this record
    /// </summary>
    /// <returns>A read-only dictionary with field names as keys and values as values</returns>
    public IReadOnlyDictionary<string, object?> ToReadOnlyDictionary()
    {
        return ToDictionary();
    }

    /// <summary>
    /// Gets an enumerator for the field name-value pairs
    /// </summary>
    /// <returns>An enumerator of key-value pairs</returns>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        if (_values == null)
        {
            yield break;
        }

        for (var i = 0; i < _reader.FieldNames.Count; i++)
        {
            yield return new KeyValuePair<string, object?>(_reader.FieldNames[i], _values[i]);
        }
    }

    /// <summary>
    /// Gets an enumerator for the field name-value pairs
    /// </summary>
    /// <returns>An enumerator of key-value pairs</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Gets the field index for a given field name
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <returns>The field index, or -1 if not found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFieldIndex(string fieldName)
    {
        return _reader.GetFieldIndex(fieldName);
    }

    /// <summary>
    /// Returns a string representation of this record
    /// </summary>
    public override string ToString()
    {
        if (_values == null)
        {
            return "Empty DBF Record";
        }

        var fields = new string[_reader.FieldNames.Count];
        for (var i = 0; i < _reader.FieldNames.Count; i++)
        {
            var valueStr = _values[i] switch
            {
                null => "null",
                DateTime dt => dt.ToString(CultureInfo.CurrentCulture),
                byte[] bytes => $"byte[{bytes.Length}]",
                _ => _values[i]!.ToString(),
            };

            fields[i] = $"{_reader.FieldNames[i]}={valueStr}";
        }

        return $"DBF Record: {{{string.Join(", ", fields)}}}";
    }
}
