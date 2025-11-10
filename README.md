# DbfSharp

[![DeepWiki](https://img.shields.io/badge/DeepWiki-emmorts%2Fdbfsharp-blue.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACwAAAAyCAYAAAAnWDnqAAAAAXNSR0IArs4c6QAAA05JREFUaEPtmUtyEzEQhtWTQyQLHNak2AB7ZnyXZMEjXMGeK/AIi+QuHrMnbChYY7MIh8g01fJoopFb0uhhEqqcbWTp06/uv1saEDv4O3n3dV60RfP947Mm9/SQc0ICFQgzfc4CYZoTPAswgSJCCUJUnAAoRHOAUOcATwbmVLWdGoH//PB8mnKqScAhsD0kYP3j/Yt5LPQe2KvcXmGvRHcDnpxfL2zOYJ1mFwrryWTz0advv1Ut4CJgf5uhDuDj5eUcAUoahrdY/56ebRWeraTjMt/00Sh3UDtjgHtQNHwcRGOC98BJEAEymycmYcWwOprTgcB6VZ5JK5TAJ+fXGLBm3FDAmn6oPPjR4rKCAoJCal2eAiQp2x0vxTPB3ALO2CRkwmDy5WohzBDwSEFKRwPbknEggCPB/imwrycgxX2NzoMCHhPkDwqYMr9tRcP5qNrMZHkVnOjRMWwLCcr8ohBVb1OMjxLwGCvjTikrsBOiA6fNyCrm8V1rP93iVPpwaE+gO0SsWmPiXB+jikdf6SizrT5qKasx5j8ABbHpFTx+vFXp9EnYQmLx02h1QTTrl6eDqxLnGjporxl3NL3agEvXdT0WmEost648sQOYAeJS9Q7bfUVoMGnjo4AZdUMQku50McDcMWcBPvr0SzbTAFDfvJqwLzgxwATnCgnp4wDl6Aa+Ax283gghmj+vj7feE2KBBRMW3FzOpLOADl0Isb5587h/U4gGvkt5v60Z1VLG8BhYjbzRwyQZemwAd6cCR5/XFWLYZRIMpX39AR0tjaGGiGzLVyhse5C9RKC6ai42ppWPKiBagOvaYk8lO7DajerabOZP46Lby5wKjw1HCRx7p9sVMOWGzb/vA1hwiWc6jm3MvQDTogQkiqIhJV0nBQBTU+3okKCFDy9WwferkHjtxib7t3xIUQtHxnIwtx4mpg26/HfwVNVDb4oI9RHmx5WGelRVlrtiw43zboCLaxv46AZeB3IlTkwouebTr1y2NjSpHz68WNFjHvupy3q8TFn3Hos2IAk4Ju5dCo8B3wP7VPr/FGaKiG+T+v+TQqIrOqMTL1VdWV1DdmcbO8KXBz6esmYWYKPwDL5b5FA1a0hwapHiom0r/cKaoqr+27/XcrS5UwSMbQAAAABJRU5ErkJggg==)](https://deepwiki.com/emmorts/dbfsharp)
![NuGet Version](https://img.shields.io/nuget/v/dbfsharp.core)

**A modern, high-performance command-line tool for working with Shapefiles and DBF database files.**

Convert Shapefiles to GeoJSON, extract data from DBF files to CSV/JSON, perform spatial queries, and analyze geospatial data—all from your terminal.

---

## Quick Start

```bash
# Install
brew install dbfsharp

# Export Shapefile to GeoJSON
dbfsharp read cities.shp --format geojson --output cities.geojson

# Spatial query - filter by bounding box
dbfsharp read parcels.shp --bounding-box "-118.5,34.0,-118.0,34.5" --format geojson

# Convert DBF to CSV
dbfsharp read data.dbf --format csv --output data.csv

# Analyze file structure
dbfsharp info cities.shp
```

---

## Key Features

- **Complete Shapefile support** - Read geometry, attributes, and perform spatial operations
- **Spatial queries** - Bounding box filtering, nearest neighbor searches, R-tree spatial indexing
- **GeoJSON export** - Convert Shapefiles to modern GeoJSON format
- **Multiple output formats** - Export to GeoJSON, CSV, TSV, JSON, or console tables
- **Universal DBF compatibility** - Supports all DBF versions (dBASE III/IV, Visual FoxPro, Clipper)
- **High performance** - Optimized streaming for large files (millions of records)
- **Cross-platform CLI** - Works on Windows, Linux, and macOS
- **Pipeline friendly** - Works with stdin/stdout for Unix-style data processing
- **Legacy encoding support** - Properly handles various character encodings

---

## Installation

### Homebrew (Recommended)

```bash
brew install dbfsharp
```

### .NET Tool (Alternative)

If you have .NET 9.0 SDK or later installed:

```bash
dotnet tool install -g DbfSharp
```

Update to the latest version:

```bash
dotnet tool update -g DbfSharp
```

### Verify Installation

```bash
dbfsharp --version
```

---

## Common Use Cases

### Working with Shapefiles

```bash
# Export complete Shapefile to GeoJSON
dbfsharp read buildings.shp --format geojson --output buildings.geojson

# Filter by geographic bounding box (minX,minY,maxX,maxY)
dbfsharp read parcels.shp --bounding-box "-122.5,37.7,-122.3,37.8" --format geojson

# Find nearest features to a point
dbfsharp read poi.shp --nearest-point "-118.25,34.05" --nearest-count 10 --format geojson

# Extract attributes only (ignore geometry)
dbfsharp read census.shp --fields "GEOID,POP2020,NAME" --format csv
```

### Converting DBF Data

```bash
# Convert to CSV for Excel/Google Sheets
dbfsharp read data.dbf --format csv --output data.csv

# Convert to JSON for modern applications
dbfsharp read products.dbf --format json --output products.json

# Extract specific fields only
dbfsharp read employees.dbf --fields "ID,NAME,EMAIL,SALARY" --format csv
```

### Data Analysis & Exploration

```bash
# Inspect file structure and metadata
dbfsharp info data.dbf

# View detailed statistics and sample records
dbfsharp info data.dbf --verbose

# Preview first 20 records
dbfsharp read data.dbf --limit 20

# Sample middle section of data
dbfsharp read data.dbf --skip 1000 --limit 50
```

### Pipeline Integration

```bash
# Filter with grep
dbfsharp read employees.dbf --format csv | grep "Engineer"

# Process with jq
dbfsharp read sales.dbf --format json | jq '.[] | select(.AMOUNT > 1000)'

# Chain with other commands
dbfsharp read data.dbf --format csv --fields "ID,NAME" | sort | uniq | head -10

# Process from stdin
cat remote_file.dbf | dbfsharp read --format json
```

---

## Commands

### `read` - Convert and Export Data

Extract data from DBF files or Shapefiles and export to various formats.

**Syntax:**
```bash
dbfsharp read [FILE] [OPTIONS]
```

**Key Options:**
- `--format`, `-f` - Output format: `table` (default), `csv`, `tsv`, `json`, `geojson`
- `--output`, `-o` - Output file path (stdout if not specified)
- `--fields` - Comma-separated list of fields to include
- `--limit`, `-l` - Maximum number of records
- `--skip`, `-s` - Skip first N records
- `--bounding-box` - Spatial filter: "minX,minY,maxX,maxY" (Shapefiles only)
- `--nearest-point` - Find nearest features: "x,y" (Shapefiles only)
- `--build-spatial-index` - Build R-tree index for faster queries

📚 **[Full command documentation →](./DbfSharp.ConsoleAot/README.md#read---extract-and-export-data)**

### `info` - Analyze File Structure

Display comprehensive metadata, field definitions, and statistics about DBF files or Shapefiles.

**Syntax:**
```bash
dbfsharp info [FILE] [OPTIONS]
```

**Key Options:**
- `--verbose`, `-v` - Show additional details including sample data
- `--fields` - Show field definitions (default: true)
- `--stats` - Show record statistics (default: true)
- `--memo` - Show memo file information (default: true)

📚 **[Full command documentation →](./DbfSharp.ConsoleAot/README.md#info---analyze-file-structure)**

---

## Supported Formats

### Input Formats

**DBF Files:**
- dBASE II, III, III+, IV, 5
- FoxBase, FoxPro 2.x
- Visual FoxPro 3-9
- Clipper
- All standard field types (Character, Date, Numeric, Logical, Memo, etc.)

**Shapefiles:**
- Geometry types: Point, MultiPoint, LineString, Polygon, MultiPatch
- Spatial components: .shp (geometry), .shx (index), .dbf (attributes)
- Projection files: .prj (coordinate system)
- Character encoding: .cpg files

**Memo Files:**
- .dbt (dBASE III, IV)
- .fpt (Visual FoxPro)

### Output Formats

- **CSV** - Comma-separated values
- **TSV** - Tab-separated values
- **JSON** - Array of objects with field names as keys
- **GeoJSON** - For Shapefiles with geometry (RFC 7946 compliant)
- **Table** - Human-readable console table

---

## Performance

DbfSharp is optimized for high throughput and low memory usage:

- **Streaming processing** - Handle files larger than available RAM
- **Memory-mapped files** - Efficient access for large datasets
- **R-tree spatial indexing** - Fast spatial queries on Shapefiles
- **Native AOT compilation** - Minimal startup overhead

**Benchmark (1 million records, 10 fields):**
- Sequential read: ~1.3M records/sec
- Memory-optimized: ~3.3M records/sec with 24MB RAM
- CSV export: ~800K records/sec
- JSON export: ~600K records/sec

💡 **Tip:** Use `--build-spatial-index` for large Shapefiles with spatial queries to significantly improve performance.

---

## .NET Library

DbfSharp also provides a high-performance .NET library for programmatic access in your applications:

```csharp
using DbfSharp.Core;

// Read Shapefile with spatial queries
using var shpReader = ShapefileReader.Open("cities.shp");
shpReader.BuildSpatialIndex();

var bbox = new BoundingBox(-100, 40, -90, 50);
var features = shpReader.QuerySpatialIndex(bbox);

foreach (var feature in features)
{
    var geometry = feature.Geometry;
    var population = feature.GetAttribute<int>("POPULATION");
    Console.WriteLine($"{geometry}: {population:N0} people");
}

// Read DBF file
using var reader = DbfReader.Create("data.dbf");
foreach (var record in reader.Records)
{
    var name = record.GetString("NAME");
    var date = record.GetDateTime("DATE");
    Console.WriteLine($"{name}: {date}");
}
```

📚 **[Complete .NET library documentation →](./DbfSharp.Core/README.md)**

**Install via NuGet:**
```bash
dotnet add package DbfSharp.Core
```

---

## Examples & Resources

- **CLI Documentation:** [DbfSharp.ConsoleAot/README.md](./DbfSharp.ConsoleAot/README.md)
- **Library Documentation:** [DbfSharp.Core/README.md](./DbfSharp.Core/README.md)
- **Issue Tracker:** [GitHub Issues](https://github.com/emmorts/DbfSharp/issues)
- **Discussions:** [GitHub Discussions](https://github.com/emmorts/DbfSharp/discussions)

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
