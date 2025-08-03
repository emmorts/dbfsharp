# DbfSharp CLI Tool

A cross-platform command-line tool for reading and processing DBF (dBASE) files with high performance and multiple output formats.

## Installation

Install as a global .NET tool:

```bash
dotnet tool install -g DbfSharp
```

## Requirements

- .NET 9.0 or later

## Features

- High-performance reading optimized for large DBF files
- Cross-platform support (Windows, macOS, Linux)
- Multiple output formats (table, CSV, TSV, JSON)
- Standard input/output for pipeline integration
- Detailed file metadata analysis
- Field filtering and record limiting
- Support for deleted record inspection

## Commands

### `read`

Reads and displays the contents of a DBF file.

**Syntax**
```bash
dbfsharp read [file-path] [options]
```

**Arguments**
- `file-path`: Path to the DBF file. If omitted, reads from standard input.

**Options**

| Option                  | Alias | Description                                                 | Default |
|-------------------------|-------|-------------------------------------------------------------|---------|
| `--format`              | `-f`  | Output format (`table`, `csv`, `tsv`, `json`)               | `table` |
| `--output`              | `-o`  | Output file path (writes to stdout if not specified)        |         |
| `--limit`               | `-l`  | Maximum number of records to display                        |         |
| `--skip`                | `-s`  | Number of records to skip from the beginning                | `0`     |
| `--show-deleted`        |       | Include records marked as deleted                           |         |
| `--fields`              |       | Comma-separated list of fields to include (e.g., "ID,NAME") |         |
| `--verbose`             | `-v`  | Enable verbose output with file information                 |         |
| `--quiet`               | `-q`  | Suppress all informational output                           |         |
| `--encoding`            |       | Override character encoding for text fields                 |         |
| `--ignore-case`         |       | Case-insensitive field name matching                        | `true`  |
| `--trim-strings`        |       | Trim whitespace from string fields                          | `true`  |
| `--ignore-missing-memo` |       | Continue if memo file is missing                            | `true`  |

### `info`

Analyzes a DBF file and displays its metadata and structure.

**Syntax**
```bash
dbfsharp info [file-path] [options]
```

**Arguments**
- `file-path`: Path to the DBF file. If omitted, reads from standard input.

**Options**

| Option                  | Alias | Description                                       | Default  |
|-------------------------|-------|---------------------------------------------------|----------|
| `--fields`              |       | Show field definitions table                      | `true`   |
| `--header`              |       | Show header information table                     | `true`   |
| `--stats`               |       | Show record statistics table                      | `true`   |
| `--memo`                |       | Show memo file information                        | `true`   |
| `--verbose`             | `-v`  | Show additional details including sample data     |          |
| `--quiet`               | `-q`  | Suppress all informational output                 |          |
| `--encoding`            |       | Override character encoding                       |          |
| `--ignore-missing-memo` |       | Continue if memo file is missing                  | `true`   |

## Usage

```bash
# display as a simple table
dbfsharp read data.dbf

# export to CSV
dbfsharp read data.dbf --format csv --output data.csv

# analyze file structure and metadata
dbfsharp info data.dbf

# verbose flag for more detailed output
dbfsharp info data.dbf --verbose

# read from stdin and filter fields
cat data.dbf | dbfsharp read --fields "NAME,SALARY,DEPARTMENT"

# read first 100 records, skipping the first 50
dbfsharp read data.dbf --limit 100 --skip 50

# export to JSON including deleted records
dbfsharp read data.dbf --format json --show-deleted --output data.json

# override encoding
dbfsharp read old_file.dbf --encoding cp1252

# extract specific fields and process with other tools
dbfsharp read data.dbf --format csv --fields "ID,NAME" | sort | head -10

# convert to JSON and process with jq
dbfsharp read data.dbf --format json | jq '.[] | select(.SALARY > 50000)'
```
