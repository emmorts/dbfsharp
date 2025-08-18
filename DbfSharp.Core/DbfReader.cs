using System.Collections;
using System.IO.MemoryMappedFiles;
using System.Text;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Exceptions;
using DbfSharp.Core.Memo;
using DbfSharp.Core.Parsing;
using DbfSharp.Core.Utils;

namespace DbfSharp.Core;

/// <summary>
/// Provides high-performance reading capabilities for dBASE (DBF) files with support for both
/// streaming and in-memory access patterns. This class handles various DBF format versions
/// (dBASE II through FoxPro) and provides comprehensive data access, field parsing, and
/// memo file integration while maintaining optimal memory usage and performance characteristics.
/// </summary>
/// <remarks>
/// <para>
/// Key features include robust error handling for malformed DBF files, automatic encoding detection,
/// memo file support (.DBT/.FPT), field validation with customizable parsing, and comprehensive
/// statistics reporting. The reader is designed to handle legacy DBF files that may not strictly
/// conform to format specifications while providing modern .NET integration through IEnumerable
/// patterns.
/// </para>
/// <para>
/// **Thread Safety**: This class is **not thread-safe**. Each thread should use its own DbfReader instance.
/// Do not share a single DbfReader instance across multiple threads without external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Streaming access for large files (recommended)
/// using var reader = DbfReader.Create("data.dbf");
/// foreach (var record in reader.Records)
/// {
///     Console.WriteLine(record["CustomerName"]);
/// }
///
/// // Loaded access for frequent operations and random access
/// using var reader = DbfReader.Create("data.dbf");
/// reader.Load(); // Load all records into memory
/// var statistics = reader.GetStatistics();
/// var firstRecord = reader[0]; // Random access is now available
/// </code>
/// </example>
public sealed class DbfReader : IDisposable, IEnumerable<DbfRecord>
{
    internal readonly Stream Stream;

    private readonly BinaryReader? _reader;
    private readonly Dictionary<string, int> _fieldIndexMap;
    private readonly bool _ownsStream;

    private readonly MemoryMappedFile? _memoryMappedFile;
    private readonly ChunkedMemoryMappedAccessor? _chunkedMemoryMappedAccessor;
    private readonly bool _useMemoryMapping;

    private DbfRecord[]? _loadedRecords;

    private EventHandler<DbfWarningEventArgs>? _warning;
    private bool _warningsRaised;

    /// <summary>
    /// Event raised when a warning condition is detected (e.g., duplicate field names)
    /// </summary>
    public event EventHandler<DbfWarningEventArgs>? Warning
    {
        add
        {
            _warning += value;
            if (_warningsRaised)
            {
                return;
            }

            _warningsRaised = true;
            RaiseDeferredWarnings();
        }
        remove => _warning -= value;
    }

    private readonly List<string>? _duplicateFields;
    private DbfRecord[]? _loadedDeletedRecords;
    private bool _disposed;

    private const uint EofMarker = 0x1A; // end of file marker in DBF files
    private string? MemoFilePath => MemoFile?.FilePath;

    /// <summary>
    /// Gets the DBF file header containing structural metadata and format information.
    /// </summary>
    public DbfHeader Header { get; }

    /// <summary>
    /// Gets the field definitions for all columns in the DBF file.
    /// </summary>
    public IReadOnlyList<DbfField> Fields { get; }

    /// <summary>
    /// Gets the names of all fields in the DBF file in their original order.
    /// </summary>
    public IReadOnlyList<string> FieldNames { get; }

    /// <summary>
    /// Gets a value indicating whether all records have been loaded into memory.
    /// </summary>
    /// <value>
    /// <c>true</c> if records are cached in memory for fast access; <c>false</c>
    /// if operating in streaming mode where records are read from disk on-demand.
    /// </value>
    public bool IsLoaded => _loadedRecords != null;

    /// <summary>
    /// Gets the total number of records in the DBF file as specified in the header.
    /// </summary>
    public int RecordCount => (int)Header.NumberOfRecords;

    /// <summary>
    /// Gets the character encoding used to interpret text data in the DBF file.
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// Gets the field parser used by this reader (internal access for SpanDbfRecord)
    /// </summary>
    internal IFieldParser FieldParser { get; }

    /// <summary>
    /// Gets the memo file associated with this reader (internal access for SpanDbfRecord)
    /// </summary>
    internal IMemoFile? MemoFile { get; }

    /// <summary>
    /// Gets the reader options (internal access for SpanDbfRecord)
    /// </summary>
    internal DbfReaderOptions Options { get; }

    /// <summary>
    /// Gets whether memory mapping is being used (internal access for SpanRecordEnumerable)
    /// </summary>
    internal bool UseMemoryMapping => _useMemoryMapping;

    /// <summary>
    /// Gets the chunked memory-mapped accessor (internal access for SpanRecordEnumerable)
    /// </summary>
    internal ChunkedMemoryMappedAccessor? ChunkedMemoryMappedAccessor =>
        _chunkedMemoryMappedAccessor;

    /// <summary>
    /// Gets the table name, typically derived from the filename.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets an enumerable collection of active (non-deleted) records.
    /// </summary>
    /// <remarks>
    /// In loaded mode, returns cached records for fast repeated access.
    /// In streaming mode, enumerates records directly from the file.
    /// This property respects the <see cref="DbfReaderOptions.SkipDeletedRecords"/> setting.
    /// In streaming mode, this enumeration can be slow if the file is large.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public IEnumerable<DbfRecord> Records
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(DbfReader));

            if (IsLoaded)
            {
                return _loadedRecords ?? Enumerable.Empty<DbfRecord>();
            }

            return EnumerateRecords(skipDeleted: Options.SkipDeletedRecords);
        }
    }

    /// <summary>
    /// Gets an enumerable collection of records marked as deleted.
    /// </summary>
    /// <remarks>
    /// In loaded mode, returns cached deleted records.
    /// In streaming mode, enumerates and filters deleted records from the file.
    /// In streaming mode, this enumeration can be slow if the file is large.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public IEnumerable<DbfRecord> DeletedRecords
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(DbfReader));

            if (IsLoaded)
            {
                return _loadedDeletedRecords ?? Enumerable.Empty<DbfRecord>();
            }

            return EnumerateRecords(skipDeleted: false, deletedOnly: true);
        }
    }

    /// <summary>
    /// Initializes a new instance of the DbfReader class. This constructor is private
    /// to enforce the use of the static factory methods <see cref="Create(string, DbfReaderOptions)"/>
    /// and <see cref="Create(string, DbfReaderOptions)"/>.
    /// </summary>
    private DbfReader(
        Stream stream,
        bool ownsStream,
        DbfHeader header,
        DbfField[] fields,
        DbfReaderOptions options,
        IMemoFile? memoFile,
        string tableName,
        MemoryMappedFile? memoryMappedFile = null,
        ChunkedMemoryMappedAccessor? chunkedMemoryMappedAccessor = null
    )
    {
        Stream = stream;
        _ownsStream = ownsStream;
        _memoryMappedFile = memoryMappedFile;
        _chunkedMemoryMappedAccessor = chunkedMemoryMappedAccessor;
        _useMemoryMapping = memoryMappedFile != null && chunkedMemoryMappedAccessor != null;

        if (stream.CanSeek)
        {
            _reader = new BinaryReader(
                stream,
                options.Encoding ?? header.Encoding,
                leaveOpen: true
            );
        }

        Header = header;
        Fields = fields;
        Options = options;
        MemoFile = memoFile;
        TableName = tableName;

        Encoding = options.Encoding ?? Header.Encoding;
        FieldParser = options.CustomFieldParser ?? new FieldParser(Header.DbfVersion);

        var fieldNames = new string[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            var name = fields[i].Name;
            if (options.EnableStringInterning)
            {
                name = string.Intern(name);
            }

            fieldNames[i] = name;
        }

        FieldNames = fieldNames;

        _fieldIndexMap = new Dictionary<string, int>(
            options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
        );
        var duplicateFields = new List<string>();

        for (var i = 0; i < fieldNames.Length; i++)
        {
            // only map the first occurrence of each field name to avoid overwriting duplicates
            if (!_fieldIndexMap.ContainsKey(fieldNames[i]))
            {
                _fieldIndexMap[fieldNames[i]] = i;
            }
            else
            {
                if (!duplicateFields.Contains(fieldNames[i]))
                {
                    duplicateFields.Add(fieldNames[i]);
                }
            }
        }

        // store duplicate fields for later warning (after event handlers can be attached)
        if (duplicateFields.Count > 0)
        {
            _duplicateFields = duplicateFields;
        }
    }

    /// <summary>
    /// Raises the Warning event
    /// </summary>
    /// <param name="e">The warning event arguments</param>
    private void OnWarning(DbfWarningEventArgs e)
    {
        _warning?.Invoke(this, e);
    }

    /// <summary>
    /// Checks for and raises warnings that were deferred during construction
    /// </summary>
    private void RaiseDeferredWarnings()
    {
        if (_duplicateFields is not { Count: > 0 })
        {
            return;
        }

        var duplicateList = string.Join(", ", _duplicateFields.Select(name => $"'{name}'"));
        var message =
            _duplicateFields.Count == 1
                ? $"Duplicate field name detected: {duplicateList}. Only the first occurrence will be accessible via FindField()."
                : $"Duplicate field names detected: {duplicateList}. Only the first occurrence of each will be accessible via FindField().";

        OnWarning(
            new DbfWarningEventArgs(
                message,
                DbfWarningType.DuplicateFieldNames,
                $"{_duplicateFields.Count} duplicate field name(s) found"
            )
        );
    }

    /// <summary>
    /// Creates a DbfReader from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the DBF file to open.</param>
    /// <param name="options">Optional reader configuration settings.</param>
    /// <returns>A new DbfReader instance.</returns>
    public static DbfReader Create(string filePath, DbfReaderOptions? options = null)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new DbfNotFoundException(filePath, $"DBF file not found: {filePath}");
        }

        options ??= new DbfReaderOptions();

        try
        {
            if (options.UseMemoryMapping)
            {
                return CreateMemoryMappedReader(filePath, options);
            }

            var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                options.BufferSize
            );
            var tableName = Path.GetFileNameWithoutExtension(filePath);
            return CreateFromStream(stream, true, options, tableName, filePath);
        }
        catch (Exception ex) when (ex is not DbfException)
        {
            throw new DbfException($"Failed to open DBF file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a DbfReader from a stream. The caller is responsible for disposing the stream.
    /// </summary>
    public static DbfReader Create(Stream stream, DbfReaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return CreateFromStream(stream, false, options, "Unknown", null);
    }

    /// <summary>
    /// Creates a memory-mapped DbfReader from the specified file path.
    /// </summary>
    private static DbfReader CreateMemoryMappedReader(string filePath, DbfReaderOptions options)
    {
        var fileInfo = new FileInfo(filePath);
        var tableName = Path.GetFileNameWithoutExtension(filePath);
        var fileSize = fileInfo.Length;

        var mmf = MemoryMappedFile.CreateFromFile(
            filePath,
            FileMode.Open,
            "DbfReader",
            0,
            MemoryMappedFileAccess.Read
        );

        var chunkedAccessor = new ChunkedMemoryMappedAccessor(mmf, fileSize);

        try
        {
            var header = ReadHeaderFromChunkedMemoryMap(chunkedAccessor);
            var fields = ReadFieldsFromChunkedMemoryMap(chunkedAccessor, header, options);

            if (fields.Length == 0 && header.HeaderLength > DbfHeader.Size + 1)
            {
                fields = ReadFieldsRobustFromChunkedMemoryMap(chunkedAccessor, header, options);
            }

            if (fields.Length == 0 && header.RecordLength == 0)
            {
                throw new DbfException(
                    "DBF file has no field definitions and invalid record length"
                );
            }

            if (header.DbfVersion == DbfVersion.DBase2)
            {
                header = RecalculateDBase2Header(header, fields);
            }

            IMemoFile? memoFile = null;
            if (header.SupportsMemoFields)
            {
                try
                {
                    memoFile = MemoFileFactory.CreateMemoFile(filePath, header.DbfVersion, options);
                }
                catch (MissingMemoFileException)
                {
                    if (!options.IgnoreMissingMemoFile)
                    {
                        throw;
                    }
                }
            }

            // create a dummy stream for compatibility (not used in memory-mapped mode)
            var dummyStream = new MemoryStream();

            return new DbfReader(
                dummyStream,
                false,
                header,
                fields,
                options,
                memoFile,
                tableName,
                mmf,
                chunkedAccessor
            );
        }
        catch
        {
            chunkedAccessor.Dispose();
            mmf.Dispose();
            throw;
        }
    }

    private static DbfReader CreateFromStream(
        Stream stream,
        bool ownsStream,
        DbfReaderOptions? options,
        string tableName,
        string? filePath
    )
    {
        options ??= new DbfReaderOptions();

        try
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException(
                    "Non-seekable streams are not supported. Consider using a seekable stream like FileStream or MemoryStream."
                );
            }

            var reader = new BinaryReader(
                stream,
                options.Encoding ?? Encoding.ASCII,
                leaveOpen: true
            );
            var header = DbfHeader.Read(reader);

            var fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : stream.Position;
            stream.Position = fieldsStartPosition;

            DbfField[] fields;
            try
            {
                fields = DbfField.ReadFields(
                    reader,
                    header.Encoding,
                    options.LowerCaseFieldNames,
                    header.DbfVersion
                );
                if (fields.Length == 0 && header.HeaderLength > DbfHeader.Size + 1)
                {
                    stream.Position = fieldsStartPosition;
                    fields = ReadFieldsRobust(reader, header, options);
                }
            }
            catch (Exception)
            {
                stream.Position = fieldsStartPosition;
                fields = ReadFieldsRobust(reader, header, options);
            }

            if (fields.Length == 0)
            {
                // zero fields is valid for some DBF files, but the record length should be minimal (typically 1 byte for deletion marker)
                if (header.RecordLength == 0)
                {
                    throw new DbfException(
                        "DBF file has no field definitions and invalid record length"
                    );
                }
            }

            if (header.DbfVersion == DbfVersion.DBase2)
            {
                header = RecalculateDBase2Header(header, fields);
            }

            IMemoFile? memoFile = null;
            if (header.SupportsMemoFields && filePath != null)
            {
                try
                {
                    memoFile = MemoFileFactory.CreateMemoFile(filePath, header.DbfVersion, options);
                }
                catch (MissingMemoFileException)
                {
                    if (!options.IgnoreMissingMemoFile)
                    {
                        throw;
                    }
                }
            }

            return new DbfReader(stream, ownsStream, header, fields, options, memoFile, tableName);
        }
        catch
        {
            if (ownsStream)
            {
                stream.Dispose();
            }

            throw;
        }
    }

    private static DbfHeader RecalculateDBase2Header(DbfHeader header, DbfField[] fields)
    {
        var calculatedRecordLength = 1 + fields.Sum(field => field.ActualLength); // incl. deletion flag
        var calculatedHeaderLength = (ushort)(8 + fields.Length * DbfField.DBase2Size);

        return new DbfHeader(
            header.DbVersionByte,
            header.Year,
            header.Month,
            header.Day,
            header.NumberOfRecords,
            calculatedHeaderLength,
            (ushort)calculatedRecordLength,
            header.Reserved1,
            header.IncompleteTransaction,
            header.EncryptionFlag,
            header.FreeRecordThread,
            header.Reserved2,
            header.Reserved3,
            header.MdxFlag,
            header.LanguageDriver,
            header.Reserved4
        );
    }

    /// <summary>
    /// Reads DBF header from chunked memory-mapped file
    /// </summary>
    private static DbfHeader ReadHeaderFromChunkedMemoryMap(ChunkedMemoryMappedAccessor accessor)
    {
        var headerBytes = new byte[DbfHeader.Size];
        accessor.ReadArray(0, headerBytes, 0, headerBytes.Length);

        using var stream = new MemoryStream(headerBytes);
        using var reader = new BinaryReader(stream);
        return DbfHeader.Read(reader);
    }

    /// <summary>
    /// Reads field definitions from chunked memory-mapped file
    /// </summary>
    private static DbfField[] ReadFieldsFromChunkedMemoryMap(
        ChunkedMemoryMappedAccessor accessor,
        DbfHeader header,
        DbfReaderOptions options
    )
    {
        var fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : DbfHeader.Size;

        return header.DbfVersion == DbfVersion.DBase2
            ? ReadDBase2FieldsFromChunkedMemoryMap(accessor, fieldsStartPosition, header, options)
            : ReadStandardFieldsFromChunkedMemoryMap(
                accessor,
                fieldsStartPosition,
                header,
                options
            );
    }

    private static DbfField[] ReadDBase2FieldsFromChunkedMemoryMap(
        ChunkedMemoryMappedAccessor accessor,
        long startPosition,
        DbfHeader header,
        DbfReaderOptions options
    )
    {
        var headerLength = header.HeaderLength;
        var dataLength = headerLength - startPosition;
        var buffer = new byte[dataLength];
        accessor.ReadArray(startPosition, buffer, 0, buffer.Length);

        using var stream = new MemoryStream(buffer);
        using var reader = new BinaryReader(stream, header.Encoding);

        return DbfField.ReadFields(
            reader,
            header.Encoding,
            options.LowerCaseFieldNames,
            header.DbfVersion
        );
    }

    private static DbfField[] ReadStandardFieldsFromChunkedMemoryMap(
        ChunkedMemoryMappedAccessor accessor,
        long startPosition,
        DbfHeader header,
        DbfReaderOptions options
    )
    {
        const int maxFields = 255; // DBF maximum fields
        const int maxDataLength = maxFields * DbfField.Size + 1; // +1 for terminator

        var remainingCapacity = accessor.Capacity - startPosition;
        var dataLength = Math.Min(maxDataLength, (int)remainingCapacity);

        var buffer = new byte[dataLength];
        accessor.ReadArray(startPosition, buffer, 0, buffer.Length);

        using var stream = new MemoryStream(buffer);
        using var reader = new BinaryReader(stream, header.Encoding);

        return DbfField.ReadFields(
            reader,
            header.Encoding,
            options.LowerCaseFieldNames,
            header.DbfVersion
        );
    }

    /// <summary>
    /// Robust field reading from chunked memory-mapped file for malformed DBF files
    /// </summary>
    private static DbfField[] ReadFieldsRobustFromChunkedMemoryMap(
        ChunkedMemoryMappedAccessor accessor,
        DbfHeader header,
        DbfReaderOptions options
    )
    {
        var fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : DbfHeader.Size;

        var maxDataLength = Math.Min(
            255 * DbfField.Size + 256,
            (int)(accessor.Capacity - fieldsStartPosition)
        );
        var buffer = new byte[maxDataLength];
        accessor.ReadArray(fieldsStartPosition, buffer, 0, buffer.Length);

        using var stream = new MemoryStream(buffer);
        using var reader = new BinaryReader(stream, header.Encoding);

        return ReadFieldsRobust(reader, header, options);
    }

    private static DbfField[] ReadFieldsRobust(
        BinaryReader reader,
        DbfHeader header,
        DbfReaderOptions options
    )
    {
        if (header.DbfVersion == DbfVersion.DBase2)
        {
            return DbfField.ReadFields(
                reader,
                header.Encoding,
                options.LowerCaseFieldNames,
                header.DbfVersion
            );
        }

        var fields = new List<DbfField>();
        while (fields.Count < 255)
        {
            if (reader.BaseStream.Position + DbfField.Size > reader.BaseStream.Length)
            {
                break;
            }

            var fieldBytes = reader.ReadBytes(DbfField.Size);
            if (fieldBytes[0] is 0x0D or 0x00 or 0x1A)
            {
                break;
            }

            try
            {
                var field = DbfField.FromBytes(
                    fieldBytes,
                    header.Encoding,
                    options.LowerCaseFieldNames
                );
                if (
                    string.IsNullOrWhiteSpace(field.Name)
                    || field.Name.Length > DbfField.MaxNameLength
                    || field.ActualLength == 0
                )
                {
                    break;
                }

                if (field.Name.Any(c => c < 32 || c > 126))
                {
                    break;
                }

                fields.Add(field);
            }
            catch (Exception)
            {
                break;
            }
        }

        return fields.ToArray();
    }

    /// <summary>
    /// Loads all records into memory for fast random access and repeated enumeration.
    /// </summary>
    public void Load()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(DbfReader));

        if (IsLoaded)
        {
            return;
        }

        var records = new List<DbfRecord>();
        var deletedRecords = new List<DbfRecord>();

        foreach (var (record, isDeleted) in EnumerateAllRecords())
        {
            if (isDeleted)
            {
                deletedRecords.Add(record);
            }
            else
            {
                records.Add(record);
            }
        }

        _loadedRecords = records.ToArray();
        _loadedDeletedRecords = deletedRecords.ToArray();
    }

    /// <summary>
    /// Unloads records from memory, returning the reader to streaming mode.
    /// </summary>
    public void Unload()
    {
        _loadedRecords = null;
        _loadedDeletedRecords = null;
    }

    /// <summary>
    /// Enumerates records using zero-allocation span-based access.
    /// Records cannot cross method boundaries due to ref struct limitations.
    /// </summary>
    /// <param name="skipDeleted">Whether to skip deleted records</param>
    /// <returns>An enumerable of span-based records</returns>
    public SpanRecordEnumerable EnumerateSpanRecords(bool skipDeleted = true)
    {
        return new SpanRecordEnumerable(this, skipDeleted);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the active records.
    /// </summary>
    public IEnumerator<DbfRecord> GetEnumerator()
    {
        return Records.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private IEnumerable<DbfRecord> EnumerateRecords(
        bool skipDeleted = true,
        bool deletedOnly = false
    )
    {
        var recordsRead = 0;
        var maxRecords = Options.MaxRecords ?? int.MaxValue;

        foreach (var (record, isDeleted) in EnumerateAllRecords())
        {
            if (recordsRead >= maxRecords)
            {
                break;
            }

            if (deletedOnly && !isDeleted)
            {
                continue;
            }

            if (skipDeleted && isDeleted)
            {
                continue;
            }

            yield return record;
            recordsRead++;
        }
    }

    private IEnumerable<(DbfRecord Record, bool IsDeleted)> EnumerateAllRecords()
    {
        if (_disposed)
        {
            yield break;
        }

        if (_useMemoryMapping)
        {
            foreach (var record in EnumerateMemoryMappedRecords())
            {
                yield return record;
            }
            yield break;
        }

        if (_reader == null)
        {
            yield break;
        }

        Stream.Position = Header.HeaderLength;
        var recordBuffer = new byte[Header.RecordLength];

        for (uint i = 0; i < Header.NumberOfRecords; i++)
        {
            var bytesRead = Stream.Read(recordBuffer, 0, recordBuffer.Length);
            if (bytesRead != recordBuffer.Length)
            {
                break;
            }

            if (recordBuffer[0] == EofMarker)
            {
                break;
            }

            yield return ParseRecord(recordBuffer);
        }
    }

    /// <summary>
    /// Enumerates records using chunked memory-mapped file access
    /// </summary>
    private IEnumerable<(DbfRecord Record, bool IsDeleted)> EnumerateMemoryMappedRecords()
    {
        if (_chunkedMemoryMappedAccessor == null)
        {
            yield break;
        }

        var recordLength = Header.RecordLength;
        var recordBuffer = new byte[recordLength];
        var startPosition = Header.HeaderLength;

        for (uint i = 0; i < Header.NumberOfRecords; i++)
        {
            var recordPosition = startPosition + i * recordLength;
            if (recordPosition + recordLength > _chunkedMemoryMappedAccessor.Capacity)
            {
                break;
            }

            _chunkedMemoryMappedAccessor.ReadArray(recordPosition, recordBuffer, 0, recordLength);

            if (recordBuffer[0] == EofMarker)
            {
                break;
            }

            yield return ParseRecord(recordBuffer);
        }
    }

    private (DbfRecord Record, bool IsDeleted) ParseRecord(byte[] recordBuffer)
    {
        var isDeleted = recordBuffer[0] == '*';
        var fieldValues = new object?[Fields.Count];
        var recordData = recordBuffer.AsSpan(1); // skip deletion flag

        // Visual FoxPro uses the Address field for field positioning, but only if addresses are meaningful
        var useAddressBasedParsing = Header.DbfVersion.IsVisualFoxPro() && HasMeaningfulAddresses();

        if (useAddressBasedParsing)
        {
            // address-based parsing: use field Address property for positioning
            for (var fieldIndex = 0; fieldIndex < Fields.Count; fieldIndex++)
            {
                var field = Fields[fieldIndex];
                var fieldLength = field.ActualLength;
                var fieldOffset = (int)field.Address - 1; // address is 1-based, convert to 0-based

                if (recordData.Length < fieldOffset + fieldLength)
                {
                    fieldValues[fieldIndex] = new InvalidValue(
                        Array.Empty<byte>(),
                        field,
                        "Record is shorter than expected for field address."
                    );
                    continue;
                }

                var fieldData = recordData.Slice(fieldOffset, fieldLength);

                try
                {
                    fieldValues[fieldIndex] = FieldParser.Parse(
                        field,
                        fieldData,
                        MemoFile,
                        Encoding,
                        Options
                    );
                }
                catch (Exception ex)
                {
                    var rawData = fieldData.ToArray();

                    if (Options.ValidateFields)
                    {
                        throw new FieldParseException(
                            field.Name,
                            field.Type.ToString(),
                            rawData,
                            $"Failed to parse field: {ex.Message}"
                        );
                    }

                    fieldValues[fieldIndex] = new InvalidValue(rawData, field, ex.Message);
                }
            }
        }
        else
        {
            // sequential parsing: fields are tightly packed
            var dataSpan = recordData;

            for (var fieldIndex = 0; fieldIndex < Fields.Count; fieldIndex++)
            {
                var field = Fields[fieldIndex];
                var fieldLength = field.ActualLength;

                if (dataSpan.Length < fieldLength)
                {
                    fieldValues[fieldIndex] = new InvalidValue(
                        Array.Empty<byte>(),
                        field,
                        "Record is shorter than expected."
                    );
                    continue;
                }

                var fieldData = dataSpan[..fieldLength];

                try
                {
                    fieldValues[fieldIndex] = FieldParser.Parse(
                        field,
                        fieldData,
                        MemoFile,
                        Encoding,
                        Options
                    );
                }
                catch (Exception ex)
                {
                    var rawData = fieldData.ToArray();

                    if (Options.ValidateFields)
                    {
                        throw new FieldParseException(
                            field.Name,
                            field.Type.ToString(),
                            rawData,
                            $"Failed to parse field: {ex.Message}"
                        );
                    }

                    fieldValues[fieldIndex] = new InvalidValue(rawData, field, ex.Message);
                }

                dataSpan = dataSpan[fieldLength..];
            }
        }

        var record = new DbfRecord(this, fieldValues);
        return (record, isDeleted);
    }

    internal bool HasMeaningfulAddresses()
    {
        // check if the field addresses form a valid sequential pattern
        // if addresses are 0 or don't form a proper sequence, fall back to sequential parsing

        if (Fields.Count == 0)
        {
            return false;
        }

        if (Fields.Any(f => f.Address == 0)) // Address 0 means no meaningful addresses
        {
            return false;
        }

        // check if addresses form a proper sequence (each field starts where the previous ends)
        uint expectedAddress = 1; // addresses are 1-based

        for (var i = 0; i < Fields.Count; i++)
        {
            if (Fields[i].Address != expectedAddress)
            {
                return false;
            }
            expectedAddress += (uint)Fields[i].ActualLength;
        }

        // ensure the total calculated record length matches header
        var calculatedRecordLength = (int)expectedAddress - 1; // -1 because addresses are 1-based
        if (calculatedRecordLength != Header.RecordLength - 1) // -1 for deletion flag
        {
            return false; // record length mismatch indicates addresses are unreliable
        }

        return true;
    }

    /// <summary>
    /// Gets the record at the specified index when operating in loaded mode.
    /// </summary>
    /// <param name="index">The zero-based index of the record to retrieve.</param>
    /// <returns>The DbfRecord at the specified index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when records are not loaded. Call Load() first.</exception>
    public DbfRecord this[int index]
    {
        get
        {
            if (!IsLoaded)
            {
                throw new InvalidOperationException(
                    "Records must be loaded to access by index. Call Load() first."
                );
            }

            if (_loadedRecords == null || index < 0 || index >= _loadedRecords.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _loadedRecords[index];
        }
    }

    /// <summary>
    /// Gets the number of active (non-deleted) records.
    /// </summary>
    public int Count => IsLoaded ? _loadedRecords?.Length ?? 0 : Records.Count();

    /// <summary>
    /// Gets the number of records marked as deleted.
    /// </summary>
    public int DeletedCount =>
        IsLoaded ? _loadedDeletedRecords?.Length ?? 0 : DeletedRecords.Count();

    /// <summary>
    /// Finds a field definition by name.
    /// </summary>
    public DbfField? FindField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return null;
        }

        return _fieldIndexMap.TryGetValue(fieldName, out var index) ? Fields[index] : null;
    }

    /// <summary>
    /// Gets the zero-based index of a field by name.
    /// </summary>
    public int GetFieldIndex(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return -1;
        }

        return _fieldIndexMap.GetValueOrDefault(fieldName, -1);
    }

    /// <summary>
    /// Determines whether the DBF file contains a field with the specified name.
    /// </summary>
    public bool HasField(string fieldName)
    {
        return GetFieldIndex(fieldName) >= 0;
    }

    /// <summary>
    /// Generates comprehensive statistics about the DBF file and current reader state.
    /// </summary>
    public DbfStatistics GetStatistics()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(DbfReader));

        return new DbfStatistics
        {
            TableName = TableName,
            DbfVersion = Header.DbfVersion,
            LastUpdateDate = Header.LastUpdateDate,
            TotalRecords = RecordCount,
            ActiveRecords = Count,
            DeletedRecords = DeletedCount,
            FieldCount = Fields.Count,
            RecordLength = Header.RecordLength,
            HeaderLength = Header.HeaderLength,
            HasMemoFile = MemoFile is { IsValid: true },
            MemoFilePath = MemoFilePath,
            Encoding = Encoding.EncodingName,
            IsLoaded = IsLoaded,
        };
    }

    /// <summary>
    /// Returns a string representation of the DBF reader's current state.
    /// </summary>
    public override string ToString()
    {
        return $"DbfReader: {TableName} ({Header.DbfVersion.GetDescription()}, {RecordCount} records)";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        MemoFile?.Dispose();
        _reader?.Dispose();
        _chunkedMemoryMappedAccessor?.Dispose();
        _memoryMappedFile?.Dispose();

        if (_ownsStream)
        {
            Stream.Dispose();
        }

        _loadedRecords = null;
        _loadedDeletedRecords = null;
        _disposed = true;
    }
}
