using BenchmarkDotNet.Running;
using Keysight.OpenTap.Plugins.Csv;
using Master.Benchmarks.RawBenchmarks;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        BenchmarkRunner.Run<AllBenchmarks>();
    }
}