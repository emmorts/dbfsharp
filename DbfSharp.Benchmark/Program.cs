using BenchmarkDotNet.Running;
using DbfSharp.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DbfReaderBenchmarks>();
    }
}
