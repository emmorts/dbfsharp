# DbfSharp

A high-performance .NET library and command-line tool for reading dBASE (DBF) files with support for all major DBF versions and memo files.

## Features

- Support for all major DBF versions (dBase III, IV, Visual FoxPro, etc.)
- Optimized streaming and memory-efficient processing
- Cross-platform support (Windows, Linux, macOS)
- Both .NET library and command-line tool
- Plugin architecture for custom field parsers

## Quick Start

### .NET Library

Refer to [Core library](./DbfSharp.Core/README.md) for a more extensive documentation.

#### Installation

```bash
dotnet add package DbfSharp.Core
```

#### Usage

```csharp
using DbfSharp.Core;

using var reader = DbfReader.Open("data.dbf");
foreach (var record in reader.Records)
{
    var name = record.GetValue<string>("NAME");
    var birthDate = record.GetValue<DateTime?>("BIRTHDATE");
    Console.WriteLine($"{name}, born {birthDate}");
}
```

### Command-Line Tool

Refer to [ConsoleAot](./DbfSharp.ConsoleAot/README.md) for a more extensive documentation.

#### Installation

```bash
dotnet tool install -g DbfSharp
```

#### Usage

```bash
dotnet tool install -g DbfSharp

# read a DBF file, default output to console as a table
dbfsharp read data.dbf

# export to CSV
dbfsharp read data.dbf --format csv --output data.csv

# get file information
dbfsharp info data.dbf --verbose
```

## Supported DBF Formats

- dBase II, III, IV
- FoxBase, FoxPro
- Visual FoxPro
- Memo files (.dbt, .fpt)
- All standard field types (Character, Date, Numeric, Logical, Memo, etc.)

