using BenchmarkDotNet.Running;
using Master.Benchmarks.Data;
using Master.Benchmarks.Raw;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        if (args[0] == "t")
        {
            foreach (TpchData data in TPCHBenchmarks.GetData().ToArray())
            {
                TPCHBenchmarks b = new TPCHBenchmarks()
                {
                    Data = data,
                };
                b.WriteRaw(new CascadingBenchmark());
            }
        }

        
        else if (args[0] == "b")
        {
            // BenchmarkRunner.Run<OpenTAPBenchmarks>();
            // BenchmarkRunner.Run<RawBenchmarks>();
            // BenchmarkRunner.Run<SparkBenchmarks>();
            BenchmarkRunner.Run<TPCHBenchmarks>();
        }
    }
}