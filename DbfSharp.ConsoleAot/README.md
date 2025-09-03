# DbfSharp CLI Tool

A high-performance, cross-platform command-line tool for reading and processing DBF (dBASE) files with comprehensive support for legacy data formats, memo files, and modern output options including shapefile support.

## Quick Start

```bash
# Install as global .NET tool
dotnet tool install -g DbfSharp

# View DBF file structure
dbfsharp info data.dbf

# Export to CSV
dbfsharp read data.dbf --format csv --output data.csv

# Pipe output to other tools
dbfsharp read data.dbf --format json | jq '.[] | select(.SALARY > 50000)'

# Read shapefile with geometry
dbfsharp read buildings.shp --format geojson --output buildings.geojson
```

## Installation

### Global .NET Tool (Recommended)

```bash
dotnet tool install -g DbfSharp
```

### Homebrew

```bash
brew install dbfsharp
```

### Build from Source

```bash
git clone https://github.com/emmorts/DbfSharp.git
cd DbfSharp/DbfSharp.ConsoleAot
dotnet build -c Release
```

## Requirements

- **.NET 9.0** or later (only if installing as a .NET tool)

## Features

- **High-performance** DBF reading optimized for large files
- **Cross-platform** native AOT compilation
- **Shapefile support** with spatial queries (bounding box, nearest neighbor)
- **Multiple output formats** (table, CSV, TSV, JSON, GeoJSON)
- **Legacy format compatibility** (dBASE III/IV/5, Visual FoxPro, Clipper)
- **Memo file support** (FPT, DBT)
- **Encoding handling** for various character sets
- **Pipeline integration** with stdin/stdout support

## Commands

### `read` - Extract and Export Data

Reads DBF file contents and exports in various formats. Also supports shapefiles with full geometry processing.

```bash
dbfsharp read [file-path] [options]
```

**Arguments:**
- `file-path` - Path to DBF file or shapefile (reads from stdin if omitted)

**Options**

| Option                  | Alias | Description                                                 | Default |
|-------------------------|-------|-------------------------------------------------------------|---------|
| `--format`              | `-f`  | Output format (`table`, `csv`, `tsv`, `json`, `geojson`)    | `table` |
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

**Spatial Query Options (Shapefiles only)**

| Option                  | Description                                       | 
|-------------------------|---------------------------------------------------|
| `--bounding-box`        | Filter by bounding box: "minX,minY,maxX,maxY"     |
| `--contains-point`      | Filter geometries containing point "x,y"          |
| `--intersects-with`     | Filter geometries intersecting bounding box       |
| `--nearest-point`       | Find features nearest to point "x,y"              |
| `--nearest-count`       | Number of nearest features to return               |
| `--nearest-distance`    | Maximum distance for nearest neighbor search       |
| `--build-spatial-index` | Build spatial index for faster queries            |

### `info` - Analyze File Structure

Analyzes DBF files and shapefiles, displaying comprehensive metadata, field definitions, spatial information, and statistics.

```bash
dbfsharp info [file-path] [options]
```

**Arguments:**
- `file-path` - Path to DBF file or shapefile (reads from stdin if omitted)

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

## Usage Examples

### Basic Operations

```bash
# View file structure and metadata
dbfsharp info customers.dbf

# Display first 10 records as table
dbfsharp read customers.dbf --limit 10

# Export entire file to CSV
dbfsharp read customers.dbf --format csv --output customers.csv

# Analyze shapefile structure and spatial info
dbfsharp info buildings.shp --verbose

# Export shapefile to GeoJSON
dbfsharp read roads.shp --format geojson --output roads.geojson
```

### Data Analysis & Filtering

```bash
# Extract specific fields only
dbfsharp read customers.dbf --fields "ID,NAME,EMAIL,PHONE" --format csv

# Sample data: skip first 100, take next 50 records
dbfsharp read large_file.dbf --skip 100 --limit 50

# Include deleted records for data recovery
dbfsharp read archive.dbf --show-deleted --format json

# Handle legacy encoding issues
dbfsharp read legacy.dbf --encoding cp1252

# Filter shapefile by bounding box (spatial query)
dbfsharp read parcels.shp --bounding-box "-118.5,34.0,-118.0,34.5" --format geojson

# Find nearest features to a point
dbfsharp read points.shp --nearest-point "-118.25,34.05" --nearest-count 5 --format geojson
```

### Pipeline Integration

```bash
# Process with standard Unix tools
dbfsharp read data.dbf --format csv --fields "ID,NAME" | sort | head -10

# Filter with grep and count
dbfsharp read employees.dbf --format csv | grep "Engineer" | wc -l

# Convert to JSON and query with jq
dbfsharp read sales.dbf --format json | jq '.[] | select(.AMOUNT > 1000)'

# Stream processing from stdin
cat remote_file.dbf | dbfsharp read --format json --fields "ID,STATUS"
```

### Spatial Operations

```bash
# Export shapefile to GeoJSON with full geometry
dbfsharp read polygons.shp --format geojson --output polygons.geojson

# Spatial filtering by bounding box
dbfsharp read features.shp --bounding-box "-74.1,40.6,-73.9,40.8" --format geojson

# Find features containing a specific point
dbfsharp read parcels.shp --contains-point "-74.0,40.7" --format geojson

# Analyze spatial extent and geometry distribution
dbfsharp info large_shapefile.shp --verbose

# Build spatial index for performance
dbfsharp read large_dataset.shp --build-spatial-index --bounding-box "-122.5,37.7,-122.3,37.8"

# Complex spatial data extraction with performance optimization
dbfsharp read census_tracts.shp --build-spatial-index \
    --bounding-box "-122.5,37.7,-122.3,37.8" \
    --fields "GEOID,NAME,POP2020" \
    --format geojson --output sf_tracts.geojson
```

## Performance

- **Streaming processing** with automatic memory/file strategy based on size
- **R-tree spatial indexing** for fast spatial queries
- **Memory-mapped files** for large datasets
- **Native AOT compilation** for minimal startup overhead
- Use `--build-spatial-index` for large shapefiles with spatial queries

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.