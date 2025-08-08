using BenchmarkDotNet.Attributes;
using DbfSharp.Core;
using System.Collections.Generic;
using System.IO;

namespace DbfSharp.Benchmark
{
    [MemoryDiagnoser]
    public class DbfReaderBenchmarks
    {
        private string _dbfPath;
        private readonly List<DbfFieldInfo> _fields;

        [Params(100_000, 1_000_000)]
        public int RowCount;

        public DbfReaderBenchmarks()
        {
            _fields = new List<DbfFieldInfo>
            {
                new DbfFieldInfo("ID", 'N', 10),
                new DbfFieldInfo("NAME", 'C', 50),
                new DbfFieldInfo("EMAIL", 'C', 50),
                new DbfFieldInfo("JOIN_DATE", 'D', 8),
                new DbfFieldInfo("SCORE", 'N', 10, 2),
                new DbfFieldInfo("ADDRESS", 'C', 100),
                new DbfFieldInfo("CITY", 'C', 50),
                new DbfFieldInfo("STATE", 'C', 2),
                new DbfFieldInfo("ZIP", 'C', 10),
                new DbfFieldInfo("NOTES", 'C', 100)
            };
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _dbfPath = Path.Combine(Path.GetTempPath(), $"benchmark_{RowCount}.dbf");
            DbfFileGenerator.Generate(_dbfPath, RowCount, _fields);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (File.Exists(_dbfPath))
            {
                File.Delete(_dbfPath);
            }
        }

        [Benchmark(Description = "Read all records from a generated DBF file")]
        public void ReadAllRecords()
        {
            using (var dbf = DbfReader.Create(_dbfPath))
            {
                foreach (var record in dbf)
                {
                    // This loop will consume the IEnumerable and trigger reading of all records.
                }
            }
        }
    }
}
