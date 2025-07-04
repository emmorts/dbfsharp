# DbfRead - DBF File Reader Tool

A cross-platform command-line tool for reading and processing DBF (dBASE) database files.

## Installation

Install as a global .NET tool:

```bash
dotnet tool install -g DbfRead
```

## Usage

### Read DBF file contents
```bash
dbfread read data.dbf
dbfread read data.dbf --format csv
dbfread read data.dbf --limit 100 --fields NAME,SALARY
```

### Display file information
```bash
dbfread info data.dbf
dbfread info data.dbf --verbose
```

### Export to different formats
```bash
dbfread export data.dbf --output data.csv
dbfread export data.dbf --format json --output data.json
```

### Validate file integrity
```bash
dbfread validate data.dbf
```

## Requirements

- .NET 9.0 runtime or later

## License

MIT License