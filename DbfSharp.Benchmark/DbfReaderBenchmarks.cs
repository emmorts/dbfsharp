using BenchmarkDotNet.Attributes;
using DbfSharp.Core;

namespace DbfSharp.Benchmark
{
    [MemoryDiagnoser]
    public class DbfReaderBenchmarks
    {
        private string _dbfPath = string.Empty;

        [Params(100, 10_000, 100_000, 1_000_000)]
        public int RowCount;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
            _dbfPath = Path.Combine(testDataDir, $"benchmark_{RowCount}.dbf");

            if (!File.Exists(_dbfPath))
            {
                throw new FileNotFoundException(
                    $"Test file not found: {_dbfPath}. Run 'dotnet run generate-test-files' first."
                );
            }
        }

        [Benchmark(Description = "Read all records synchronously (streaming mode)")]
        public void ReadAllRecordsSync()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.Records)
            {
                // access a field to ensure parsing happens - traditional API
                var name = record.GetString(1); // access NAME field
            }
        }

        [Benchmark(Description = "Read all records with zero-allocation span API")]
        public void ReadAllRecordsSpan()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                // access a field using zero-allocation span API
                var name = record.GetString(1); // access NAME field
            }
        }

        [Benchmark(Description = "Read raw field bytes (true zero-allocation)")]
        public void ReadRawFieldBytes()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                // access raw field bytes - no allocations at all
                var fieldBytes = record.GetFieldBytes(1); // access NAME field bytes
                var fieldLength = fieldBytes.Length; // just access length to ensure enumeration
            }
        }

        [Benchmark(
            Description = "Read all records synchronously (streaming mode) with field access"
        )]
        public void ReadAllRecordsWithFieldAccess()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.Records)
            {
                // Access the first field to trigger parsing
                var value = record[0];
            }
        }

        [Benchmark(Description = "Load all records into memory then enumerate")]
        public void ReadAllRecordsLoaded()
        {
            using var dbf = DbfReader.Create(_dbfPath);
            dbf.Load();

            foreach (var record in dbf)
            {
                // this loop will enumerate the loaded records from memory
            }
        }

        [Benchmark(Description = "Count records only (minimal processing)")]
        public void CountRecordsOnly()
        {
            using var dbf = DbfReader.Create(_dbfPath);
            var count = 0;

            foreach (var record in dbf)
            {
                count++;
                // just count records without accessing field data
            }
        }

        [Benchmark(Description = "Read single field per record (streaming mode)")]
        public void ReadSingleFieldSync()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf)
            {
                _ = record[0]; // access only the first field to minimize allocation
            }
        }

        [Benchmark(Description = "Read raw bytes with memory-optimized options")]
        public void ReadRawBytesMemoryOptimized()
        {
            var options = DbfReaderOptions.CreateMemoryOptimized();
            using var dbf = DbfReader.Create(_dbfPath, options);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                var fieldBytes = record.GetFieldBytes(1);
                var fieldLength = fieldBytes.Length;
            }
        }

        [Benchmark(Description = "Read raw bytes with zero-allocation options")]
        public void ReadRawBytesZeroAllocation()
        {
            var options = DbfReaderOptions.CreateZeroAllocationOptimized();
            using var dbf = DbfReader.Create(_dbfPath, options);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                var fieldBytes = record.GetFieldBytes(1);
                var fieldLength = fieldBytes.Length;
            }
        }

        [Benchmark(Description = "Read raw bytes with memory-mapped files")]
        public void ReadRawBytesMemoryMapped()
        {
            var options = new DbfReaderOptions { UseMemoryMapping = true };
            using var dbf = DbfReader.Create(_dbfPath, options);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                var fieldBytes = record.GetFieldBytes(1);
                var fieldLength = fieldBytes.Length;
            }
        }

        [Benchmark(Description = "Read raw bytes with memory-mapped + performance options")]
        public void ReadRawBytesMemoryMappedOptimized()
        {
            var options = DbfReaderOptions.CreatePerformanceOptimized();
            options.UseMemoryMapping = true;
            using var dbf = DbfReader.Create(_dbfPath, options);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                var fieldBytes = record.GetFieldBytes(1);
                var fieldLength = fieldBytes.Length;
            }
        }
    }
}
