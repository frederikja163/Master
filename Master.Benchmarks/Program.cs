using BenchmarkDotNet.Running;
using Master.Benchmarks.RawBenchmarks;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        // new RawSqlite().Write("File.db", new Data(10000, 10).PopulateOrderedInts());
        BenchmarkRunner.Run<AllBenchmarks>();
    }
}