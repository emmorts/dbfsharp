using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DbfSharp.Core;

/// <summary>
/// Represents a single record from a DBF file with high-performance access patterns
/// </summary>
public readonly struct DbfRecord : IEnumerable<KeyValuePair<string, object?>>
{
    private readonly object?[] _values;
    private readonly string[] _fieldNames;
    private readonly Dictionary<string, int>? _fieldIndexMap;

    /// <summary>
    /// Initializes a new DbfRecord
    /// </summary>
    /// <param name="values">The field values</param>
    /// <param name="fieldNames">The field names</param>
    /// <param name="fieldIndexMap">Optional field name to index mapping for fast lookups</param>
    internal DbfRecord(object?[] values, string[] fieldNames, Dictionary<string, int>? fieldIndexMap = null)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _fieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));
        _fieldIndexMap = fieldIndexMap;

        if (values.Length != fieldNames.Length)
            throw new ArgumentException("Values and field names arrays must have the same length");
    }

    /// <summary>
    /// Gets the number of fields in this record
    /// </summary>
    public int FieldCount => _values?.Length ?? 0;

    /// <summary>
    /// Gets the field names
    /// </summary>
    public ReadOnlySpan<string> FieldNames => _fieldNames;

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
                throw new ArgumentOutOfRangeException(nameof(index));
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
                throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));

            var index = GetFieldIndex(fieldName);
            if (index < 0)
                throw new ArgumentException($"Field '{fieldName}' not found", nameof(fieldName));

            return _values[index];
        }
    }

    /// <summary>
    /// Gets a strongly-typed value by field index
    /// </summary>
    /// <typeparam name="T">The expected type</typeparam>
    /// <param name="index">The zero-based field index</param>
    /// <returns>The field value cast to the specified type</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be cast to the specified type</exception>
    public T GetValue<T>(int index)
    {
        var value = this[index];
        return ConvertValue<T>(value);
    }

    /// <summary>
    /// Gets a strongly-typed value by field name
    /// </summary>
    /// <typeparam name="T">The expected type</typeparam>
    /// <param name="fieldName">The field name</param>
    /// <returns>The field value cast to the specified type</returns>
    /// <exception cref="ArgumentException">Thrown when field name is not found</exception>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be cast to the specified type</exception>
    public T GetValue<T>(string fieldName)
    {
        var value = this[fieldName];
        return ConvertValue<T>(value);
    }

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
    /// Tries to get a strongly-typed value by field name
    /// </summary>
    /// <typeparam name="T">The expected type</typeparam>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The field value if found and convertible</param>
    /// <returns>True if the field was found and convertible, false otherwise</returns>
    public bool TryGetValue<T>(string fieldName, [MaybeNullWhen(false)] out T value)
    {
        if (TryGetValue(fieldName, out var rawValue))
        {
            try
            {
                value = ConvertValue<T>(rawValue);
                return true;
            }
            catch
            {
                // Conversion failed
            }
        }

        value = default;
        return false;
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

    /// <summary>
    /// Tries to get a strongly-typed value by field index
    /// </summary>
    /// <typeparam name="T">The expected type</typeparam>
    /// <param name="index">The zero-based field index</param>
    /// <param name="value">The field value if found and convertible</param>
    /// <returns>True if the index was valid and value convertible, false otherwise</returns>
    public bool TryGetValue<T>(int index, [MaybeNullWhen(false)] out T value)
    {
        if (TryGetValue(index, out var rawValue))
        {
            try
            {
                value = ConvertValue<T>(rawValue);
                return true;
            }
            catch
            {
                // Conversion failed
            }
        }

        value = default;
        return false;
    }

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
        if (_fieldNames == null || index < 0 || index >= _fieldNames.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _fieldNames[index];
    }

    /// <summary>
    /// Creates a dictionary representation of this record
    /// </summary>
    /// <returns>A dictionary with field names as keys and values as values</returns>
    public Dictionary<string, object?> ToDictionary()
    {
        if (_fieldNames == null || _values == null)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>(_fieldNames.Length);
        for (int i = 0; i < _fieldNames.Length; i++)
        {
            result[_fieldNames[i]] = _values[i];
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
        if (_fieldNames == null || _values == null)
            yield break;

        for (int i = 0; i < _fieldNames.Length; i++)
        {
            yield return new KeyValuePair<string, object?>(_fieldNames[i], _values[i]);
        }
    }

    /// <summary>
    /// Gets an enumerator for the field name-value pairs
    /// </summary>
    /// <returns>An enumerator of key-value pairs</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the field index for a given field name
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <returns>The field index, or -1 if not found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFieldIndex(string fieldName)
    {
        // Use the index map if available for O(1) lookup
        if (_fieldIndexMap != null)
        {
            return _fieldIndexMap.TryGetValue(fieldName, out var index) ? index : -1;
        }

        // Fall back to linear search
        if (_fieldNames != null)
        {
            for (int i = 0; i < _fieldNames.Length; i++)
            {
                if (string.Equals(_fieldNames[i], fieldName, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Converts a value to the specified type with support for DBF-specific conversions
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="value">The value to convert</param>
    /// <returns>The converted value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T ConvertValue<T>(object? value)
    {
        if (value is T directCast)
            return directCast;

        if (value == null)
        {
            if (default(T) == null)
                return default!;
            throw new InvalidCastException($"Cannot convert null to non-nullable type {typeof(T)}");
        }

        // Handle common conversions
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {typeof(T)}", ex);
        }
    }

    /// <summary>
    /// Returns a string representation of this record
    /// </summary>
    public override string ToString()
    {
        if (_fieldNames == null || _values == null)
            return "Empty DBF Record";

        var fields = new string[_fieldNames.Length];
        for (int i = 0; i < _fieldNames.Length; i++)
        {
            var valueStr = _values[i]?.ToString() ?? "null";
            fields[i] = $"{_fieldNames[i]}={valueStr}";
        }

        return $"DBF Record: {{{string.Join(", ", fields)}}}";
    }

    /// <summary>
    /// Determines equality based on field values
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is DbfRecord other && Equals(other);
    }

    /// <summary>
    /// Determines equality based on field values
    /// </summary>
    public bool Equals(DbfRecord other)
    {
        if (_values == null && other._values == null)
            return true;
        if (_values == null || other._values == null)
            return false;
        if (_values.Length != other._values.Length)
            return false;

        for (int i = 0; i < _values.Length; i++)
        {
            if (!Equals(_values[i], other._values[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a hash code based on field values
    /// </summary>
    public override int GetHashCode()
    {
        if (_values == null)
            return 0;

        var hash = new HashCode();
        foreach (var value in _values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(DbfRecord left, DbfRecord right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(DbfRecord left, DbfRecord right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates an empty record
    /// </summary>
    /// <returns>An empty DbfRecord</returns>
    public static DbfRecord Empty => new(Array.Empty<object>(), Array.Empty<string>());
}