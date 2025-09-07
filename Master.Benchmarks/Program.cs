using BenchmarkDotNet.Running;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        BenchmarkRunner.Run<AllBenchmarks>();
    }
}