# DbfSharp

![GitHub License](https://img.shields.io/github/license/emmorts/DbfSharp)

Yet another DBF file reader, but this one is mine. I built it primarily to make my own life easier. I often find myself needing to quickly peek into various `.dbf` files, validate their structure, or just export them to something more readable like CSV or JSON.

This project is split into two main parts:

1.  `DbfSharp.Core`: A high-performance, memory-efficient .NET library for reading DBF files.
2.  `DbfSharp.Console`: A cross-platform .NET command-line tool built on top of the core library.

Most people will probably just use the CLI tool.

## The CLI Tool: `dbfsharp`

This is the quick and dirty way to get stuff done. For more detailed documentation, see the [DbfSharp.Console README](/DbfSharp.Console/README.md).

### Installation

It's a .NET tool, so installation is a one-liner (perhaps I'll eventually add it to a brew tap or something, but for now, this is the easiest way):

```bash
dotnet tool install -g DbfSharp
````

### Usage

Once installed, you can call it directly from your terminal.

**1. Get info about a file:**

This is great for a first look. It tells you the version, number of records, encoding, and field definitions.

```bash
dbfsharp info my_data.dbf
```

**2. Read and display data:**

You can dump the contents straight to your console. It defaults to a nice table view.

```bash
# Show the first 100 records in a table
dbfsharp read my_data.dbf --limit 100

# Select specific columns
dbfsharp read my_data.dbf --fields NAME,SALARY,EMAIL
```

**3. Export to other formats:**

You can pipe the output to a file or just view it directly.

```bash
# Export the whole file to CSV
dbfsharp read my_data.dbf --format csv > data.csv

# Quietly export 10 first rows to JSON
dbfsharp read my_data.dbf --limit 10 --format json --quiet > data.json

# You can even pipe data into it from stdin
cat my_data.dbf | dbfsharp read --format tsv
```

**Supported formats:** `table`, `csv`, `tsv`, `json`, and `excel`. Note that for Excel, you *must* specify an output file with `--output`.

## Compatibility

The library aims to support a wide range of DBF formats. Here's a summary of the tested versions.

| Version          | Description                         | Version Byte | Memo Support |
| :--------------- |:------------------------------------| :----------- | :----------- |
| **dBase II** | dBASE II (pain in the !$#*)         | `0x02`       | None         |
| **dBase III Plus** | FoxBASE+/dBase III PLUS, no memo    | `0x03`       | None         |
| **dBase III Plus** | FoxBASE+/dBase III PLUS, with memo  | `0x83`       | `.dbt`       |
| **dBase IV** | dBASE IV with memo                  | `0x8B`       | `.dbt`       |
| **dBase IV SQL** | dBASE IV SQL table files, no memo   | `0x43`       | None         |
| **dBase IV SQL** | dBASE IV SQL table files, with memo | `0xCB`       | `.dbt`       |
| **FoxPro** | FoxPro 2.x (or earlier) with memo   | `0xF5`       | `.fpt`       |
| **Visual FoxPro**| Visual FoxPro                       | `0x30`       | `.fpt`       |
| **Visual FoxPro**| VFP with autoincrement              | `0x31`       | `.fpt`       |
| **Visual FoxPro**| VFP with Varchar/Varbinary          | `0x32`       | `.fpt`       |

## The Core Library: `DbfSharp.Core`

If you need to read DBF files from your own .NET application, you can use the core library directly. It's built with performance in mind, using modern .NET features like `System.IO.Pipelines` and `Span<T>`. For more detailed documentation, see the [DbfSharp.Core README](https://www.google.com/search?q=/DbfSharp.Core/README.md).

### Quick Start

Here's a basic example of how to read records.

```csharp
using DbfSharp.Core;

// Open the DBF file. By default, it streams records.
using var reader = DbfReader.Open("path/to/your/data.dbf");

foreach (var record in reader.Records)
{
    // Access fields by name (case-insensitive by default)
    var name = record.GetValue<string>("NAME");
    var birthDate = record.GetValue<DateTime?>("BIRTHDATE");
    var salary = record.GetValue<decimal?>("SALARY");

    Console.WriteLine($"{name}, born {birthDate:yyyy-MM-dd}, salary: {salary:C}");

    // Or access by index for a little extra speed
    var id = record[0];
}
```

### Configuration

You can control the reader's behavior with `DbfReaderOptions`.

```csharp
var options = new DbfReaderOptions
{
    // Load all records into memory for faster random access
    LoadOnOpen = true,

    // Don't throw an error if a .dbt or .fpt file is missing
    IgnoreMissingMemoFile = true,

    // Override the auto-detected encoding
    Encoding = System.Text.Encoding.GetEncoding("windows-1252")
};

using var reader = DbfReader.Open("data.dbf", options);

// Now you can access records by index because LoadOnOpen = true
var firstRecord = reader[0];
var lastRecord = reader[reader.Count - 1];
```

## Why?

Honestly, I've used other DBF libraries and tools, but I wanted something that was:

  * Written in modern C\# with a focus on performance.
  * Worked seamlessly across Windows, Linux, and macOS.
  * Had a simple, scriptable CLI that I could use in my daily workflow.
  * Was easy to extend if I ever came across a weird, non-standard DBF file.

This tool solves my problem. Maybe it will solve yours too.
