# DbfSharp - DBF File Reader Tool

A cross-platform command-line tool for reading and processing DBF (dBASE) database files.

## Installation

Install as a global .NET tool:

```bash
dotnet tool install -g DbfSharp
```

## Usage

### Read DBF file contents
```bash
dbfsharp read data.dbf
dbfsharp read data.dbf --format csv
dbfsharp read data.dbf --limit 100 --fields NAME,SALARY
```

### Display file information
```bash
dbfsharp info data.dbf
dbfsharp info data.dbf --verbose
```

### Export to different formats
```bash
dbfsharp export data.dbf --output data.csv
dbfsharp export data.dbf --format json --output data.json
```

### Validate file integrity
```bash
dbfsharp validate data.dbf
```

## Requirements

- .NET 9.0 runtime or later

## License

MIT License