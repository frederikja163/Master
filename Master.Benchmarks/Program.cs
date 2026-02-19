using BenchmarkDotNet.Running;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            BenchmarkRunner.Run<OpenTAPBenchmarks>();
            BenchmarkRunner.Run<RawBenchmarks>();
            BenchmarkRunner.Run<SparkBenchmarks>();
            BenchmarkRunner.Run<TPCHBenchmarks>();
            BenchmarkRunner.Run<SplitNullsBenchmarks>();
        }

        switch (args[0])
        {
            case "otap":
                BenchmarkRunner.Run<OpenTAPBenchmarks>();
                break;
            case "spark":
                BenchmarkRunner.Run<SparkBenchmarks>();
                break;
            case "raw":
                BenchmarkRunner.Run<RawBenchmarks>();
                break;
            case "tpch":
                BenchmarkRunner.Run<TPCHBenchmarks>();
                break;
            case "split":
                BenchmarkRunner.Run<SplitNullsBenchmarks>();
                break;
        }
    }
}