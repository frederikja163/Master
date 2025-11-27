using BenchmarkDotNet.Running;
using Master.Benchmarks.Raw;
using Parquet;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        new RawCsv().Write("file.parquet", new Data(1000, 1, 0.5f).PopulateOrderedInts());
        // BenchmarkRunner.Run<OpenTAPBenchmarks>();
        // BenchmarkRunner.Run<RawBenchmarks>();
        // BenchmarkRunner.Run<SparkBenchmarks>();
    }
}