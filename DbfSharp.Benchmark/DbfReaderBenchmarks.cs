using BenchmarkDotNet.Attributes;
using DbfSharp.Core;

namespace DbfSharp.Benchmark
{
    [MemoryDiagnoser]
    public class DbfReaderBenchmarks
    {
        private string _dbfPath = string.Empty;

        [Params(100, 10_000, 100_000)]
        public int RowCount;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
            _dbfPath = Path.Combine(testDataDir, $"benchmark_{RowCount}.dbf");

            if (!File.Exists(_dbfPath))
            {
                throw new FileNotFoundException($"Test file not found: {_dbfPath}. Run 'dotnet run generate-test-files' first.");
            }
        }

        [Benchmark(Description = "Read all records synchronously (streaming mode)")]
        public void ReadAllRecordsSync()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.Records)
            {
                // Access a field to ensure parsing happens - traditional API
                var name = record.GetString(1); // Access NAME field
            }
        }

        [Benchmark(Description = "Read all records with zero-allocation span API")]
        public void ReadAllRecordsSpan()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                // Access a field using zero-allocation span API
                var name = record.GetString(1); // Access NAME field
            }
        }

        [Benchmark(Description = "Read raw field bytes (true zero-allocation)")]
        public void ReadRawFieldBytes()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf.EnumerateSpanRecords())
            {
                // Access raw field bytes - no allocations at all
                var fieldBytes = record.GetFieldBytes(1); // Access NAME field bytes
                var fieldLength = fieldBytes.Length; // Just access length to ensure enumeration
            }
        }

        [Benchmark(Description = "Read all records synchronously (streaming mode) with field access")]
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
                // This loop will enumerate the loaded records from memory.
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
                // Just count records without accessing field data
            }
        }

        [Benchmark(Description = "Read single field per record (streaming mode)")]
        public void ReadSingleFieldSync()
        {
            using var dbf = DbfReader.Create(_dbfPath);

            foreach (var record in dbf)
            {
                _ = record[0]; // Access only the first field to minimize allocation
            }
        }
    }
}
