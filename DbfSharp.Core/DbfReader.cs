using System.Buffers;
using System.Collections;
using System.IO.Pipelines;
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
/// Key features include robust error handling for malformed DBF files, automatic encoding detection,
/// memo file support (.DBT/.FPT), field validation with customizable parsing, and comprehensive
/// statistics reporting. The reader is designed to handle legacy DBF files that may not strictly
/// conform to format specifications while providing modern .NET integration through IEnumerable
/// and async/await patterns.
/// </para>
/// <para>
/// **Thread Safety**: This class is **not thread-safe**. Each thread should use its own DbfReader instance.
/// Do not share a single DbfReader instance across multiple threads without external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Asynchronous streaming access for large files (recommended)
/// using var reader = await DbfReader.CreateAsync("data.dbf");
/// await foreach (var record in reader.ReadRecordsAsync())
/// {
///     Console.WriteLine(record["CustomerName"]);
/// }
///
/// // Loaded access for frequent operations and random access
/// using var reader = await DbfReader.CreateAsync("data.dbf");
/// await reader.LoadAsync(); // Load all records into memory
/// var statistics = reader.GetStatistics();
/// var firstRecord = reader[0]; // Random access is now available
/// </code>
/// </example>
public sealed class DbfReader : IDisposable, IAsyncDisposable, IEnumerable<DbfRecord>
{
    private readonly Stream _stream;
    private readonly BinaryReader? _reader;
    private readonly DbfReaderOptions _options;
    private readonly IMemoFile? _memoFile;
    private readonly IFieldParser _fieldParser;
    private readonly Dictionary<string, int> _fieldIndexMap;
    private readonly bool _ownsStream;
    private readonly PipeReader? _pipeReader;
    private readonly Task? _pipeWriterTask;

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
            // Raise deferred warnings when first event handler is attached
            if (!_warningsRaised)
            {
                _warningsRaised = true;
                RaiseDeferredWarnings();
            }
        }
        remove
        {
            _warning -= value;
        }
    }

    private readonly List<string>? _duplicateFields;
    private DbfRecord[]? _loadedDeletedRecords;
    private bool _disposed;

    private const int StackAllocThreshold = 256;
    private const uint EofMarker = 0x1A; // End of file marker in DBF files

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
    /// Gets the file path to the associated memo file, if present.
    /// </summary>
    public string? MemoFilePath => _memoFile?.FilePath;

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

            return EnumerateRecords(skipDeleted: _options.SkipDeletedRecords);
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
    /// to enforce the use of the static factory methods <see cref="CreateAsync(string, DbfReaderOptions, CancellationToken)"/>
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
        PipeReader? pipeReader = null,
        Task? pipeWriterTask = null)
    {
        _stream = stream;
        _ownsStream = ownsStream;
        if (stream.CanSeek && pipeReader == null)
        {
            _reader = new BinaryReader(stream, options.Encoding ?? header.Encoding, leaveOpen: true);
        }

        Header = header;
        Fields = fields;
        _options = options;
        _memoFile = memoFile;
        TableName = tableName;
        _pipeReader = pipeReader;
        _pipeWriterTask = pipeWriterTask;

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
        var duplicateFields = new List<string>();

        for (var i = 0; i < fieldNames.Length; i++)
        {
            // Only map the first occurrence of each field name to avoid overwriting duplicates
            if (!_fieldIndexMap.ContainsKey(fieldNames[i]))
            {
                _fieldIndexMap[fieldNames[i]] = i;
            }
            else
            {
                // Track duplicate field names
                if (!duplicateFields.Contains(fieldNames[i]))
                {
                    duplicateFields.Add(fieldNames[i]);
                }
            }
        }

        // Store duplicate fields for later warning (after event handlers can be attached)
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
    internal void RaiseDeferredWarnings()
    {
        if (_duplicateFields is { Count: > 0 })
        {
            var duplicateList = string.Join(", ", _duplicateFields.Select(name => $"'{name}'"));
            var message = _duplicateFields.Count == 1
                ? $"Duplicate field name detected: {duplicateList}. Only the first occurrence will be accessible via FindField()."
                : $"Duplicate field names detected: {duplicateList}. Only the first occurrence of each will be accessible via FindField().";

            OnWarning(new DbfWarningEventArgs(message, DbfWarningType.DuplicateFieldNames,
                $"{_duplicateFields.Count} duplicate field name(s) found"));
        }
    }

    /// <summary>
    /// Asynchronously creates and initializes a DbfReader from the specified file path.
    /// This is the recommended factory method for opening a DBF file.
    /// </summary>
    /// <param name="filePath">The path to the DBF file to open.</param>
    /// <param name="options">Optional reader configuration settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a new DbfReader instance.</returns>
    public static async Task<DbfReader> CreateAsync(string filePath, DbfReaderOptions? options = null,
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
    /// Creates a DbfReader from the specified file path using synchronous I/O.
    /// For applications using async/await, prefer <see cref="CreateAsync(string, DbfReaderOptions, CancellationToken)"/>.
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
    /// Asynchronously creates a DbfReader from a stream. The caller is responsible for disposing the stream.
    /// </summary>
    public static async Task<DbfReader> CreateAsync(Stream stream, DbfReaderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateFromStreamAsync(stream, false, options, "Unknown", null, cancellationToken);
    }

    /// <summary>
    /// Creates a DbfReader from a stream using synchronous I/O. The caller is responsible for disposing the stream.
    /// </summary>
    public static DbfReader Create(Stream stream, DbfReaderOptions? options = null)
    {
        return CreateFromStream(stream, false, options, "Unknown", null);
    }

    private static DbfReader CreateFromStream(Stream stream, bool ownsStream, DbfReaderOptions? options,
        string tableName, string? filePath)
    {
        options ??= new DbfReaderOptions();

        try
        {
            var reader = new BinaryReader(stream, options.Encoding ?? Encoding.ASCII, leaveOpen: true);
            var header = DbfHeader.Read(reader);

            var fieldsStartPosition = header.DbfVersion == DbfVersion.DBase2 ? 8 : stream.Position;
            stream.Position = fieldsStartPosition;

            DbfField[] fields;
            try
            {
                fields = DbfField.ReadFields(reader, header.Encoding, 0, options.LowerCaseFieldNames,
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
                // Zero fields is valid for some DBF files, but the record length should be minimal (typically 1 byte for deletion marker)
                if (header.RecordLength == 0)
                {
                    throw new DbfException("DBF file has no field definitions and invalid record length");
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

    private static async Task<DbfReader> CreateFromStreamAsync(Stream stream, bool ownsStream,
        DbfReaderOptions? options, string tableName, string? filePath, CancellationToken cancellationToken)
    {
        options ??= new DbfReaderOptions();

        try
        {
            var pipe = new Pipe();
            var writingTask = FillPipeAsync(stream, pipe.Writer, cancellationToken);
            var pipeReader = pipe.Reader;

            var header = await DbfHeader.ReadAsync(pipeReader, cancellationToken);

            var fields = await DbfField.ReadFieldsAsync(pipeReader, header.Encoding, 0,
                options.LowerCaseFieldNames, header.DbfVersion, header.HeaderLength, cancellationToken);

            if (fields.Length == 0)
            {
                // Zero fields is valid for some DBF files, but the record length should be minimal (typically 1 byte for deletion marker)
                if (header.RecordLength == 0)
                {
                    throw new DbfException("DBF file has no field definitions and invalid record length");
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

            // For seekable streams, position to record data
            if (stream.CanSeek)
            {
                stream.Position = header.HeaderLength;
            }
            else
            {
                // For non-seekable streams using pipe, we need to advance the pipe reader
                // to skip any remaining padding after field definitions
                var consumedBytes = DbfHeader.Size + fields.Length * DbfField.Size + 1; // +1 for terminator
                var remainingToSkip = header.HeaderLength - consumedBytes;


                if (remainingToSkip > 0)
                {
                    // Read and discard the padding bytes
                    while (remainingToSkip > 0)
                    {
                        var paddingResult = await pipeReader.ReadAsync(cancellationToken);
                        var paddingBuffer = paddingResult.Buffer;

                        if (paddingBuffer.IsEmpty && paddingResult.IsCompleted)
                        {
                            break;
                        }

                        var toSkip = Math.Min(remainingToSkip, (int)paddingBuffer.Length);
                        var skipPosition = paddingBuffer.GetPosition(toSkip);
                        pipeReader.AdvanceTo(skipPosition);
                        remainingToSkip -= toSkip;


                        if (paddingResult.IsCompleted)
                        {
                            break;
                        }
                    }
                }
            }

            // For seekable streams, we don't need the pipe reader for record enumeration
            // since we can use the properly positioned stream directly
            var readerPipeReader = stream.CanSeek ? null : pipeReader;
            var readerPipeTask = stream.CanSeek ? null : writingTask;


            return new DbfReader(stream, ownsStream, header, fields, options, memoFile, tableName, readerPipeReader,
                readerPipeTask);
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

    private static async Task FillPipeAsync(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
    {
        const int minimumBufferSize = 4096;

        try
        {
            while (true)
            {
                var memory = writer.GetMemory(minimumBufferSize);
                var bytesRead = await stream.ReadAsync(memory, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                writer.Advance(bytesRead);

                var result = await writer.FlushAsync(cancellationToken);
                if (result.IsCompleted)
                {
                    break;
                }
            }

            await writer.CompleteAsync();
        }
        catch (Exception ex)
        {
            await writer.CompleteAsync(ex);
        }
    }

    private static DbfHeader RecalculateDBase2Header(DbfHeader header, DbfField[] fields)
    {
        var calculatedRecordLength = 1 + fields.Sum(field => field.ActualLength); // incl. deletion flag
        var calculatedHeaderLength = (ushort)(8 + fields.Length * DbfField.DBase2Size);

        return new DbfHeader(
            header.DbVersionByte, header.Year, header.Month, header.Day, header.NumberOfRecords,
            calculatedHeaderLength, (ushort)calculatedRecordLength, header.Reserved1, header.IncompleteTransaction,
            header.EncryptionFlag, header.FreeRecordThread, header.Reserved2, header.Reserved3,
            header.MdxFlag, header.LanguageDriver, header.Reserved4
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
    /// Asynchronously loads all records into memory for fast random access and repeated enumeration.
    /// </summary>
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
    public void Unload()
    {
        _loadedRecords = null;
        _loadedDeletedRecords = null;
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

            if (recordBuffer[0] == EofMarker)
            {
                break;
            }

            yield return ParseRecord(recordBuffer);
        }
    }

    /// <summary>
    /// Asynchronously reads active records from the DBF file.
    /// </summary>
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

        var maxRecords = _options.MaxRecords ?? int.MaxValue;

        var enumerator = _pipeReader == null
            ? EnumerateRecordsFromStreamAsync(maxRecords, cancellationToken)
            : EnumerateRecordsFromPipeAsync(maxRecords, cancellationToken);

        await foreach (var record in enumerator.WithCancellation(cancellationToken))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Handles record enumeration for seekable streams. This is the faster path
    /// when the underlying stream supports direct positioning.
    /// </summary>
    private async IAsyncEnumerable<(DbfRecord Record, bool IsDeleted)> EnumerateRecordsFromStreamAsync(
        int maxRecords, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_stream.CanSeek)
        {
            _stream.Position = Header.HeaderLength;
        }

        var recordBuffer = new byte[Header.RecordLength];
        var recordMemory = new Memory<byte>(recordBuffer);

        for (var recordsRead = 0; recordsRead < Header.NumberOfRecords && recordsRead < maxRecords; recordsRead++)
        {
            await _stream.ReadExactlyAsync(recordMemory, cancellationToken);
            if (recordBuffer[0] == EofMarker) // EOF marker
            {
                break;
            }

            yield return ParseRecord(recordBuffer);
        }
    }

    /// <summary>
    /// Handles record enumeration using a PipeReader, ideal for non-seekable streams.
    /// This method processes records from a continuous byte stream.
    /// </summary>
    private async IAsyncEnumerable<(DbfRecord Record, bool IsDeleted)> EnumerateRecordsFromPipeAsync(
        int maxRecords, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recordLength = Header.RecordLength;
        var recordsRead = 0;

        while (recordsRead < Header.NumberOfRecords && recordsRead < maxRecords)
        {
            var result = await _pipeReader!.ReadAsync(cancellationToken);
            var buffer = result.Buffer;

            while (buffer.Length >= recordLength)
            {
                if (recordsRead >= maxRecords)
                {
                    break;
                }

                // Note: Header terminator skipping is now handled in ReadFieldsAsync method
                // No need to skip 0x0D bytes here as we should already be positioned at record data

                var recordSequence = buffer.Slice(0, recordLength);
                if (recordSequence.FirstSpan[0] == EofMarker) // EOF marker
                {
                    _pipeReader.AdvanceTo(recordSequence.Start);
                    yield break;
                }

                var recordBytes = recordSequence.ToArray();
                yield return ParseRecord(recordBytes);
                recordsRead++;

                buffer = buffer.Slice(recordLength);
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted || recordsRead >= maxRecords)
            {
                break;
            }
        }
    }

    private (DbfRecord Record, bool IsDeleted) ParseRecord(in ReadOnlySequence<byte> recordSequence)
    {
        var isDeleted = recordSequence.FirstSpan[0] == '*';
        var fieldValues = new object?[Fields.Count];
        var recordData = recordSequence.Slice(1); // Skip deletion flag

        // Visual FoxPro uses the Address field for field positioning, but only if addresses are meaningful
        var useAddressBasedParsing = Header.DbfVersion.IsVisualFoxPro() && HasMeaningfulAddresses();

        if (useAddressBasedParsing)
        {
            // Address-based parsing: use field Address property for positioning
            for (var fieldIndex = 0; fieldIndex < Fields.Count; fieldIndex++)
            {
                var field = Fields[fieldIndex];
                var fieldLength = field.ActualLength;
                var fieldOffset = (int)field.Address - 1; // Address is 1-based, convert to 0-based

                if (recordData.Length < fieldOffset + fieldLength)
                {
                    fieldValues[fieldIndex] =
                        new InvalidValue(Array.Empty<byte>(), field, "Record is shorter than expected for field address.");
                    continue;
                }

                var fieldDataSequence = recordData.Slice(fieldOffset, fieldLength);


                try
                {
                    fieldValues[fieldIndex] = ParseFieldFromSequence(field, fieldDataSequence);
                }
                catch (Exception ex)
                {
                    if (_options.ValidateFields)
                    {
                        throw new FieldParseException(field.Name, field.Type.ToString(), fieldDataSequence.ToArray(),
                            $"Failed to parse field: {ex.Message}");
                    }

                    fieldValues[fieldIndex] = new InvalidValue(fieldDataSequence.ToArray(), field, ex.Message);
                }
            }
        }
        else
        {
            // Sequential parsing: fields are tightly packed
            var dataSequence = recordData;


            for (var fieldIndex = 0; fieldIndex < Fields.Count; fieldIndex++)
            {
                var field = Fields[fieldIndex];
                var fieldLength = field.ActualLength;

                if (dataSequence.Length < fieldLength)
                {
                    fieldValues[fieldIndex] =
                        new InvalidValue(Array.Empty<byte>(), field, "Record is shorter than expected.");
                    continue;
                }

                var fieldDataSequence = dataSequence.Slice(0, fieldLength);


                try
                {
                    fieldValues[fieldIndex] = ParseFieldFromSequence(field, fieldDataSequence);
                }
                catch (Exception ex)
                {
                    if (_options.ValidateFields)
                    {
                        throw new FieldParseException(field.Name, field.Type.ToString(), fieldDataSequence.ToArray(),
                            $"Failed to parse field: {ex.Message}");
                    }

                    fieldValues[fieldIndex] = new InvalidValue(fieldDataSequence.ToArray(), field, ex.Message);
                }

                dataSequence = dataSequence.Slice(fieldLength);
            }
        }

        var record = new DbfRecord(this, fieldValues);
        return (record, isDeleted);
    }

    private bool HasMeaningfulAddresses()
    {
        // Check if the field addresses form a valid sequential pattern
        // If addresses are 0 or don't form a proper sequence, fall back to sequential parsing

        if (Fields.Count == 0) return false;

        // Check if any field has address 0 (indicates addresses not used)
        if (Fields.Any(f => f.Address == 0)) return false;

        // Check if addresses form a proper sequence (each field starts where the previous ends)
        uint expectedAddress = 1; // Addresses are 1-based

        for (var i = 0; i < Fields.Count; i++)
        {
            if (Fields[i].Address != expectedAddress)
            {
                return false; // Address doesn't match expected sequential position
            }
            expectedAddress += (uint)Fields[i].ActualLength;
        }

        // Additional validation: ensure the total calculated record length matches header
        var calculatedRecordLength = (int)expectedAddress - 1; // -1 because addresses are 1-based
        if (calculatedRecordLength != Header.RecordLength - 1) // -1 for deletion flag
        {
            return false; // Record length mismatch indicates addresses are unreliable
        }


        return true;
    }

    private object? ParseFieldFromSequence(DbfField field, in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            return _fieldParser.Parse(field, sequence.FirstSpan, _memoFile, Encoding, _options);
        }

        var length = (int)sequence.Length;
        byte[]? rentedBuffer = null;
        try
        {
            var span = length <= StackAllocThreshold
                ? stackalloc byte[length]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));
            sequence.CopyTo(span);
            return _fieldParser.Parse(field, span[..length], _memoFile, Encoding, _options);
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private (DbfRecord Record, bool IsDeleted) ParseRecord(byte[] recordBuffer)
    {
        var sequence = new ReadOnlySequence<byte>(recordBuffer);
        return ParseRecord(in sequence);
    }

    /// <summary>
    /// Gets the record at the specified index when operating in loaded mode.
    /// </summary>
    /// <param name="index">The zero-based index of the record to retrieve.</param>
    /// <returns>The DbfRecord at the specified index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when records are not loaded. Call Load() or LoadAsync() first.</exception>
    public DbfRecord this[int index]
    {
        get
        {
            if (!IsLoaded)
            {
                throw new InvalidOperationException(
                    "Records must be loaded to access by index. Call Load() or LoadAsync() first.");
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
    public int DeletedCount => IsLoaded ? _loadedDeletedRecords?.Length ?? 0 : DeletedRecords.Count();

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
            HasMemoFile = _memoFile is { IsValid: true },
            MemoFilePath = MemoFilePath,
            Encoding = Encoding.EncodingName,
            IsLoaded = IsLoaded
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_reader != null)
        {
            await CastAndDispose(_reader);
        }

        if (_memoFile != null)
        {
            await CastAndDispose(_memoFile);
        }

        if (_pipeReader != null)
        {
            await _pipeReader.CompleteAsync();
        }

        if (_pipeWriterTask != null)
        {
            try
            {
                // ensure the background pipe-filling task is complete and observe any exceptions
                await _pipeWriterTask;
            }
            catch
            {
                // exception will be propagated to the reader so we can ignore it here
            }

            await CastAndDispose(_pipeWriterTask);
        }

        if (_ownsStream)
        {
            await _stream.DisposeAsync();
        }

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}
