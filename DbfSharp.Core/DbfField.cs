using System.Runtime.InteropServices;
using System.Text;
using DbfSharp.Core.Enums;

namespace DbfSharp.Core;

/// <summary>
/// Represents a field descriptor in a DBF file
/// Based on the DBFField structure from the Python dbfread implementation
/// </summary>
public readonly struct DbfField
{
    /// <summary>
    /// Size of each field descriptor in bytes
    /// </summary>
    public const int Size = 32;

    /// <summary>
    /// Size of dBASE II field descriptor in bytes
    /// </summary>
    public const int DBase2Size = 16;

    /// <summary>
    /// Maximum length of a field name
    /// </summary>
    public const int MaxNameLength = 11;

    /// <summary>
    /// Maximum length of a dBASE II field name
    /// </summary>
    public const int DBase2MaxNameLength = 11;

    /// <summary>
    /// Field name (null-terminated, max 11 characters)
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Field type character
    /// </summary>
    public readonly FieldType Type;

    /// <summary>
    /// Field data address (not used in file-based DBF)
    /// </summary>
    public readonly uint Address;

    /// <summary>
    /// Field length in bytes
    /// </summary>
    public readonly byte Length;

    /// <summary>
    /// Number of decimal places for numeric fields
    /// </summary>
    public readonly byte DecimalCount;

    /// <summary>
    /// Reserved field 1
    /// </summary>
    public readonly ushort Reserved1;

    /// <summary>
    /// Work area ID
    /// </summary>
    public readonly byte WorkAreaId;

    /// <summary>
    /// Reserved field 2
    /// </summary>
    public readonly byte Reserved2;

    /// <summary>
    /// Reserved field 3
    /// </summary>
    public readonly byte Reserved3;

    /// <summary>
    /// Set fields flag
    /// </summary>
    public readonly byte SetFieldsFlag;

    /// <summary>
    /// Reserved field 4 (7 bytes)
    /// </summary>
    public readonly ulong Reserved4;

    /// <summary>
    /// Index field flag
    /// </summary>
    public readonly byte IndexFieldFlag;

    /// <summary>
    /// Initializes a new instance of the DbfField struct
    /// </summary>
    public DbfField(
        string name,
        FieldType type,
        uint address,
        byte length,
        byte decimalCount,
        ushort reserved1,
        byte workAreaId,
        byte reserved2,
        byte reserved3,
        byte setFieldsFlag,
        ulong reserved4,
        byte indexFieldFlag)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Address = address;
        Length = length;
        DecimalCount = decimalCount;
        Reserved1 = reserved1;
        WorkAreaId = workAreaId;
        Reserved2 = reserved2;
        Reserved3 = reserved3;
        SetFieldsFlag = setFieldsFlag;
        Reserved4 = reserved4;
        IndexFieldFlag = indexFieldFlag;
    }

    /// <summary>
    /// Gets the expected .NET type for this field
    /// </summary>
    public Type ExpectedNetType => Type.GetExpectedNetType();

    /// <summary>
    /// Indicates whether this field type uses memo files
    /// </summary>
    public bool UsesMemoFile => Type.UsesMemoFile();

    /// <summary>
    /// Indicates whether this field type supports null values
    /// </summary>
    public bool SupportsNull => Type.SupportsNull();

    /// <summary>
    /// Gets the actual field length, handling special cases
    /// </summary>
    public int ActualLength
    {
        get
        {
            // For character fields > 255 bytes, the high byte is stored in decimal_count
            if (Type == FieldType.Character)
            {
                return Length | (DecimalCount << 8);
            }

            return Length;
        }
    }

    /// <summary>
    /// Gets the actual decimal count, handling special cases
    /// </summary>
    public byte ActualDecimalCount
    {
        get
        {
            // For character fields > 255 bytes, decimal_count is used for length extension
            if (Type == FieldType.Character && Length == 0)
            {
                return 0;
            }

            return DecimalCount;
        }
    }

    /// <summary>
    /// Reads field descriptors from a binary reader
    /// </summary>
    /// <param name="reader">The binary reader positioned after the DBF header</param>
    /// <param name="encoding">The encoding to use for field names</param>
    /// <param name="fieldCount">The expected number of fields</param>
    /// <param name="lowerCaseNames">Whether to convert field names to lowercase</param>
    /// <param name="dbfVersion">The DBF version for format-specific parsing</param>
    /// <returns>An array of field descriptors</returns>
    public static DbfField[] ReadFields(BinaryReader reader, Encoding encoding, int fieldCount,
        bool lowerCaseNames = false, DbfVersion dbfVersion = DbfVersion.Unknown)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));

        // Handle dBASE II differently
        if (dbfVersion == DbfVersion.DBase2)
        {
            return ReadDBase2Fields(reader, encoding, lowerCaseNames);
        }

        var fields = new List<DbfField>();

        // Read field descriptors until we hit the terminator (0x0D) or reach the end
        while (true)
        {
            // Peek at the first byte to check for field terminator
            var currentPosition = reader.BaseStream.Position;
            var firstByte = reader.ReadByte();

            // Check for field terminator
            if (firstByte == 0x0D)
            {
                // Field terminator found, we're done reading fields
                break;
            }

            // Check for end of file or invalid data
            if (firstByte == 0x00 || firstByte == 0x1A)
            {
                // Likely hit end of data or invalid field
                reader.BaseStream.Position = currentPosition; // Reset position
                break;
            }

            // Read the rest of the field descriptor
            var remainingBytes = reader.ReadBytes(Size - 1);
            if (remainingBytes.Length != Size - 1)
            {
                // Not enough data for a complete field descriptor
                reader.BaseStream.Position = currentPosition; // Reset position
                break;
            }

            // Combine first byte with remaining bytes
            var fieldBytes = new byte[Size];
            fieldBytes[0] = firstByte;
            Array.Copy(remainingBytes, 0, fieldBytes, 1, remainingBytes.Length);

            try
            {
                var field = FromBytes(fieldBytes, encoding, lowerCaseNames);

                // Validate that this looks like a real field
                if (string.IsNullOrWhiteSpace(field.Name) || field.ActualLength == 0)
                {
                    // This doesn't look like a valid field, stop reading
                    reader.BaseStream.Position = currentPosition; // Reset position
                    break;
                }

                fields.Add(field);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("field terminator"))
            {
                // Skip this field and continue
                break;
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Unknown field type"))
            {
                // For unknown field types, create a character field as fallback
                var fallbackField = CreateFallbackField(fieldBytes, encoding, lowerCaseNames, fields.Count);
                if (fallbackField.HasValue)
                {
                    fields.Add(fallbackField.Value);
                }
            }
            catch (Exception)
            {
                // If we can't parse this field, it might not be a valid field descriptor
                // Reset position and stop reading
                reader.BaseStream.Position = currentPosition;
                break;
            }

            // Safety check to prevent infinite loops
            if (fields.Count >= 255) // DBF files can't have more than 255 fields
            {
                break;
            }
        }

        return fields.ToArray();
    }

    /// <summary>
    /// Reads dBASE II field descriptors which have a different format
    /// </summary>
    private static DbfField[] ReadDBase2Fields(BinaryReader reader, Encoding encoding, bool lowerCaseNames)
    {
        var fields = new List<DbfField>();
        
        // dBASE II field descriptors are 16 bytes each and start immediately after the 32-byte header
        // They continue until we reach the actual data records
        // There's no explicit field terminator - we need to detect when fields end
        
        var startPosition = reader.BaseStream.Position;
        
        while (true)
        {
            var currentPosition = reader.BaseStream.Position;
            
            // Check if we have enough bytes for a field descriptor
            if (reader.BaseStream.Position + DBase2Size > reader.BaseStream.Length)
            {
                reader.BaseStream.Position = currentPosition;
                break;
            }
            
            // Try to read a 16-byte field descriptor
            var fieldBytes = reader.ReadBytes(DBase2Size);
            if (fieldBytes.Length != DBase2Size)
            {
                // Not enough data, we've reached the end
                reader.BaseStream.Position = currentPosition;
                break;
            }

            // Check if this looks like a valid field descriptor
            // Field name should be in first 11 bytes and contain printable characters
            var nameBytes = fieldBytes.AsSpan(0, DBase2MaxNameLength);
            var nullTerminator = nameBytes.IndexOf((byte)0);
            if (nullTerminator >= 0)
            {
                nameBytes = nameBytes.Slice(0, nullTerminator);
            }
            
            // Check if name contains only printable ASCII characters
            bool validName = true;
            if (nameBytes.Length == 0)
            {
                validName = false;
            }
            else
            {
                foreach (var b in nameBytes)
                {
                    if (b < 32 || b > 126) // Not printable ASCII
                    {
                        validName = false;
                        break;
                    }
                }
            }
            
            // Check field type (should be a known type character)
            var typeChar = (char)fieldBytes[11];
            var fieldType = FieldTypeExtensions.FromChar(typeChar);
            
            // Check field length (should be reasonable)
            var length = fieldBytes[12];
            
            if (!validName || fieldType == null || length == 0 || length > 255)
            {
                // This doesn't look like a valid field descriptor
                // We've probably reached the data section
                reader.BaseStream.Position = currentPosition;
                break;
            }

            try
            {
                var field = FromDBase2Bytes(fieldBytes, encoding, lowerCaseNames);
                fields.Add(field);
            }
            catch (Exception)
            {
                // Failed to parse, probably reached data section
                reader.BaseStream.Position = currentPosition;
                break;
            }
            
            // Safety check to prevent infinite loops
            if (fields.Count >= 128) // dBASE II typically has fewer fields
            {
                break;
            }
        }
        
        return fields.ToArray();
    }

    /// <summary>
    /// Creates a dBASE II field from a 16-byte descriptor
    /// </summary>
    private static DbfField FromDBase2Bytes(ReadOnlySpan<byte> bytes, Encoding encoding, bool lowerCaseName)
    {
        if (bytes.Length != DBase2Size)
            throw new ArgumentException($"Expected {DBase2Size} bytes for dBASE II field descriptor, got {bytes.Length}");

        // dBASE II field format (16 bytes):
        // Bytes 0-10: Field name (11 bytes, null-terminated)
        // Byte 11: Field type
        // Byte 12: Field length
        // Byte 13: Decimal count
        // Bytes 14-15: Reserved/unused
        
        // Extract field name (first 11 bytes, null-terminated)
        var nameBytes = bytes.Slice(0, DBase2MaxNameLength);
        var nullTerminator = nameBytes.IndexOf((byte)0);
        if (nullTerminator >= 0)
        {
            nameBytes = nameBytes.Slice(0, nullTerminator);
        }

        var name = encoding.GetString(nameBytes);
        if (lowerCaseName)
        {
            name = name.ToLowerInvariant();
        }

        // Extract field type
        var typeChar = (char)bytes[11];
        var fieldType = FieldTypeExtensions.FromChar(typeChar) ?? FieldType.Character;

        // Extract field length and decimal count
        var length = bytes[12];
        var decimalCount = bytes[13];

        return new DbfField(
            name,
            fieldType,
            0, // address - not used in file-based DBF
            length,
            decimalCount,
            0, // reserved1
            0, // work area id
            0, // reserved2
            0, // reserved3
            0, // set fields flag
            0, // reserved4
            0  // index field flag
        );
    }

    /// <summary>
    /// Creates a fallback field for unknown field types
    /// </summary>
    private static DbfField? CreateFallbackField(byte[] fieldBytes, Encoding encoding, bool lowerCaseNames,
        int fieldIndex)
    {
        try
        {
            // Extract field name
            var nameBytes = fieldBytes.AsSpan(0, MaxNameLength);
            var nullTerminator = nameBytes.IndexOf((byte)0);
            if (nullTerminator >= 0)
            {
                nameBytes = nameBytes.Slice(0, nullTerminator);
            }

            var name = encoding.GetString(nameBytes);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"UNKNOWN_FIELD_{fieldIndex}";
            }

            if (lowerCaseNames)
            {
                name = name.ToLowerInvariant();
            }

            // Use character field as fallback with reasonable defaults
            var length = fieldBytes[16];
            if (length == 0) length = 10; // Default length

            return new DbfField(
                name,
                FieldType.Character,
                0, // address
                length,
                0, // decimal count
                0, // reserved1
                0, // work area id
                0, // reserved2
                0, // reserved3
                0, // set fields flag
                0, // reserved4
                0 // index field flag
            );
        }
        catch
        {
            // If we can't even create a fallback field, skip it
            return null;
        }
    }

    /// <summary>
    /// Creates a field descriptor from a byte array
    /// </summary>
    /// <param name="bytes">The byte array containing the field descriptor data</param>
    /// <param name="encoding">The encoding to use for the field name</param>
    /// <param name="lowerCaseName">Whether to convert the field name to lowercase</param>
    /// <returns>The parsed field descriptor</returns>
    public static DbfField FromBytes(ReadOnlySpan<byte> bytes, Encoding encoding, bool lowerCaseName = false)
    {
        if (bytes.Length != Size)
            throw new ArgumentException($"Expected {Size} bytes for field descriptor, got {bytes.Length}",
                nameof(bytes));

        // Extract field name (first 11 bytes, null-terminated)
        var nameBytes = bytes.Slice(0, MaxNameLength);
        var nullTerminator = nameBytes.IndexOf((byte)0);
        if (nullTerminator >= 0)
        {
            nameBytes = nameBytes.Slice(0, nullTerminator);
        }

        var name = encoding.GetString(nameBytes);
        if (lowerCaseName)
        {
            name = name.ToLowerInvariant();
        }

        // Extract field type
        var typeChar = (char)bytes[11];

        // Handle null terminator or invalid field types more gracefully
        if (typeChar == '\0')
        {
            // This might be the field terminator, skip this field
            throw new ArgumentException("Encountered field terminator (null byte) in field definition");
        }

        // For unknown field types, we'll treat them as character fields and continue
        // This allows us to at least read the file structure
        var fieldType = FieldTypeExtensions.FromChar(typeChar) ?? FieldType.Character;

        // Extract other fields
        var address = BitConverter.ToUInt32(bytes.Slice(12, 4));
        var length = bytes[16];
        var decimalCount = bytes[17];
        var reserved1 = BitConverter.ToUInt16(bytes.Slice(18, 2));
        var workAreaId = bytes[20];
        var reserved2 = bytes[21];
        var reserved3 = bytes[22];
        var setFieldsFlag = bytes[23];

        // Reserved4 is 7 bytes, we'll store it as ulong (using first 7 bytes)
        var reserved4Bytes = new byte[8];
        bytes.Slice(24, 7).CopyTo(reserved4Bytes);
        var reserved4 = BitConverter.ToUInt64(reserved4Bytes);

        var indexFieldFlag = bytes[31];

        return new DbfField(
            name,
            fieldType,
            address,
            length,
            decimalCount,
            reserved1,
            workAreaId,
            reserved2,
            reserved3,
            setFieldsFlag,
            reserved4,
            indexFieldFlag);
    }

    /// <summary>
    /// Validates the field descriptor for common issues
    /// </summary>
    /// <param name="dbfVersion">The DBF version for context-specific validation</param>
    /// <exception cref="ArgumentException">Thrown when the field has invalid properties</exception>
    public void Validate(DbfVersion dbfVersion)
    {
        // Validate field name
        if (string.IsNullOrEmpty(Name))
            throw new ArgumentException("Field name cannot be null or empty");

        if (Name.Length > MaxNameLength)
            throw new ArgumentException($"Field name '{Name}' exceeds maximum length of {MaxNameLength}");

        // Validate field type specific constraints
        switch (Type)
        {
            case FieldType.Integer:
                if (ActualLength != 4)
                    throw new ArgumentException($"Integer field '{Name}' must have length 4, got {ActualLength}");
                break;

            case FieldType.Logical:
                if (ActualLength != 1)
                    throw new ArgumentException($"Logical field '{Name}' must have length 1, got {ActualLength}");
                break;

            case FieldType.Currency:
                if (ActualLength != 8)
                    throw new ArgumentException($"Currency field '{Name}' must have length 8, got {ActualLength}");
                break;

            case FieldType.Double:
                if (ActualLength != 8)
                    throw new ArgumentException($"Double field '{Name}' must have length 8, got {ActualLength}");
                break;

            case FieldType.Timestamp:
            case FieldType.TimestampAlternate:
                if (ActualLength != 8)
                    throw new ArgumentException($"Timestamp field '{Name}' must have length 8, got {ActualLength}");
                break;

            case FieldType.Date:
                if (ActualLength != 8)
                    throw new ArgumentException($"Date field '{Name}' must have length 8, got {ActualLength}");
                break;

            case FieldType.Character:
            case FieldType.Varchar:
                if (ActualLength == 0)
                    throw new ArgumentException($"Character field '{Name}' cannot have zero length");
                break;

            case FieldType.Numeric:
            case FieldType.Float:
                if (ActualLength == 0)
                    throw new ArgumentException($"Numeric field '{Name}' cannot have zero length");
                if (ActualDecimalCount > ActualLength)
                    throw new ArgumentException(
                        $"Decimal count ({ActualDecimalCount}) cannot exceed field length ({ActualLength}) for field '{Name}'");
                break;
        }

        // Validate memo field requirements
        if (UsesMemoFile && !dbfVersion.SupportsMemoFields())
        {
            throw new ArgumentException(
                $"Field '{Name}' of type {Type} requires memo file support, but DBF version {dbfVersion} does not support memo fields");
        }
    }

    /// <summary>
    /// Returns a string representation of the field
    /// </summary>
    public override string ToString()
    {
        var description = $"{Name} ({Type.GetDescription()}, {ActualLength}";

        if (Type == FieldType.Numeric || Type == FieldType.Float)
        {
            description += $", {ActualDecimalCount} decimals";
        }

        description += ")";

        if (UsesMemoFile)
        {
            description += " [Memo]";
        }

        return description;
    }

    /// <summary>
    /// Determines equality based on field properties
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is DbfField other && Equals(other);
    }

    /// <summary>
    /// Determines equality based on field properties
    /// </summary>
    public bool Equals(DbfField other)
    {
        return Name == other.Name &&
               Type == other.Type &&
               ActualLength == other.ActualLength &&
               ActualDecimalCount == other.ActualDecimalCount;
    }

    /// <summary>
    /// Gets a hash code based on field properties
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Type, ActualLength, ActualDecimalCount);
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(DbfField left, DbfField right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(DbfField left, DbfField right)
    {
        return !left.Equals(right);
    }
}