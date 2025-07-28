using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Exceptions;
using DbfSharp.Core.Memo;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Core;

/// <summary>
/// Provides high-performance reading capabilities for dBASE (DBF) files with support for both
/// streaming and in-memory access patterns. This class handles various DBF format versions
/// (dBASE II through FoxPro) and provides comprehensive data access, field parsing, and
/// memo file integration while maintaining optimal memory usage and performance characteristics.
/// </summary>
/// <remarks>
/// <para>
/// The DbfReader supports two primary access modes:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Streaming Mode</strong>: Records are read on-demand from disk, providing minimal memory footprint for large files</description></item>
/// <item><description><strong>Loaded Mode</strong>: All records are loaded into memory for fast random access and repeated enumeration</description></item>
/// </list>
/// <para>
/// Key features include robust error handling for malformed DBF files, automatic encoding detection,
/// memo file support (.DBT/.FPT), field validation with customizable parsing, and comprehensive
/// statistics reporting. The reader is designed to handle legacy DBF files that may not strictly
/// conform to format specifications while providing modern .NET integration through IEnumerable
/// and async/await patterns.
/// </para>
/// <para>
/// Thread safety: This class is not thread-safe. Each thread should use its own DbfReader instance
/// or external synchronization must be provided.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Streaming access for large files
/// using var reader = DbfReader.Open("data.dbf");
/// foreach (var record in reader.Records)
/// {
///     Console.WriteLine(record["CustomerName"]);
/// }
///
/// // Loaded access for frequent operations
/// using var reader = DbfReader.Open("data.dbf", new DbfReaderOptions { LoadOnOpen = true });
/// var statistics = reader.GetStatistics();
/// var firstRecord = reader[0]; // Random access available when loaded
/// </code>
/// </example>
public sealed class DbfReader : IDisposable, IEnumerable<DbfRecord>
{
    private readonly Stream _stream;
    private readonly BinaryReader? _reader;
    private readonly DbfReaderOptions _options;
    private readonly IMemoFile? _memoFile;
    private readonly IFieldParser _fieldParser;
    private readonly Dictionary<string, int>? _fieldIndexMap;
    private readonly bool _ownsStream;

    private DbfRecord[]? _loadedRecords;
    private DbfRecord[]? _loadedDeletedRecords;
    private bool _disposed;

    /// <summary>
    /// Gets the DBF file header containing structural metadata and format information.
    /// </summary>
    /// <value>
    /// The parsed header information including version, record counts, field definitions,
    /// and last update timestamp.
    /// </value>
    public DbfHeader Header { get; }

    /// <summary>
    /// Gets the field definitions for all columns in the DBF file.
    /// </summary>
    /// <value>
    /// A read-only collection of field descriptors containing name, type, length,
    /// and precision information for each column in the table structure.
    /// </value>
    public IReadOnlyList<DbfField> Fields { get; }

    /// <summary>
    /// Gets the names of all fields in the DBF file in their original order.
    /// </summary>
    /// <value>
    /// A read-only collection of field names, optionally processed through string
    /// interning for memory optimization when enabled in reader options.
    /// </value>
    public IReadOnlyList<string> FieldNames { get; }

    /// <summary>
    /// Gets the file path to the associated memo file, if present.
    /// </summary>
    /// <value>
    /// The full path to the memo file (.DBT or .FPT) containing variable-length
    /// text data, or null if no memo file is associated with this DBF file.
    /// </value>
    public string? MemoFilePath => _memoFile?.FilePath;

    /// <summary>
    /// Gets a value indicating whether all records have been loaded into memory.
    /// </summary>
    /// <value>
    /// <c>true</c> if records are cached in memory for fast access; <c>false</c>
    /// if operating in streaming mode where records are read from disk on-demand.
    /// Affects performance characteristics and available operations.
    /// </value>
    public bool IsLoaded => _loadedRecords != null;

    /// <summary>
    /// Gets the total number of records in the DBF file as specified in the header.
    /// </summary>
    /// <value>
    /// The record count from the DBF header, which may include both active and
    /// deleted records. This represents the physical record count in the file.
    /// </value>
    public int RecordCount => (int)Header.NumberOfRecords;

    /// <summary>
    /// Gets the character encoding used to interpret text data in the DBF file.
    /// </summary>
    /// <value>
    /// The encoding determined from the DBF header's language driver or explicitly
    /// specified in reader options. Used for converting byte data to strings.
    /// </value>
    public Encoding Encoding { get; }

    /// <summary>
    /// Gets the table name, typically derived from the filename.
    /// </summary>
    /// <value>
    /// The table identifier, usually the filename without extension, or "Unknown"
    /// when reading from a stream without file context.
    /// </value>
    public string TableName { get; }

    /// <summary>
    /// Gets an enumerable collection of active (non-deleted) records.
    /// </summary>
    /// <value>
    /// In loaded mode, returns cached records for fast repeated access.
    /// In streaming mode, enumerates records directly from the file.
    /// Respects the SkipDeletedRecords option setting.
    /// </value>
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

            return EnumerateRecords(skipDeleted: _options.SkipDeletedRecords);
        }
    }

    /// <summary>
    /// Gets an enumerable collection of records marked as deleted.
    /// </summary>
    /// <value>
    /// In loaded mode, returns cached deleted records.
    /// In streaming mode, enumerates and filters deleted records from the file.
    /// Useful for data recovery or audit purposes.
    /// </value>
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
    /// Initializes a new instance of the DbfReader class with the specified stream and configuration.
    /// </summary>
    /// <remarks>
    /// This constructor is private and used internally by the static factory methods.
    /// It performs final initialization after header and field parsing is complete.
    /// </remarks>
    private DbfReader(
        Stream stream,
        bool ownsStream,
        DbfHeader header,
        DbfField[] fields,
        DbfReaderOptions options,
        IMemoFile? memoFile,
        string tableName)
    {
        _stream = stream;
        _ownsStream = ownsStream;
        if (stream.CanSeek)
        {
            _reader = new BinaryReader(stream, options.Encoding ?? header.Encoding, leaveOpen: true);
        }

        Header = header;
        Fields = fields;
        _options = options;
        _memoFile = memoFile;
        TableName = tableName;

        Encoding = options.Encoding ?? Header.Encoding;
        _fieldParser = options.CustomFieldParser ?? new FieldParser(Header.DbfVersion);

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

        _fieldIndexMap =
            new Dictionary<string, int>(options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        for (var i = 0; i < fieldNames.Length; i++)
        {
            _fieldIndexMap[fieldNames[i]] = i;
        }

        if (options.LoadOnOpen)
        {
            Load();
        }
    }

    /// <summary>
    /// Opens a DBF file from the specified file path with optional configuration.
    /// </summary>
    /// <param name="filePath">The path to the DBF file to open.</param>
    /// <param name="options">Optional reader configuration settings.</param>
    /// <returns>A new DbfReader instance configured for the specified file.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    /// <exception cref="DbfNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="DbfException">Thrown when the file cannot be opened or parsed.</exception>
    /// <remarks>
    /// This method automatically detects memo files (.DBT/.FPT) in the same directory
    /// and associates them with the DBF file when present. The returned reader owns
    /// the underlying file stream and will dispose it when the reader is disposed.
    /// </remarks>
    public static DbfReader Open(string filePath, DbfReaderOptions? options = null)
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
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, options.BufferSize);
            var tableName = Path.GetFileNameWithoutExtension(filePath);

            return CreateFromStream(stream, true, options, tableName, filePath);
        }
        catch (Exception ex) when (ex is not DbfException)
        {
            throw new DbfException($"Failed to open DBF file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Opens a DBF file from the specified stream with optional configuration.
    /// </summary>
    /// <param name="stream">The stream containing DBF file data.</param>
    /// <param name="options">Optional reader configuration settings.</param>
    /// <returns>A new DbfReader instance configured for the specified stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="DbfException">Thrown when the stream content cannot be parsed as a DBF file.</exception>
    /// <remarks>
    /// When reading from a stream, memo file detection is not available since there's
    /// no file system context. The reader does not own the stream and will not dispose
    /// it - the caller remains responsible for stream lifecycle management.
    /// </remarks>
    public static DbfReader Open(Stream stream, DbfReaderOptions? options = null)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }

        options ??= new DbfReaderOptions();

        return CreateFromStream(stream, false, options, "Unknown", null);
    }

    /// <summary>
    /// Asynchronously opens a DBF file from the specified file path with optional configuration.
    /// </summary>
    /// <param name="filePath">The path to the DBF file to open.</param>
    /// <param name="options">Optional reader configuration settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns a configured DbfReader instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    /// <exception cref="DbfNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="DbfException">Thrown when the file cannot be opened or parsed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// This method provides async I/O for the initial file parsing operations, making it
    /// suitable for use in async contexts where blocking I/O should be avoided.
    /// </remarks>
    public static async Task<DbfReader> OpenAsync(string filePath, DbfReaderOptions? options = null,
        CancellationToken cancellationToken = default)
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
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, options.BufferSize,
                useAsync: true);
            var tableName = Path.GetFileNameWithoutExtension(filePath);

            return await CreateFromStreamAsync(stream, true, options, tableName, filePath, cancellationToken);
        }
        catch (Exception ex) when (ex is not DbfException)
        {
            throw new DbfException($"Failed to open DBF file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously opens a DBF file from the specified stream with optional configuration.
    /// </summary>
    /// <param name="stream">The stream containing DBF file data.</param>
    /// <param name="options">Optional reader configuration settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns a configured DbfReader instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="DbfException">Thrown when the stream content cannot be parsed as a DBF file.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public static async Task<DbfReader> OpenAsync(Stream stream, DbfReaderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }

        options ??= new DbfReaderOptions();

        return await CreateFromStreamAsync(stream, false, options, "Unknown", null, cancellationToken);
    }

    private static DbfReader CreateFromStream(Stream stream, bool ownsStream, DbfReaderOptions options,
        string tableName, string? filePath)
    {
        try
        {
            var reader = new BinaryReader(stream, options.Encoding ?? Encoding.ASCII, leaveOpen: true);
            var header = DbfHeader.Read(reader);

            var fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : stream.Position;
            stream.Position = fieldsStartPosition;

            DbfField[] fields;
            try
            {
                fields = DbfField.ReadFields(reader, header.Encoding, header.FieldCount, options.LowerCaseFieldNames,
                    header.DbfVersion);
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
                throw new DbfException("No valid field definitions found in DBF file");
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

    private static async Task<DbfReader> CreateFromStreamAsync(Stream stream, bool ownsStream, DbfReaderOptions options,
        string tableName, string? filePath, CancellationToken cancellationToken)
    {
        try
        {
            var header = await DbfHeader.ReadAsync(stream, cancellationToken);

            if (stream.CanSeek)
            {
                var fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : stream.Position;
                stream.Position = fieldsStartPosition;
            }

            DbfField[] fields;
            try
            {
                fields = await DbfField.ReadFieldsAsync(stream, header.Encoding, header.FieldCount,
                    options.LowerCaseFieldNames, header.DbfVersion, cancellationToken);
                if (fields.Length == 0 && header.HeaderLength > DbfHeader.Size + 1)
                {
                    if (!stream.CanSeek)
                    {
                        throw new InvalidOperationException("Cannot re-read fields from a non-seekable stream.");
                    }

                    long fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : DbfHeader.Size;
                    stream.Position = fieldsStartPosition;
                    fields = await ReadFieldsRobustAsync(stream, header, options, cancellationToken);
                }
            }
            catch (Exception)
            {
                if (!stream.CanSeek)
                {
                    throw new InvalidOperationException("Cannot re-read fields from a non-seekable stream.");
                }

                long fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : DbfHeader.Size;
                stream.Position = fieldsStartPosition;
                fields = await ReadFieldsRobustAsync(stream, header, options, cancellationToken);
            }

            if (fields.Length == 0)
            {
                throw new DbfException("No valid field definitions found in DBF file");
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
                await stream.DisposeAsync();
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

    private static DbfField[] ReadFieldsRobust(BinaryReader reader, DbfHeader header, DbfReaderOptions options)
    {
        if (header.DbfVersion == DbfVersion.DBase2)
        {
            return DbfField.ReadFields(reader, header.Encoding, -1, options.LowerCaseFieldNames, header.DbfVersion);
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
                var field = DbfField.FromBytes(fieldBytes, header.Encoding, options.LowerCaseFieldNames);
                if (string.IsNullOrWhiteSpace(field.Name) || field.Name.Length > DbfField.MaxNameLength ||
                    field.ActualLength == 0)
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

    private static async Task<DbfField[]> ReadFieldsRobustAsync(Stream stream, DbfHeader header,
        DbfReaderOptions options, CancellationToken cancellationToken)
    {
        if (header.DbfVersion == DbfVersion.DBase2)
        {
            return await DbfField.ReadFieldsAsync(stream, header.Encoding, -1, options.LowerCaseFieldNames,
                header.DbfVersion, cancellationToken);
        }

        var fields = new List<DbfField>();
        var fieldBytes = new byte[DbfField.Size];
        var fieldMemory = new Memory<byte>(fieldBytes);

        while (fields.Count < 255)
        {
            if (stream.Position + DbfField.Size > stream.Length)
            {
                break;
            }

            await stream.ReadExactlyAsync(fieldMemory, cancellationToken);
            if (fieldBytes[0] is 0x0D or 0x00 or 0x1A)
            {
                break;
            }

            try
            {
                var field = DbfField.FromBytes(fieldBytes, header.Encoding, options.LowerCaseFieldNames);
                if (string.IsNullOrWhiteSpace(field.Name) || field.Name.Length > DbfField.MaxNameLength ||
                    field.ActualLength == 0)
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
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <remarks>
    /// After calling this method, record access becomes significantly faster but memory
    /// usage increases proportionally to the file size. Records remain loaded until
    /// Unload() is called or the reader is disposed. This operation is idempotent -
    /// calling it multiple times has no additional effect.
    /// </remarks>
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
    /// Asynchronously loads all records into memory for fast random access and repeated enumeration.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the loading operation.</param>
    /// <returns>A task representing the asynchronous loading operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// This method provides the same functionality as Load() but with async I/O support,
    /// making it suitable for use in async contexts where blocking I/O should be avoided.
    /// </remarks>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(DbfReader));

        if (IsLoaded)
        {
            return;
        }

        var records = new List<DbfRecord>();
        var deletedRecords = new List<DbfRecord>();

        await foreach (var (record, isDeleted) in EnumerateAllRecordsAsync(cancellationToken))
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
    /// <remarks>
    /// This method frees memory used by cached records and switches the reader back
    /// to streaming mode where records are read on-demand. Useful for reducing memory
    /// usage when random access is no longer needed.
    /// </remarks>
    public void Unload()
    {
        _loadedRecords = null;
        _loadedDeletedRecords = null;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the active records.
    /// </summary>
    /// <returns>An enumerator for the Records collection.</returns>
    public IEnumerator<DbfRecord> GetEnumerator()
    {
        return Records.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<DbfRecord> EnumerateRecords(bool skipDeleted = true, bool deletedOnly = false)
    {
        var recordsRead = 0;
        var maxRecords = _options.MaxRecords ?? int.MaxValue;

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
        if (_disposed || _reader == null)
        {
            yield break;
        }

        _stream.Position = Header.HeaderLength;

        var recordBuffer = new byte[Header.RecordLength];

        for (uint i = 0; i < Header.NumberOfRecords; i++)
        {
            var bytesRead = _stream.Read(recordBuffer, 0, recordBuffer.Length);
            if (bytesRead != recordBuffer.Length)
            {
                break;
            }

            if (recordBuffer[0] == 0x1A)
            {
                break;
            }

            yield return ParseRecord(recordBuffer);
        }
    }

    /// <summary>
    /// Asynchronously reads active records from the DBF file.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>An async enumerable of DbfRecord instances.</returns>
    /// <remarks>
    /// This method provides async enumeration of records, respecting the SkipDeletedRecords
    /// option setting. Suitable for processing large files without blocking the calling thread.
    /// </remarks>
    public async IAsyncEnumerable<DbfRecord> ReadRecordsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (record, isDeleted) in EnumerateAllRecordsAsync(cancellationToken))
        {
            if (_options.SkipDeletedRecords && isDeleted)
            {
                continue;
            }

            yield return record;
        }
    }

    /// <summary>
    /// Asynchronously reads deleted records from the DBF file.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>An async enumerable of deleted DbfRecord instances.</returns>
    /// <remarks>
    /// This method specifically returns records marked for deletion, useful for
    /// data recovery or audit scenarios.
    /// </remarks>
    public async IAsyncEnumerable<DbfRecord> ReadDeletedRecordsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (record, isDeleted) in EnumerateAllRecordsAsync(cancellationToken))
        {
            if (isDeleted)
            {
                yield return record;
            }
        }
    }

    private async IAsyncEnumerable<(DbfRecord Record, bool IsDeleted)> EnumerateAllRecordsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            yield break;
        }

        if (_stream.CanSeek)
        {
            _stream.Position = Header.HeaderLength;
        }

        var recordBuffer = new byte[Header.RecordLength];
        var recordMemory = new Memory<byte>(recordBuffer);

        for (uint i = 0; i < Header.NumberOfRecords; i++)
        {
            await _stream.ReadExactlyAsync(recordMemory, cancellationToken);

            if (recordBuffer[0] == 0x1A)
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
        var dataOffset = 1;

        for (var fieldIndex = 0; fieldIndex < Fields.Count; fieldIndex++)
        {
            var field = Fields[fieldIndex];
            var fieldLength = field.ActualLength;

            if (dataOffset + fieldLength > recordBuffer.Length)
            {
                fieldValues[fieldIndex] =
                    new InvalidValue(Array.Empty<byte>(), field, "Record is shorter than expected.");
                continue;
            }

            var fieldData = recordBuffer.AsSpan(dataOffset, fieldLength);

            try
            {
                fieldValues[fieldIndex] = _fieldParser.Parse(field, fieldData, _memoFile, Encoding, _options);
            }
            catch (Exception ex)
            {
                if (_options.ValidateFields)
                {
                    throw new FieldParseException(field.Name, field.Type.ToString(), fieldData.ToArray(),
                        $"Failed to parse field: {ex.Message}");
                }

                fieldValues[fieldIndex] = new InvalidValue(fieldData.ToArray(), field, ex.Message);
            }

            dataOffset += fieldLength;
        }

        var record = new DbfRecord(this, fieldValues);

        return (record, isDeleted);
    }

    /// <summary>
    /// Gets the record at the specified index when operating in loaded mode.
    /// </summary>
    /// <param name="index">The zero-based index of the record to retrieve.</param>
    /// <returns>The DbfRecord at the specified index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when records are not loaded. Call Load() first.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the valid range.</exception>
    /// <remarks>
    /// Random access by index is only available when records have been loaded into memory.
    /// In streaming mode, use enumeration instead of indexed access.
    /// </remarks>
    public DbfRecord this[int index]
    {
        get
        {
            if (!IsLoaded)
            {
                throw new InvalidOperationException("Records must be loaded to access by index. Call Load() first.");
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
    /// <value>
    /// In loaded mode, returns the cached count for immediate access.
    /// In streaming mode, enumerates all records to determine the count, which may be expensive.
    /// </value>
    public int Count => IsLoaded ? _loadedRecords?.Length ?? 0 : Records.Count();

    /// <summary>
    /// Gets the number of records marked as deleted.
    /// </summary>
    /// <value>
    /// In loaded mode, returns the cached count for immediate access.
    /// In streaming mode, enumerates all deleted records to determine the count, which may be expensive.
    /// </value>
    public int DeletedCount => IsLoaded ? _loadedDeletedRecords?.Length ?? 0 : DeletedRecords.Count();

    /// <summary>
    /// Finds a field definition by name using case-sensitive or case-insensitive comparison.
    /// </summary>
    /// <param name="fieldName">The name of the field to find.</param>
    /// <returns>The DbfField definition if found; otherwise, null.</returns>
    /// <remarks>
    /// The comparison behavior depends on the IgnoreCase setting in the reader options.
    /// This method provides efficient field lookup using the internal field index mapping.
    /// </remarks>
    public DbfField? FindField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName) || _fieldIndexMap == null)
        {
            return null;
        }

        return _fieldIndexMap.TryGetValue(fieldName, out var index) ? Fields[index] : null;
    }

    /// <summary>
    /// Gets the zero-based index of a field by name.
    /// </summary>
    /// <param name="fieldName">The name of the field to locate.</param>
    /// <returns>The field index if found; otherwise, -1.</returns>
    /// <remarks>
    /// The comparison behavior depends on the IgnoreCase setting in the reader options.
    /// This method is useful for optimizing repeated field access by caching the index.
    /// </remarks>
    public int GetFieldIndex(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName) || _fieldIndexMap == null)
        {
            return -1;
        }

        return _fieldIndexMap.GetValueOrDefault(fieldName, -1);
    }

    /// <summary>
    /// Determines whether the DBF file contains a field with the specified name.
    /// </summary>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <returns><c>true</c> if the field exists; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The comparison behavior depends on the IgnoreCase setting in the reader options.
    /// </remarks>
    public bool HasField(string fieldName) => GetFieldIndex(fieldName) >= 0;

    /// <summary>
    /// Generates comprehensive statistics about the DBF file and current reader state.
    /// </summary>
    /// <returns>A DbfStatistics object containing detailed file and reader information.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <remarks>
    /// The statistics include both file-level metadata (version, encoding, structure) and
    /// runtime information (record counts, access mode, memo file status). Record counts
    /// may require enumeration in streaming mode, potentially affecting performance.
    /// </remarks>
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
            HasMemoFile = _memoFile is { IsValid: true },
            MemoFilePath = MemoFilePath,
            Encoding = Encoding.EncodingName,
            IsLoaded = IsLoaded
        };
    }

    /// <summary>
    /// Returns a string representation of the DBF reader's current state.
    /// </summary>
    /// <returns>
    /// A formatted string containing the table name, DBF version, record count, and access mode.
    /// </returns>
    public override string ToString()
    {
        var status = IsLoaded ? "loaded" : "streaming";
        return $"DbfReader: {TableName} ({Header.DbfVersion.GetDescription()}, {RecordCount} records, {status})";
    }

    /// <summary>
    /// Releases all resources used by the DbfReader.
    /// </summary>
    /// <remarks>
    /// This method closes the underlying stream (if owned), disposes associated memo files,
    /// and releases cached record data. After disposal, the reader cannot be used for any operations.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _memoFile?.Dispose();
        _reader?.Dispose();

        if (_ownsStream)
        {
            _stream.Dispose();
        }

        _loadedRecords = null;
        _loadedDeletedRecords = null;
        _disposed = true;
    }
}
