using BenchmarkDotNet.Running;
using Master.Benchmarks.Raw;
using Parquet;
using Vortex.Net;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        // BenchmarkRunner.Run<OpenTAPBenchmarks>();
        // BenchmarkRunner.Run<RawBenchmarks>();
        BenchmarkRunner.Run<SparkBenchmarks>();
    }
}