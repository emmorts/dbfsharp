# DbfSharp.Core

A high-performance, memory-efficient DBF (dBase) file reader for .NET with support for all major DBF versions and memo files.

## Features

- **Complete DBF Format Support**: Supports all major DBF versions (dBase III, IV, Visual FoxPro, etc.)
- **High Performance**: Optimized for speed with streaming and loaded access patterns
- **Memory Efficient**: Streaming by default, optional loading for random access
- **Memo File Support**: FPT, DBT files for large text and binary data
- **Extensible**: Plugin architecture for custom field parsers
- **Modern C#**: Leverages Span<T>, Memory<T>, and latest .NET features
- **Thread-Safe**: Safe for concurrent read operations
- **Comprehensive**: Support for all standard field types and character encodings

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

### Performance Optimized

```csharp
// Use performance-optimized settings for large files
var options = DbfReaderOptions.CreatePerformanceOptimized();
using var reader = DbfReader.Open("large_file.dbf", options);

foreach (var record in reader.Records)
{
    // Process records with maximum performance
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

// Now you can access records by index
var firstRecord = reader[0];
var lastRecord = reader[reader.Count - 1];

// Or use LINQ
var highSalaries = reader.Records
    .Where(r => r.GetValue<decimal?>("SALARY") > 50000)
    .ToList();
```

## Configuration Options

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

### Statistics and Metadata

```csharp
using var reader = DbfReader.Open("data.dbf");
var stats = reader.GetStatistics();

Console.WriteLine($"Table: {stats.TableName}");
Console.WriteLine($"Version: {stats.DbfVersion}");
Console.WriteLine($"Records: {stats.ActiveRecords:N0} active, {stats.DeletedRecords:N0} deleted");
Console.WriteLine($"Fields: {stats.FieldCount}");
Console.WriteLine($"Encoding: {stats.Encoding}");
Console.WriteLine($"Last Updated: {stats.LastUpdateDate}");
```

## Performance Optimization

### Enabling Unsafe Code (Optional)

For maximum performance, you can enable unsafe code compilation:

1. Add to your project file:
```xml
<PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

2. Reference the NuGet package and use unsafe extensions:
```csharp
// This will automatically use unsafe optimizations when available
using var reader = DbfReader.Open("data.dbf", DbfReaderOptions.CreatePerformanceOptimized());
```

### Memory Management Tips

1. **Use streaming for large files**: Don't call `Load()` unless you need random access
2. **Enable string interning**: Reduces memory for repeated field names
3. **Adjust buffer size**: Larger buffers improve I/O performance
4. **Use memory mapping**: Enable for large files on 64-bit systems
5. **Limit record count**: Use `MaxRecords` for sampling large files

### Performance Benchmarks

Typical performance on modern hardware:

- **Streaming**: 100,000+ records/second
- **Memory usage**: <100MB for 1M records (streaming mode)
- **Startup time**: <50ms for header parsing
- **Large files**: 1GB+ files supported efficiently

## Compatibility

- **.NET 6.0+**: Primary target framework
- **All platforms**: Windows, Linux, macOS
- **DBF versions**: dBase III, IV, Visual FoxPro, FoxBase
- **Character encodings**: Automatic detection, 50+ code pages supported

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions welcome! Please see CONTRIBUTING.md for guidelines.