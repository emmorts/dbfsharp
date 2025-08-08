```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 9.0.109
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2


```
| Method                                       | RowCount | Mean        | Error     | StdDev    | Median      | Gen0        | Gen1      | Allocated  |
|--------------------------------------------- |--------- |------------:|----------:|----------:|------------:|------------:|----------:|-----------:|
| **&#39;Read all records from a generated DBF file&#39;** | **100000**   |    **235.9 ms** |   **1.37 ms** |   **1.21 ms** |    **235.6 ms** |   **2666.6667** |         **-** |   **67.18 MB** |
| **&#39;Read all records from a generated DBF file&#39;** | **1000000**  |  **2,281.9 ms** |  **15.83 ms** |  **13.22 ms** |  **2,278.9 ms** |  **31000.0000** |         **-** |   **698.9 MB** |
| **&#39;Read all records from a generated DBF file&#39;** | **10000000** | **23,486.2 ms** | **467.46 ms** | **986.03 ms** | **22,963.4 ms** | **312000.0000** | **2000.0000** | **7016.15 MB** |
