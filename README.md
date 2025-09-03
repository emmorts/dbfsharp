# DbfSharp

A high-performance .NET library and command-line tool for reading dBASE (DBF) files and Shapefiles with support for all major DBF versions, memo files, and advanced spatial operations.

## Features

- Support for all major DBF versions (dBase III, IV, Visual FoxPro, etc.)
- **Complete Shapefile support** with geometry reading and spatial indexing
- **Spatial operations** with R-tree indexing for fast spatial queries
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

#### DBF Usage

```csharp
using DbfSharp.Core;

using var reader = DbfReader.Create("data.dbf");
foreach (var record in reader.Records)
{
    var name = record.GetString("NAME");
    var birthDate = record.GetDateTime("BIRTHDATE");
    Console.WriteLine($"{name}, born {birthDate}");
}
```

#### Shapefile Usage

```csharp
using DbfSharp.Core;

// Read shapefile with geometry and attributes
using var reader = ShapefileReader.Open("cities.shp");
foreach (var feature in reader.Features)
{
    var geometry = feature.Geometry; // Point, LineString, Polygon, etc.
    var population = feature.GetAttribute<int>("POPULATION");
    Console.WriteLine($"City at {geometry}: {population:N0} people");
}
```

#### Spatial Operations

```csharp
// Build spatial index for fast queries
reader.BuildSpatialIndex();

// Find features within bounding box
var bbox = new BoundingBox(minX: -100, minY: 40, maxX: -90, maxY: 50);
var features = reader.QuerySpatialIndex(bbox);

// Find nearest features to a point
var point = new Point(-95.5, 45.2);
var nearest = reader.FindNearestFeatures(new Coordinate(point.X, point.Y), count: 5);
```

### Command-Line Tool

Refer to [ConsoleAot](./DbfSharp.ConsoleAot/README.md) for a more extensive documentation.

#### Installation

```bash
dotnet tool install -g DbfSharp
```

#### Usage

```bash
# read a DBF file, default output to console as a table
dbfsharp read data.dbf

# export to CSV
dbfsharp read data.dbf --format csv --output data.csv

# read shapefile with geometry
dbfsharp read buildings.shp --format geojson --output buildings.geojson

# spatial queries on shapefiles
dbfsharp read parcels.shp --bounding-box "-118.5,34.0,-118.0,34.5" --format geojson

# get file information
dbfsharp info data.dbf --verbose
```

## Supported Formats

### DBF Files
- dBase II, III, IV
- FoxBase, FoxPro
- Visual FoxPro
- Memo files (.dbt, .fpt)
- All standard field types (Character, Date, Numeric, Logical, Memo, etc.)

### Shapefiles
- **Geometry Types**: Point, MultiPoint, LineString, Polygon, MultiPatch
- **Spatial Index**: R-tree indexing for fast spatial queries
- **Complete Support**: .shp (geometry), .shx (index), .dbf (attributes)
- **Coordinate Systems**: .prj files with projection support
- **Encoding**: .cpg files for proper character encoding
