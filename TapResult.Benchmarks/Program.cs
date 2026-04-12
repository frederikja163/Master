using BenchmarkDotNet.Running;
using TapResult.Benchmarks.Raw;

namespace TapResult.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        BenchmarkRunner.Run<OpenTAPBenchmarks>();
        BenchmarkRunner.Run<RawBenchmarks>();
        BenchmarkRunner.Run<SparkBenchmarks>();
        BenchmarkRunner.Run<TPCHBenchmarks>();
    }
}