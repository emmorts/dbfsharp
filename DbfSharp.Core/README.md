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

### Optimization Tips

- Use streaming for large files (don't call `Load()` unless you need random access)
- Adjust buffer size for better I/O performance
- Use memory mapping for large files on 64-bit systems
- Limit record count with `MaxRecords` for sampling large files

### Benchmarks

TBD