# DbfSharp - DBF File Reader Tool

A cross-platform, command-line tool for reading and processing DBF (dBASE) database files, designed for performance and ease of use.

## Installation

Install as a global .NET tool:

```bash
dotnet tool install -g DbfSharp
````

## Requirements

- .NET 9.0 runtime or later

## Core Features

- **High-Performance Reading**: Optimized for speed, even with large DBF files.
- **Cross-Platform**: Runs on Windows, macOS, and Linux.
- **Multiple Output Formats**: Export data to `table`, `csv`, `tsv`, and `json`.
- **Standard Input/Output**: Seamlessly integrates into your data processing pipelines.
- **Detailed File Analysis**: The `info` command provides in-depth metadata, from header details to field definitions.

## Usage

### `read`

Reads and displays the contents of a DBF file.

**Syntax**

```bash
dbfsharp read [file-path] [options]
```

**Arguments**

- `file-path`: Path to the DBF file. If omitted, `dbfsharp` will read from standard input.

**Options**

| Option                  | Alias | Description                                                    | Default  |
|-------------------------|-------|----------------------------------------------------------------|----------|
| `--format`              | `-f`  | Sets the output format (`table`, `csv`, `tsv`, `json`).        | `table`  |
| `--output`              | `-o`  | Specifies the path for the output file.                        | `stdout` |
| `--limit`               | `-l`  | The maximum number of records to display.                      |          |
| `--skip`                | `-s`  | Skips a specified number of records from the beginning.        | `0`      |
| `--show-deleted`        |       | Includes records that are marked as deleted.                   |          |
| `--fields`              |       | A comma-separated list of fields to include (e.g., "ID,NAME"). |          |
| `--verbose`             | `-v`  | Enables verbose output, including file information.            |          |
| `--quiet`               | `-q`  | Suppresses all informational output.                           |          |
| `--encoding`            |       | Overrides the character encoding for reading text fields.      |          |
| `--ignore-case`         |       | Treats field names as case-insensitive.                        | `true`   |
| `--trim-strings`        |       | Trims leading and trailing whitespace from string fields.      | `true`   |
| `--ignore-missing-memo` |       | Prevents failure if a required memo file is missing.           | `true`   |

-----

### `info`

Analyzes a DBF file and displays its metadata and structure.

**Syntax**

```bash
dbfsharp info [file-path] [options]
```

**Arguments**

- `file-path`: Path to the DBF file. If omitted, `dbfsharp` will read from standard input.

**Options**

| Option                  | Description                               | Default                                          |
|-------------------------|-------------------------------------------|--------------------------------------------------|
| `--fields`              | Shows a table with field definitions.     | `true`                                           |
| `--header`              | Displays a table with header information. | `true`                                           |
| `--stats`               | Shows a table with record statistics.     | `true`                                           |
| `--memo`                | Displays information about the memo file. | `true`                                           |
| `--verbose`             | `-v`                                      | Shows additional details, including sample data. | |
| `--quiet`               | `-q`                                      | Suppresses all informational output.             | |
| `--encoding`            |                                           | Overrides the character encoding.                | |
| `--ignore-missing-memo` |                                           | Prevents failure if a memo file is missing.      | `true`|

## Examples

### Reading Data

- **Display a DBF file in a table**:
  ```bash
  dbfsharp read data.dbf
  ```
- **Export to CSV**:
  ```bash
  dbfsharp read data.dbf --format csv --output data.csv
  ```
- **Read from `stdin` and select specific fields**:
  ```bash
  cat data.dbf | dbfsharp read --fields "NAME,SALARY"
  ```

### Analyzing Files

- **Get a full analysis of a DBF file**:
  ```bash
  dbfsharp info data.dbf
  ```
- **Get a verbose analysis, including sample data**:
  ```bash
  dbfsharp info data.dbf --verbose
  ```
