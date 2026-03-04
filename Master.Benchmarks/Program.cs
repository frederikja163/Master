using BenchmarkDotNet.Running;
using Master.Benchmarks.Raw;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        var b = new TPCHBenchmarks() { Data = TPCHBenchmarks.GetData().First(t => t.ToString() == "LINEITEM") };
        b.WriteRaw(new CascadingBenchmark());
        /*BenchmarkRunner.Run<OpenTAPBenchmarks>();
        BenchmarkRunner.Run<RawBenchmarks>();
        BenchmarkRunner.Run<SparkBenchmarks>();*/
        // BenchmarkRunner.Run<TPCHBenchmarks>();
    }
}