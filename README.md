# DbfSharp.Core

A high-performance, memory-efficient, and fully-featured .NET library for reading dBase (DBF) files. DbfSharp.Core provides a modern, intuitive API for accessing data from legacy DBF files, with robust support for various formats, memo files, and character encodings. It is designed for speed, efficiency, and ease of use, leveraging modern .NET features like `Span<T>` and `IAsyncEnumerable<T>`.

## Features

* **Comprehensive DBF Format Support**: Natively supports a wide range of DBF versions, including dBase II, dBase III+, dBase IV, FoxPro, Visual FoxPro, and more.
* **High Performance**: Optimized for speed with efficient parsing logic and minimal memory allocations.
* **Memory Efficient**: By default, records are streamed from the file, ensuring minimal memory consumption even with gigabyte-sized files. An optional "loaded" mode allows for in-memory random access and LINQ operations.
* **Full Memo File Support**: Reads from `.dbt` and `.fpt` memo files, correctly handling large text fields and binary data types like Picture, Object, and General fields.
* **Encoding Detection**: Tries to guess the correct character encoding from the DBF file's language driver byte (that is rarely sufficient though, so explicitly providing encoding is highly recommended).
* **Modern and Extensible API**: The library is built with modern C\# features, with extensibility in mind.
* **Asynchronous and Synchronous APIs**: Provides both `async` and synchronous methods for file operations.
* **Metadata and Statistics**: Access file header information, field definitions, and statistics like record counts (active and deleted), file size, etc.

-----

## Getting Started

### Installation

Install the DbfSharp.Core library from NuGet Package Manager.

```shell
dotnet add package DbfSharp.Core
```

### Basic Usage

The most common way to use the library is to open a file and stream its records.

```csharp
using DbfSharp.Core;
using System.Text;

// configure options if needed, e.g., to specify an encoding
var options = new DbfReaderOptions
{
    Encoding = Encoding.GetEncoding("windows-1252")
};

// asynchronously open and read a DBF file
await using var reader = await DbfReader.CreateAsync("data.dbf", options);

// asynchronously iterate over records
await foreach (var record in reader.ReadRecordsAsync())
{
    // access fields by name with type-specific getters
    var name = record.GetString("NAME");
    var birthDate = record.GetDateTime("BIRTHDATE");
    var salary = record.GetDecimal("SALARY");
    var isMember = record.GetBoolean("IS_MEMBER");

    Console.WriteLine($"Name: {name}, Born: {birthDate:d}, Salary: {salary:C}, Member: {isMember}");
}
```

-----

## Usage Examples

### Synchronous Reading

For console applications or scripts, you can use the synchronous API.

```csharp
using var reader = DbfReader.Create("data.dbf");

foreach (var record in reader.Records)
{
    // access fields by index for maximum performance
    var id = record[0];
    var name = record[1] as string;

    Console.WriteLine($"ID: {id}, Name: {name}");
}
```

### Loaded Mode for Random Access and LINQ

If you need to perform multiple passes over the data or require random access, you can load all records into memory.

```csharp
await using var reader = await DbfReader.CreateAsync("analysis.dbf");
await reader.LoadAsync(); // load all active records into memory

// now you can access records by index
var firstRecord = reader[0];
var lastRecord = reader[reader.Count - 1];

// you can also use LINQ to query the in-memory records
var highEarners = reader.Records
    .Where(r => r.GetDecimal("SALARY") > 75000m)
    .OrderBy(r => r.GetString("NAME"))
    .ToList();

Console.WriteLine($"Found {highEarners.Count} high earners.");
```

### Working with Deleted Records

DBF files mark records for deletion instead of removing them immediately. You can control whether to include or exclude these records.

```csharp
var options = new DbfReaderOptions
{
    // set to false to include deleted records in the 'Records' collection
    SkipDeletedRecords = false
};

await using var reader = await DbfReader.CreateAsync("data.dbf", options);

// records marked for deletion will now be included in `reader.ReadRecordsAsync()` enumeration
// you can also access only the deleted records.
await foreach (var deletedRecord in reader.ReadDeletedRecordsAsync())
{
    Console.WriteLine($"Deleted Record ID: {deletedRecord.GetInt32("ID")}");
}
```

### Accessing Metadata and Statistics

You can inspect the DBF file's structure and get summary statistics.

```csharp
await using var reader = await DbfReader.CreateAsync("data.dbf");

DbfStatistics stats = reader.GetStatistics();
Console.WriteLine(stats.ToString());

// get header information
DbfHeader header = reader.Header;
Console.WriteLine($"DBF Version: {header.DbfVersion.GetDescription()}");
Console.WriteLine($"Last Updated: {header.LastUpdateDate}");

// inspect field definitions
Console.WriteLine("\nFields:");
foreach (DbfField field in reader.Fields)
{
    Console.WriteLine($"- {field.Name} ({field.Type}), Length: {field.ActualLength}");
}
```

-----

## Configuration

Customize the reader's behavior using `DbfReaderOptions`.

| Property                | Description                                                                                              | Default       |
| ----------------------- | -------------------------------------------------------------------------------------------------------- | ------------- |
| `Encoding`              | Character encoding for text fields. If `null`, it's auto-detected from the file's header.       | `null`        |
| `IgnoreCase`            | Perform case-insensitive field name lookups.                                                | `true`        |
| `LowerCaseFieldNames`   | Convert all field names to lowercase upon reading.                                          | `false`       |
| `IgnoreMissingMemoFile` | If `true`, continues reading even if a required memo file (`.dbt` or `.fpt`) is missing.        | `false`       |
| `TrimStrings`           | Trim trailing whitespace from character fields.                                               | `true`        |
| `ValidateFields`        | If `true`, throws an exception if field data is malformed. If `false`, returns an `InvalidValue` object. | `true` |
| `SkipDeletedRecords`    | If `true`, excludes records marked for deletion from the `Records` enumeration.           | `true`        |
| `MaxRecords`            | Limits the total number of records read. Useful for sampling large files.                 | `null` (no limit) |
| `BufferSize`            | The buffer size in bytes for file I/O operations.                                               | `65536`       |
| `CustomFieldParser`     | Provide a custom parser for specialized field handling.                                         | `null`        |

### Preset Configurations

For convenience, `DbfReaderOptions` provides static methods to create optimized settings for common scenarios.

* **For Maximum Performance:** Ideal for large files where read speed is critical.

  ```csharp
  var perfOptions = DbfReaderOptions.CreatePerformanceOptimized();
  // disables validation and string trimming, uses a larger buffer
  await using var reader = await DbfReader.CreateAsync("large_file.dbf", perfOptions);
  ```

* **For Minimal Memory Usage:** Best for memory-constrained environments.

  ```csharp
  var memOptions = DbfReaderOptions.CreateMemoryOptimized();
  // uses a smaller buffer and ensures streaming is prioritized
  await using var reader = await DbfReader.CreateAsync("huge_file.dbf", memOptions);
  ```

* **For Maximum Compatibility:** Use this for problematic or non-standard files.

  ```csharp
  var compatOptions = DbfReaderOptions.CreateCompatibilityOptimized();
  // ignores missing memo files, disables validation, and uses fallback decoding
  await using var reader = await DbfReader.CreateAsync("old_file.dbf", compatOptions);
  ```

-----

## Error Handling

The library provides specific exceptions to handle common file and parsing errors.

```csharp
try
{
    var options = new DbfReaderOptions { ValidateFields = true };
    await using var reader = await DbfReader.CreateAsync("non_existent.dbf", options);

    await foreach (var record in reader.ReadRecordsAsync())
    {
        // some processing logic here
    }
}
catch (DbfNotFoundException ex)
{
    // DBF file itself was not found
    Console.WriteLine($"File not found: {ex.FilePath}");
}
catch (MissingMemoFileException ex)
{
    // a required .dbt or .fpt file is missing
    Console.WriteLine($"Memo file missing for {ex.DbfFilePath}. Expected at: {ex.MemoFilePath}");
}
catch (FieldParseException ex)
{
    // thrown when 'ValidateFields' is true and data cannot be parsed
    Console.WriteLine($"Parse error in field '{ex.FieldName}' ({ex.FieldType}): {ex.Message}");
    Console.WriteLine($"Raw Data: {Convert.ToHexString(ex.RawData)}");
}
catch (DbfException ex)
{
    // general DBF-related error
    Console.WriteLine($"A DBF error occurred: {ex.Message}");
}
```

### Handling Invalid Values without Exceptions

If you set `ValidateFields = false`, the reader will not throw a `FieldParseException`. Instead, it will return an `InvalidValue` object for any field that cannot be parsed. This allows you to continue processing a file even if it contains corrupted data.

```csharp
var options = new DbfReaderOptions { ValidateFields = false };
await using var reader = await DbfReader.CreateAsync("corrupted_data.dbf", options);

await foreach (var record in reader.ReadRecordsAsync())
{
    if (record["SALARY"] is InvalidValue invalid)
    {
        Console.WriteLine($"Could not parse SALARY. Error: {invalid.ErrorMessage}");
    }
    else
    {
        Console.WriteLine($"Salary: {record.GetDecimal("SALARY")}");
    }
}
```
