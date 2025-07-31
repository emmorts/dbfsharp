using System.Buffers;
using System.IO.Pipelines;
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
        byte indexFieldFlag
    )
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
            if (Type == FieldType.Character && Length == 0)
            {
                return 0;
            }

            return DecimalCount;
        }
    }

    /// <summary>
    /// Asynchronously reads field descriptors from a pipe reader.
    /// </summary>
    public static async ValueTask<DbfField[]> ReadFieldsAsync(
        PipeReader pipeReader,
        Encoding encoding,
        int fieldCount,
        bool lowerCaseNames = false,
        DbfVersion dbfVersion = DbfVersion.Unknown,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(pipeReader);
        ArgumentNullException.ThrowIfNull(encoding);

        if (dbfVersion == DbfVersion.DBase2)
        {
            return await ReadDBase2FieldsAsync(pipeReader, encoding, lowerCaseNames, cancellationToken);
        }

        var fields = new List<DbfField>();
        byte[]? rentedBuffer = null;

        try
        {
            rentedBuffer = ArrayPool<byte>.Shared.Rent(Size);

            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                var position = buffer.Start;

                if (buffer.IsEmpty && result.IsCompleted)
                {
                    break;
                }

                var terminator = buffer.PositionOf((byte)0x0D);
                if (terminator != null)
                {
                    buffer = buffer.Slice(0, terminator.Value);
                }

                while (buffer.Length >= Size)
                {
                    var fieldSequence = buffer.Slice(0, Size);

                    if (fieldSequence.FirstSpan[0] == 0x0D || fieldSequence.FirstSpan[0] == 0x1A)
                    {
                        pipeReader.AdvanceTo(fieldSequence.Start);
                        return fields.ToArray();
                    }

                    DbfField field;
                    if (fieldSequence.IsSingleSegment)
                    {
                        field = FromBytes(fieldSequence.FirstSpan, encoding, lowerCaseNames);
                    }
                    else
                    {
                        var fieldBytes = rentedBuffer.AsSpan(0, Size);
                        fieldSequence.CopyTo(fieldBytes);
                        field = FromBytes(fieldBytes, encoding, lowerCaseNames);
                    }

                    if (string.IsNullOrWhiteSpace(field.Name) || field.ActualLength == 0)
                    {
                        pipeReader.AdvanceTo(fieldSequence.Start);
                        return fields.ToArray();
                    }

                    fields.Add(field);
                    buffer = buffer.Slice(Size);
                    position = fieldSequence.End;
                }

                pipeReader.AdvanceTo(position, result.Buffer.End);

                if (result.IsCompleted || terminator != null)
                {
                    break;
                }
            }

            return fields.ToArray();
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Asynchronously reads field descriptors from a stream.
    /// </summary>
    public static async ValueTask<DbfField[]> ReadFieldsAsync(
        Stream stream,
        Encoding encoding,
        int fieldCount,
        bool lowerCaseNames = false,
        DbfVersion dbfVersion = DbfVersion.Unknown,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        if (dbfVersion == DbfVersion.DBase2)
        {
            return await ReadDBase2FieldsAsync(stream, encoding, lowerCaseNames, cancellationToken);
        }

        var fields = new List<DbfField>();
        var firstByteBuf = new byte[1];
        var fieldBytes = new byte[Size];

        while (true)
        {
            var currentPosition = stream.Position;
            var bytesRead = await stream.ReadAsync(firstByteBuf.AsMemory(0, 1), cancellationToken);

            if (bytesRead == 0 || firstByteBuf[0] == 0x0D || firstByteBuf[0] == 0x1A)
            {
                break;
            }

            fieldBytes[0] = firstByteBuf[0];
            await stream.ReadExactlyAsync(
                new Memory<byte>(fieldBytes, 1, Size - 1),
                cancellationToken
            );

            try
            {
                var field = FromBytes(fieldBytes, encoding, lowerCaseNames);
                if (string.IsNullOrWhiteSpace(field.Name) || field.ActualLength == 0)
                {
                    stream.Position = currentPosition;
                    break;
                }
                fields.Add(field);
            }
            catch (ArgumentException)
            {
                stream.Position = currentPosition;
                break;
            }
            catch (Exception)
            {
                stream.Position = currentPosition;
                break;
            }

            if (fields.Count >= 255)
            {
                break;
            }
        }

        return fields.ToArray();
    }

    /// <summary>
    /// Reads field descriptors from a binary reader
    /// </summary>
    public static DbfField[] ReadFields(
        BinaryReader reader,
        Encoding encoding,
        int fieldCount,
        bool lowerCaseNames = false,
        DbfVersion dbfVersion = DbfVersion.Unknown
    )
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(encoding);

        if (dbfVersion == DbfVersion.DBase2)
        {
            return ReadDBase2Fields(reader, encoding, lowerCaseNames);
        }

        var fields = new List<DbfField>();

        while (true)
        {
            var currentPosition = reader.BaseStream.Position;
            var firstByte = reader.ReadByte();

            if (firstByte == 0x0D || firstByte is 0x00 or 0x1A)
            {
                break;
            }

            var remainingBytes = reader.ReadBytes(Size - 1);
            if (remainingBytes.Length != Size - 1)
            {
                reader.BaseStream.Position = currentPosition;
                break;
            }

            var fieldBytes = new byte[Size];
            fieldBytes[0] = firstByte;
            Array.Copy(remainingBytes, 0, fieldBytes, 1, remainingBytes.Length);

            try
            {
                var field = FromBytes(fieldBytes, encoding, lowerCaseNames);

                if (string.IsNullOrWhiteSpace(field.Name) || field.ActualLength == 0)
                {
                    reader.BaseStream.Position = currentPosition;
                    break;
                }

                fields.Add(field);
            }
            catch (ArgumentException)
            {
                reader.BaseStream.Position = currentPosition;
                break;
            }
            catch (Exception)
            {
                reader.BaseStream.Position = currentPosition;
                break;
            }

            if (fields.Count >= 255)
            {
                break;
            }
        }

        return fields.ToArray();
    }

    private static async ValueTask<DbfField[]> ReadDBase2FieldsAsync(
        PipeReader pipeReader,
        Encoding encoding,
        bool lowerCaseNames,
        CancellationToken cancellationToken
    )
    {
        var fields = new List<DbfField>();
        byte[]? rentedBuffer = null;

        try
        {
            rentedBuffer = ArrayPool<byte>.Shared.Rent(DBase2Size);

            while (fields.Count < 128)
            {
                var result = await pipeReader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (buffer.Length < DBase2Size)
                {
                    if (result.IsCompleted)
                    {
                        break;
                    }

                    pipeReader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }

                var fieldSequence = buffer.Slice(0, DBase2Size);
                if (fieldSequence.IsSingleSegment)
                {
                    if (!IsValidDBase2Field(fieldSequence.FirstSpan))
                    {
                        pipeReader.AdvanceTo(buffer.Start);
                        break;
                    }
                    fields.Add(FromDBase2Bytes(fieldSequence.FirstSpan, encoding, lowerCaseNames));
                }
                else
                {
                    var fieldBytes = rentedBuffer.AsSpan(0, DBase2Size);
                    fieldSequence.CopyTo(fieldBytes);
                    if (!IsValidDBase2Field(fieldBytes))
                    {
                        pipeReader.AdvanceTo(buffer.Start);
                        break;
                    }
                    fields.Add(FromDBase2Bytes(fieldBytes, encoding, lowerCaseNames));
                }

                pipeReader.AdvanceTo(fieldSequence.End);
            }

            return fields.ToArray();
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private static async ValueTask<DbfField[]> ReadDBase2FieldsAsync(
        Stream stream,
        Encoding encoding,
        bool lowerCaseNames,
        CancellationToken cancellationToken
    )
    {
        var fields = new List<DbfField>();
        var fieldBytes = new byte[DBase2Size];

        while (true)
        {
            var currentPosition = stream.Position;
            var bytesRead = await stream.ReadAsync(fieldBytes.AsMemory(0, DBase2Size), cancellationToken);

            if (bytesRead != DBase2Size)
            {
                stream.Position = currentPosition;
                break;
            }

            if (!IsValidDBase2Field(fieldBytes))
            {
                stream.Position = currentPosition;
                break;
            }

            try
            {
                var field = FromDBase2Bytes(fieldBytes, encoding, lowerCaseNames);
                fields.Add(field);
            }
            catch (Exception)
            {
                stream.Position = currentPosition;
                break;
            }

            if (fields.Count >= 128)
            {
                break;
            }
        }

        return fields.ToArray();
    }

    private static DbfField[] ReadDBase2Fields(
        BinaryReader reader,
        Encoding encoding,
        bool lowerCaseNames
    )
    {
        var fields = new List<DbfField>();

        while (true)
        {
            var currentPosition = reader.BaseStream.Position;

            if (reader.BaseStream.Position + DBase2Size > reader.BaseStream.Length)
            {
                break;
            }

            var fieldBytes = reader.ReadBytes(DBase2Size);
            if (fieldBytes.Length != DBase2Size)
            {
                reader.BaseStream.Position = currentPosition;
                break;
            }

            if (!IsValidDBase2Field(fieldBytes))
            {
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
                reader.BaseStream.Position = currentPosition;
                break;
            }

            if (fields.Count >= 128)
            {
                break;
            }
        }

        return fields.ToArray();
    }

    private static bool IsValidDBase2Field(ReadOnlySpan<byte> bytes)
    {
        var nameBytes = bytes[..DBase2MaxNameLength];
        var nullTerminator = nameBytes.IndexOf((byte)0);
        if (nullTerminator >= 0)
        {
            nameBytes = nameBytes[..nullTerminator];
        }

        if (nameBytes.Length == 0)
        {
            return false;
        }

        foreach (var b in nameBytes)
        {
            if (b is < 32 or > 126)
            {
                return false;
            }
        }

        var typeChar = (char)bytes[11];
        var fieldType = FieldTypeExtensions.FromChar(typeChar);
        var length = bytes[12];

        return fieldType != null && length > 0;
    }

    private static DbfField FromDBase2Bytes(
        ReadOnlySpan<byte> bytes,
        Encoding encoding,
        bool lowerCaseName
    )
    {
        if (bytes.Length != DBase2Size)
        {
            throw new ArgumentException(
                $"Expected {DBase2Size} bytes for dBASE II field descriptor, got {bytes.Length}"
            );
        }

        var nameBytes = bytes[..DBase2MaxNameLength];
        var nullTerminator = nameBytes.IndexOf((byte)0);
        if (nullTerminator >= 0)
        {
            nameBytes = nameBytes[..nullTerminator];
        }

        var name = encoding.GetString(nameBytes);
        if (lowerCaseName)
        {
            name = name.ToLowerInvariant();
        }

        var typeChar = (char)bytes[11];
        var fieldType = FieldTypeExtensions.FromChar(typeChar) ?? FieldType.Character;
        var length = bytes[12];
        var decimalCount = bytes[13];

        return new DbfField(name, fieldType, 0, length, decimalCount, 0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Creates a DbfField instance from a byte array
    /// </summary>
    /// <param name="bytes">The byte array containing the field descriptor</param>
    /// <param name="encoding">The encoding to use for the field name</param>
    /// <param name="lowerCaseName">Whether to convert the field name to lower case</param>
    /// <returns>A DbfField instance</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the byte array length is not equal to the expected size
    /// or if a null terminator is encountered in the field definition
    /// </exception>
    public static DbfField FromBytes(
        ReadOnlySpan<byte> bytes,
        Encoding encoding,
        bool lowerCaseName = false
    )
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"Expected {Size} bytes for field descriptor, got {bytes.Length}",
                nameof(bytes)
            );
        }

        var nameBytes = bytes[..MaxNameLength];
        var nullTerminator = nameBytes.IndexOf((byte)0);
        if (nullTerminator >= 0)
        {
            nameBytes = nameBytes[..nullTerminator];
        }

        var name = encoding.GetString(nameBytes);
        if (lowerCaseName)
        {
            name = name.ToLowerInvariant();
        }

        var typeChar = (char)bytes[11];
        if (typeChar == '\0')
        {
            throw new ArgumentException(
                "Encountered field terminator (null byte) in field definition"
            );
        }

        var fieldType = FieldTypeExtensions.FromChar(typeChar) ?? FieldType.Character;

        var address = BitConverter.ToUInt32(bytes.Slice(12, 4));
        var length = bytes[16];
        var decimalCount = bytes[17];
        var reserved1 = BitConverter.ToUInt16(bytes.Slice(18, 2));
        var workAreaId = bytes[20];
        var reserved2 = bytes[21];
        var reserved3 = bytes[22];
        var setFieldsFlag = bytes[23];

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
            indexFieldFlag
        );
    }

    /// <summary>
    /// Validates the field against the specified DBF version
    /// </summary>
    /// <param name="dbfVersion">The DBF version to validate against</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the field name is invalid, length is incorrect for the type, or memo file usage is not supported
    /// </exception>
    public void Validate(DbfVersion dbfVersion)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new ArgumentException("Field name cannot be null or empty");
        }

        if (Name.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Field name '{Name}' exceeds maximum length of {MaxNameLength}"
            );
        }

        switch (Type)
        {
            case FieldType.Integer:
                if (ActualLength != 4)
                {
                    throw new ArgumentException(
                        $"Integer field '{Name}' must have length 4, got {ActualLength}"
                    );
                }

                break;
            case FieldType.Logical:
                if (ActualLength != 1)
                {
                    throw new ArgumentException(
                        $"Logical field '{Name}' must have length 1, got {ActualLength}"
                    );
                }

                break;
            case FieldType.Currency:
            case FieldType.Double:
            case FieldType.Timestamp:
            case FieldType.TimestampAlternate:
            case FieldType.Date:
                if (ActualLength != 8)
                {
                    throw new ArgumentException(
                        $"{Type} field '{Name}' must have length 8, got {ActualLength}"
                    );
                }

                break;
            case FieldType.Character:
            case FieldType.Varchar:
            case FieldType.Numeric:
            case FieldType.Float:
                if (ActualLength == 0)
                {
                    throw new ArgumentException($"{Type} field '{Name}' cannot have zero length");
                }

                if (
                    Type is FieldType.Numeric or FieldType.Float
                    && ActualDecimalCount > ActualLength
                )
                {
                    throw new ArgumentException(
                        $"Decimal count ({ActualDecimalCount}) cannot exceed field length ({ActualLength}) for field '{Name}'"
                    );
                }

                break;
        }

        if (UsesMemoFile && !dbfVersion.SupportsMemoFields())
        {
            throw new ArgumentException(
                $"Field '{Name}' of type {Type} requires memo file support, but DBF version {dbfVersion} does not support memo fields"
            );
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var description = $"{Name} ({Type.GetDescription()}, {ActualLength}";

        if (Type is FieldType.Numeric or FieldType.Float)
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
    ///  Checks if this field is equal to another DbfField instance
    /// </summary>
    /// <param name="obj">The object to compare with</param>
    /// <returns>True if the fields are equal, false otherwise</returns>
    public override bool Equals(object? obj)
    {
        return obj is DbfField other && Equals(other);
    }

    private bool Equals(DbfField other)
    {
        return Name == other.Name
            && Type == other.Type
            && ActualLength == other.ActualLength
            && ActualDecimalCount == other.ActualDecimalCount;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Type, ActualLength, ActualDecimalCount);
    }

    /// <summary>
    /// Overloaded equality operator to compare two DbfField instances
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>True if both fields are equal, false otherwise</returns>
    public static bool operator ==(DbfField left, DbfField right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Overloaded inequality operator to compare two DbfField instances
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>True if both fields are not equal, false otherwise</returns>
    public static bool operator !=(DbfField left, DbfField right)
    {
        return !left.Equals(right);
    }
}
