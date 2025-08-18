# DbfSharp.Core

A high-performance, memory-efficient DBF (dBase) file reader for .NET with support for all major DBF versions and memo files.

## Installation

```bash
dotnet add package DbfSharp.Core
```

## Features

- Support for all major DBF versions (dBase III, IV, Visual FoxPro, etc.)
- High performance with streaming and loaded access patterns
- Memory efficient streaming by default, optional loading for random access
- Full memo file support (FPT, DBT files) for large text and binary data
- Plugin architecture for custom field parsers
- Modern C# with Span<T>, Memory<T>, and latest .NET features
- Support for all standard field types and character encodings

## Quick Start

### Basic Usage

```csharp
using DbfSharp.Core;

// Open and read a DBF file
using var reader = DbfReader.Open("data.dbf");

foreach (var record in reader.Records)
{
    var name = record.GetValue<string>("NAME");
    var birthDate = record.GetValue<DateTime?>("BIRTHDATE");
    var salary = record.GetValue<decimal?>("SALARY");

    Console.WriteLine($"{name}, born {birthDate}, salary: {salary:C}");
}
```

### Async Operations

```csharp
// Async file operations
using var reader = await DbfReader.OpenAsync("data.dbf");

await foreach (var record in reader.ReadRecordsAsync())
{
    var name = record.GetValue<string>("NAME");
    Console.WriteLine($"Processing: {name}");
}
```

### Performance Optimized

```csharp
// Use performance-optimized settings for large files
var options = DbfReaderOptions.CreatePerformanceOptimized();
using var reader = DbfReader.Open("large_file.dbf", options);

foreach (var record in reader.Records)
{
    var id = record[0]; // Access by index for speed
    var name = record["NAME"]; // Access by name
}
```

### Memory Optimized

```csharp
// Use memory-optimized settings for huge files
var options = DbfReaderOptions.CreateMemoryOptimized();
using var reader = DbfReader.Open("huge_file.dbf", options);

// Process one record at a time with minimal memory usage
foreach (var record in reader.Records)
{
    ProcessRecord(record);
}
```

### Loaded Mode for Analysis

```csharp
// Load all records into memory for random access
using var reader = DbfReader.Open("analysis.dbf");
reader.Load();

// Access records by index
var firstRecord = reader[0];
var lastRecord = reader[reader.Count - 1];

// Use LINQ for queries
var highSalaries = reader.Records
    .Where(r => r.GetValue<decimal?>("SALARY") > 50000)
    .ToList();
```

## Configuration

### DbfReaderOptions

```csharp
var options = new DbfReaderOptions
{
    Encoding = Encoding.UTF8,           // Override auto-detected encoding
    IgnoreCase = true,                  // Case-insensitive field names
    LowerCaseFieldNames = false,        // Convert field names to lowercase
    LoadOnOpen = false,                 // Load all records immediately
    IgnoreMissingMemoFile = true,       // Don't fail if memo file missing
    TrimStrings = true,                 // Trim whitespace from strings
    ValidateFields = false,             // Skip field validation for speed
    MaxRecords = 10000,                 // Limit number of records read
    BufferSize = 64 * 1024,            // I/O buffer size
    UseMemoryMapping = true             // Use memory-mapped files (64-bit)
};

using var reader = DbfReader.Open("data.dbf", options);
```

### Preset Configurations

```csharp
// Maximum performance
var perfOptions = DbfReaderOptions.CreatePerformanceOptimized();

// Minimum memory usage
var memOptions = DbfReaderOptions.CreateMemoryOptimized();

// Maximum compatibility
var compatOptions = DbfReaderOptions.CreateCompatibilityOptimized();
```

## Supported Field Types

| DBF Type | .NET Type | Description |
|----------|-----------|-------------|
| C | string | Character/Text |
| D | DateTime? | Date (YYYYMMDD) |
| F | float? | Floating point |
| I | int | 32-bit integer |
| L | bool? | Logical (T/F/Y/N/?) |
| M | string | Memo (variable length text) |
| N | decimal? | Numeric (integer or decimal) |
| O | double | Double precision float |
| T, @ | DateTime? | Timestamp |
| Y | decimal | Currency |
| B | varies | Binary (memo or double depending on version) |
| G | byte[] | General/OLE object |
| P | byte[] | Picture |
| V | string | Varchar |
| + | int | Autoincrement |
| 0 | byte[] | Flags |

## Advanced Features

### Custom Field Parsers

```csharp
public class CustomFieldParser : FieldParserBase
{
    public override bool CanParse(FieldType fieldType, DbfVersion dbfVersion)
    {
        return fieldType == FieldType.Character;
    }

    public override object? Parse(DbfField field, ReadOnlySpan<byte> data,
        IMemoFile? memoFile, Encoding encoding, DbfReaderOptions options)
    {
        // Custom parsing logic
        var text = encoding.GetString(data);
        return text.ToUpperInvariant(); // Example: convert to uppercase
    }
}

var options = new DbfReaderOptions
{
    CustomFieldParser = new CustomFieldParser()
};
```

### Error Handling

```csharp
try
{
    using var reader = DbfReader.Open("data.dbf");
    foreach (var record in reader.Records)
    {
        // Check for invalid values
        foreach (var kvp in record)
        {
            if (kvp.Value is InvalidValue invalid)
            {
                Console.WriteLine($"Invalid value in field {kvp.Key}: {invalid.ErrorMessage}");
            }
        }
    }
}
catch (DbfNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.FilePath}");
}
catch (MissingMemoFileException ex)
{
    Console.WriteLine($"Memo file missing: {ex.MemoFilePath}");
}
catch (FieldParseException ex)
{
    Console.WriteLine($"Parse error in field {ex.FieldName}: {ex.Message}");
}
```

### Working with Deleted Records

```csharp
var options = new DbfReaderOptions
{
    SkipDeletedRecords = false // Include deleted records
};

using var reader = DbfReader.Open("data.dbf", options);

// Access only deleted records
await foreach (var deletedRecord in reader.ReadDeletedRecordsAsync())
{
    Console.WriteLine($"Deleted Record ID: {deletedRecord.GetValue<int>("ID")}");
}
```

### Metadata and Statistics

```csharp
using var reader = DbfReader.Open("data.dbf");
var stats = reader.GetStatistics();

Console.WriteLine($"Table: {stats.TableName}");
Console.WriteLine($"Version: {stats.DbfVersion}");
Console.WriteLine($"Records: {stats.TotalRecords:N0}");
Console.WriteLine($"Fields: {stats.FieldCount}");
Console.WriteLine($"Encoding: {stats.Encoding}");
Console.WriteLine($"Last Updated: {stats.LastUpdateDate}");

// Inspect field definitions
foreach (DbfField field in reader.Fields)
{
    Console.WriteLine($"- {field.Name} ({field.Type}), Length: {field.ActualLength}");
}
```

## Performance

`DbfSharp.Core` is optimized for both high throughput and low memory usage. Performance benchmarks measured on **AMD Ryzen 5 7600X** with **.NET 9.0**:

### Key Performance Metrics (1,000,000 records)

| Method                     | Time    | Throughput    | Memory | Best For                          |
|----------------------------|---------|---------------|--------|-----------------------------------|
| Raw field bytes access     | 137ms   | 7.3M rows/sec | 23MB   | Performance-critical applications |
| Zero-allocation span API   | 185ms   | 5.4M rows/sec | 86MB   | Large datasets with convenience   |
| Memory-mapped files        | 289ms   | 3.5M rows/sec | 23MB   | Very large files, low memory      |
| Memory-optimized streaming | 299ms   | 3.3M rows/sec | 24MB   | General high-performance use      |
| Default streaming          | 756ms   | 1.3M rows/sec | 716MB  | Typical business applications     |
| Load all into memory       | 2,082ms | 480K rows/sec | 764MB  | Random access, LINQ queries       |

### Performance Scaling Across Dataset Sizes

| Records       | Raw Field Bytes   | Span API          | Memory-Mapped     | Default Streaming |
|---------------|-------------------|-------------------|-------------------|-------------------|
| **100**       | 52μs (1.9M/sec)   | 56μs (1.8M/sec)   | 79μs (1.3M/sec)   | 129μs (775K/sec)  |
| **10,000**    | 1.2ms (8.7M/sec)  | 1.7ms (5.8M/sec)  | 2.4ms (4.2M/sec)  | 7.5ms (1.3M/sec)  |
| **100,000**   | 13.7ms (7.3M/sec) | 18.3ms (5.5M/sec) | 30.1ms (3.3M/sec) | 73.2ms (1.4M/sec) |
| **1,000,000** | 137ms (7.3M/sec)  | 185ms (5.4M/sec)  | 289ms (3.5M/sec)  | 756ms (1.3M/sec)  |

### Memory Usage Comparison

| Approach             | 100 records | 10K records | 100K records | 1M records |
|----------------------|-------------|-------------|--------------|------------|
| Raw field bytes      | 61KB        | 319KB       | 2.4MB        | 23MB       |
| Memory-mapped        | 23KB        | 256KB       | 2.4MB        | 23MB       |
| Zero-allocation span | 67KB        | 866KB       | 8.6MB        | 86MB       |
| Default streaming    | 125KB       | 6.9MB       | 69MB         | 716MB      |
| Load into memory     | 131KB       | 7.5MB       | 74MB         | 764MB      |

### Code Examples by Use Case

#### Maximum Performance - 7.3M rows/sec
Use when you need the fastest possible processing:

```csharp
using var dbf = DbfReader.Create("data.dbf");

foreach (var record in dbf.EnumerateSpanRecords())
{
    var nameBytes = record.GetFieldBytes(1); // Zero allocations
    // Process raw bytes directly
}
```

#### Memory Constrained - 3.5M rows/sec, 23MB for 1M rows
Use for large files when memory usage is critical:

```csharp
var options = new DbfReaderOptions { UseMemoryMapping = true };
using var dbf = DbfReader.Create("large_file.dbf", options);

foreach (var record in dbf.EnumerateSpanRecords())
{
    var name = record.GetString(1); // Fast string conversion
    var id = record.GetInt32(0);
}
```

#### Balanced Approach - 1.3M rows/sec
Default streaming mode balances performance and memory:

```csharp
using var dbf = DbfReader.Create("data.dbf");

foreach (var record in dbf.Records)
{
    var name = record.GetString("NAME");
    var id = record.GetInt32("ID");
}
```

#### Random Access - 500K rows/sec (after loading)
Best for analysis requiring frequent lookups:

```csharp
using var dbf = DbfReader.Create("data.dbf");
dbf.Load(); // One-time load cost

// Now random access is very fast
var record = dbf[1000];
var filteredRecords = dbf.Records
    .Where(r => r.GetDecimal("SALARY") > 50000)
    .ToList();
```

### When to Use Each Approach

**Raw Field Bytes (`GetFieldBytes()`)** - *Maximum throughput*
- **Use when:** Processing millions of records, custom parsing logic, performance-critical applications
- **Avoid when:** You need convenient object access or complex field parsing

**Zero-Allocation Span API (`EnumerateSpanRecords()`)** - *Best overall performance*
- **Use when:** Need both speed and convenience, processing large datasets
- **Avoid when:** Memory is extremely constrained

**Memory-Mapped Files** - *Large files with memory constraints*
- **Use when:** Files >500MB, running on memory-limited systems, need decent performance
- **Avoid when:** File size <10MB (overhead not worth it)

**Default Streaming** - *General purpose*
- **Use when:** Moderate file sizes, need convenient object access, typical business applications
- **Avoid when:** Processing millions of records frequently

**Load Into Memory** - *Analysis and random access*
- **Use when:** Need LINQ queries, random access by index, multiple passes over data
- **Avoid when:** File size >100MB or memory is limited

### Optimization Tips

1. **Choose the right access pattern:**
   - Use `EnumerateSpanRecords()` for maximum performance
   - Use `Records` for convenient object access
   - Use `Load()` only when you need random access

2. **Memory management:**
   - Enable memory mapping for files >100MB: `options.UseMemoryMapping = true`
   - Use memory-optimized options: `DbfReaderOptions.CreateMemoryOptimized()`

3. **Field access optimization:**
   - Access by index (`record[0]`) is faster than by name (`record["NAME"]`)
   - Use `GetFieldBytes()` for raw data processing
   - Cache field indices for repeated access
