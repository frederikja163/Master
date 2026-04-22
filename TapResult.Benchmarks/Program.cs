using BenchmarkDotNet.Running;
using TapResult.OpenTAP;

namespace TapResult.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        new ReadBenchmarks().Setup();
        new ReadBenchmarks().ReadSingleTapResult().GetAwaiter().GetResult();
        switch (args[0])
        {
            case nameof(OpenTAPBenchmarks):
                BenchmarkRunner.Run<OpenTAPBenchmarks>();
                break;
            case nameof(RawBenchmarks):
                BenchmarkRunner.Run<RawBenchmarks>();
                break;
            case nameof(SparkBenchmarks):
                BenchmarkRunner.Run<SparkBenchmarks>();
                break;
            case nameof(TPCHBenchmarks):
                BenchmarkRunner.Run<TPCHBenchmarks>();
                break;
            case nameof(ReadBenchmarks):
                BenchmarkRunner.Run<ReadBenchmarks>();
                break;
        }
    }
}